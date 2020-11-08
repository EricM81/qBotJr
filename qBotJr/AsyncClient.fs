namespace qBotJr

open Discord.WebSocket
open System
open Discord.WebSocket
open qBotJr.Interpreter
open qBotJr.T
open qBotJr.helper


//Threadsafe MailboxProcessor to filter commands and update state
module AsyncClient =

    let mutable private state = State.create

    type private FoundMessage = Command * MessageFilter option
    type private FoundReaction = ReAction * ReactionFilter option


    module private command =

        let inline parseMsg (cmd : Command) (msg : SocketMessage) =
            parseInput cmd.PrefixUpper msg.Content
            |> ParsedMsg.create msg

        let checkPermAndRun (cmd : Command) (nm : NewMessage) =
            let pm = parseMsg cmd nm.Message
            let goo = nm.GuildOO

            if (getPerm goo.User) >= cmd.RequiredPerm then cmd.PermSuccess pm goo else cmd.PermFailure pm goo

        let inline matchPrefix (cmd  : Command) (nm : NewMessage) : bool =
            let str = nm.Message.Content
            let i = cmd.PrefixLength
            (str.Length >= i && str.Substring(0, i).ToUpper() = cmd.PrefixUpper)

        let matchArray (nm : NewMessage) (arr : Command array) : ContinueOption<NewMessage, FoundMessage> =
            arr
            |> Array.tryFind (fun cmd -> matchPrefix cmd nm)
            |> function
                | Some cmd -> Found(cmd, None)
                | None -> Continue nm

        let filterStaticCommands (nm : NewMessage) : ContinueOption<NewMessage, FoundMessage> =
            //all static bot commands start with a "q"
            //no Q, no need to check
            let q = nm.Message.Content.[0]
            if (q = 'Q' || q = 'q') then matchArray nm state.StaticFilters else Continue nm


        //cmds I can run for testing....or memeing
        let filterCreatorCommands (nm : NewMessage) : ContinueOption<NewMessage, FoundMessage> =
            if (UserPermission.Creator) = isCreator nm.GuildOO.User then
                matchArray nm state.CreatorFilters
            else Continue nm


        let filterDynamicCommands (nm : NewMessage) : ContinueOption<NewMessage, FoundMessage> =
            let now = DateTimeOffset.Now
            let guildID = nm.GuildOO.Guild.Id

            let rec matchFilterItem (xs : Command list) : ContinueOption<NewMessage, FoundMessage> =
                match xs with
                | [] -> Continue nm.Message
                | x :: xs ->
                    match nm.Message with
                    | ParseMsg x args -> checkPermAndRun nm.GuildOO x args
                    | _ -> matchFilterItem xs

            let rec matchFilter (xs : MessageFilter list) : ContinueOption<NewMessage, FoundMessage> =
                match xs with
                | [] -> Continue nm
                | x :: xs when (guildID = x.GuildID && now < x.TTL) ->

                    let result =
                        match x.User with
                        | Some user when user = nm.GuildOO.User.Id -> matchFilterItem x.Items
                        | Some user when user <> nm.GuildOO.User.Id -> Continue nm
                        | _ -> matchFilterItem x.Items

                    match result with
                    | Found y ->
                        x.TTL <- DateTimeOffset.MinValue
                        Found y
                    | _ -> matchFilter xs

                | _ :: xs -> matchFilter xs

            matchFilter State.MessageFilters

    module private reaction =
        let inline updateModeList (player : uint64) (players : uint64 list) (isHere : bool) : uint64 list =
            let isInList =
                players |> List.exists (fun id -> player = id)

            match isInList, isHere with
            | true, true
            | false, false -> players
            | true, false -> players |> List.filter (fun id -> player <> id)
            | false, true -> player :: players

        let inline matchGameModes (server : Server) (mr : MessageReaction) : bool =
            let isMatch (mode : Mode) : bool =
                if mode.ModeMsg.Id = mr.Message.Id then
                    mode.PlayerIDs <- updateModeList mr.Reaction.UserId mode.PlayerIDs mr.IsHere
                    true
                else
                    false

            server.Modes |> List.exists isMatch

        let inline updateHereList (server : Server) (mr : MessageReaction) (player : Player option) =
            match player, mr.IsHere with
            | Some p, true
            | Some p, false -> p.isHere <- mr.IsHere
            | None, true ->
                let iUser = mr.GuildOO.User
                server.Players <-
                    (Player.create iUser.Id iUser.Nickname)
                    :: server.Players
            | None, false -> ()

            server.PlayerListIsDirty <- true

        let inline matchHereMsg (server : Server) (mr : MessageReaction) : bool =
            match server.HereMsg with
            | Some msg when mr.Message.Id = msg.Id ->
                server.Players
                |> List.tryFind (fun player -> if player.UID = mr.Reaction.UserId then true else false)
                |> updateHereList server mr
                true
            | _ -> matchGameModes server mr

        let filterStaticReactions (mr : MessageReaction) : ContinueOption<MessageReaction, FoundReaction> =
            let reactionFound =
                State.Guilds
                |> Map.exists (fun _ v -> if v.Guild.Id = mr.GuildOO.Guild.Id then matchHereMsg v mr else false)

            if reactionFound then Found else Continue mr

        //let inline matchFilterChoice (filterChoice : ReactionFilterChoice) (mr : MessageReaction) : bool =

        let inline matchReActions (ras : ReAction list) (mr : MessageReaction) : bool =
            let inline matchAction (ra : ReAction) : bool =
                if mr.Reaction.Emote.Name = ra.Emoji then
                    ra.Action mr
                    true
                else
                    false

            ras |> List.exists matchAction

        let inline matchFilter (filter : ReactionFilter) (mr : MessageReaction) : bool =
            let isMatch =
                match filter.FilterChoice with
                | ByReaction br when br.MsgID = mr.Message.Id -> matchReActions br.Actions mr
                | ByReactionAndUser bru when bru.MsgID = mr.Message.Id
                                             && bru.UserID = mr.Reaction.UserId -> matchReActions bru.Actions mr
                | _ -> false

            if isMatch then
                filter.TTL <- DateTimeOffset.MinValue
                true
            else
                false


        let filterDynamicReactions (mr : MessageReaction) : ContinueOption<MessageReaction, FoundReaction> =
            let isMatch =
                State.ReactionFilters
                |> List.exists (fun filter ->
                    if filter.GuildID = mr.GuildOO.Guild.Id then matchFilter filter mr else false)

            if isMatch then Found else Continue mr

    module private mail =

        let processMail (inbox : MailboxProcessor<MailboxMessage>) =
            let rec msgLoop () =
                async {
                    let! mm = inbox.Receive()

                    match mm with
                    | NewMessage nm ->
                        let search =
                            command.filterStaticCommands nm
                            |> bindCont command.filterCreatorCommands
                            |> bindCont command.filterDynamicCommands
                        match search with
                        | Found cmd -> command.checkPermAndRun cmd nm
                        | _ -> ()

                    | MessageReaction mr ->
                        reaction.filterStaticReactions mr
                        |> bindCont reaction.filterDynamicReactions
                        |> ignore
                    | Task t -> t.Invoke &state

                    return! msgLoop ()
                }

            msgLoop ()

        let agent = MailboxProcessor.Start(processMail)

    let InitializeClient creatorFilters staticFilters =
        state.CreatorFilters <- creatorFilters
        state.StaticFilters <- staticFilters

    //once a message is handled, it returns
    let Receive (mm : MailboxMessage) = mail.agent.Post mm



