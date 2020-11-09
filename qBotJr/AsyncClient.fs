namespace qBotJr

open System
open Discord.WebSocket
open qBotJr.T
open qBotJr.Interpreter
open qBotJr.helper


//Threadsafe MailboxProcessor to filter commands and update state
module AsyncClient =

    let mutable private state = State.create

    module private command =

        type private FoundMessage = Command * MessageFilter option

        let inline parseMsg (cmd : Command) (msg : SocketMessage) =
            parseInput cmd.PrefixUpper msg.Content |> ParsedMsg.create msg

        let checkPermAndRun (cmd : Command) (nm : NewMessage) =
            let pm = parseMsg cmd nm.Message
            let goo = nm.Goo


            if (getPerm goo.User) >= cmd.RequiredPerm then cmd.PermSuccess pm goo else cmd.PermFailure pm goo

        let inline matchPrefix (cmd : Command) (nm : NewMessage) : bool =
            let str = nm.Message.Content
            let i = cmd.PrefixLength
            (str.Length >= i && str.Substring(0, i).ToUpper() = cmd.PrefixUpper)

        let matchArray (nm : NewMessage) (items : Command array) : ContinueOption<NewMessage, FoundMessage> =
            items
            |> Array.tryFind (fun cmd -> matchPrefix cmd nm)
            |> function
            | Some cmd -> Found(cmd, None)
            | None -> Continue nm

        let searchStatic (nm : NewMessage) : ContinueOption<NewMessage, FoundMessage> =
            //all static bot commands start with a "q"
            //no Q, no need to check
            let q = nm.Message.Content.[0]
            if (q = 'Q' || q = 'q') then matchArray nm state.StaticFilters else Continue nm

        //cmds I can run for testing....or memeing
        let searchCreator (nm : NewMessage) : ContinueOption<NewMessage, FoundMessage> =
            if (UserPermission.Creator) = isCreator nm.Goo.User then
                matchArray nm state.CreatorFilters
            else
                Continue nm

        let matchList (nm : NewMessage) (items : Command list) : Command option =
            items |> List.tryFind (fun cmd -> matchPrefix cmd nm)

        let matchFilter (msgGuild : uint64) (nm : NewMessage) (filter : MessageFilter) : FoundMessage option =
            match filter.GuildID, filter.User with
            | id, None when id = msgGuild -> matchList nm filter.Items
            | id, Some u when id = msgGuild && u = nm.Goo.User.Id -> matchList nm filter.Items
            | _ -> None //user doesn't match
            |> function
            | Some cmd -> Some(cmd, Some filter)
            | None -> None

        let searchDynamic (nm : NewMessage) : ContinueOption<NewMessage, FoundMessage> =
            let now = DateTimeOffset.Now
            let msgGuild = nm.Goo.Guild.Id

            state.DynamicFilters
            |> List.tryPick (fun filter -> if filter.TTL > now then matchFilter msgGuild nm filter else None)
            |> function
            | Some fm -> Found fm
            | None -> Continue nm

    module private reaction =

        type private FoundReaction = ReactionAction * ReactionFilter option

        let inline matchReaction (msg : uint64) (emoji : string) (item : ReAction) : ReactionAction option =
            if item.MessageID = msg && item.Emoji = emoji then Some item.Action else None

        let matchList (msg : uint64) (emoji : string) (items : ReAction list) : ReactionAction option =
            items |> List.tryPick (fun item -> matchReaction msg emoji item)

        let matchModes (msg : uint64) (emoji : string) (items : Mode<Server> list) : ReactionAction option =
            items |> List.tryPick (fun item -> matchReaction msg emoji item.HereMsg.ReAction)

        let matchServer (msg : uint64) (emoji : string) (server : Server) : ReactionAction option =
            match server.HereMsg with
            | Some x when x.MessageID = msg && x.Emoji = emoji -> Some x.ReAction.Action
            | _ -> matchModes msg emoji server.Modes

        let searchServers (mr : MessageReaction) : ContinueOption<MessageReaction, FoundReaction> =
            //check here msg and game modes
            let msg = mr.Message.Id
            let emoji = mr.Reaction.Emote.Name

            state.Guilds
            |> Map.tryPick (fun _ server -> matchServer msg emoji server)
            |> function
            | Some x -> Found(x, None)
            | _ -> Continue mr

        let matchFilter (msg : uint64) (user : uint64) (emoji : string) (filter : ReactionFilter) : FoundReaction option =
            match filter.MessageId, filter.UserID with
            | fMsg, None when fMsg = msg -> matchList msg emoji filter.Items
            | fMsg, Some fUser when fMsg = msg && fUser = user -> matchList msg emoji filter.Items
            | _ -> None
            |> function
            | Some react -> Some(react, Some filter)
            | _ -> None


        let searchDynamic (mr : MessageReaction) : ContinueOption<MessageReaction, FoundReaction> =
            //check filters collection
            let msg = mr.Message.Id
            let user = mr.Reaction.UserId
            let emoji = mr.Reaction.Emote.Name

            state.ReactionFilters
            |> List.tryPick (fun filter -> matchFilter msg user emoji filter)
            |> function
            | Some fr -> Found fr
            | _ -> Continue mr

    let private matchMailbox (mm : MailboxMessage) =
        match mm with
        | NewMessage nm ->
            command.searchStatic nm
            |> bindCont command.searchCreator
            |> bindCont command.searchDynamic
            |> function
            | Found (cmd, filter) ->
                match filter with
                | None -> ()
                | Some f -> f.TTL <- DateTimeOffset.MinValue
                if cmd.RequiredPerm = UserPermission.Admin then
                    state.Guilds
                    |> Map.exists (fun k v ->
                        if k = nm.Goo.Guild.Id then
                            v.TTL <- DateTimeOffset.Now.AddHours(1.0)
                            true
                        else
                            false)
                    |> ignore
                command.checkPermAndRun cmd nm
            | _ -> ()
        | MessageReaction mr ->
            reaction.searchServers mr
            |> bindCont reaction.searchDynamic
            |> function
            | Found (action, filter) ->
                match filter with
                | Some f -> f.TTL <- DateTimeOffset.MinValue
                | None -> ()
                action mr
            | _ -> ()
        | Task t -> t.Invoke &state

    let private processMail (inbox : MailboxProcessor<MailboxMessage>) =
        let rec msgLoop () =
            async {
                let! mm = inbox.Receive()
                matchMailbox mm
                return! msgLoop () }
        msgLoop ()

    let private agent = MailboxProcessor.Start(processMail)

    let InitializeClient creatorFilters staticFilters =
        state.CreatorFilters <- creatorFilters
        state.StaticFilters <- staticFilters

    //once a message is handled, it returns
    let Receive (mm : MailboxMessage) = agent.Post mm
