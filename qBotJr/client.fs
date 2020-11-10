namespace qBotJr

open System
open Discord.WebSocket
open FSharpx.Control
open qBotJr.T
open qBotJr.parser
open qBotJr.helper


//Threadsafe MailboxProcessor to filter commands and update state
module client =

    let mutable private state = State.create

    type private FoundMessage = Command * MessageFilter option
    type private FoundReaction = ReactionAction * ReactionFilter option

    module private command =


        let inline parseMsg (cmd : Command) (msg : SocketMessage) =
            parseInput cmd.PrefixUpper msg.Content |> ParsedMsg.create msg

        let checkPermAndRun (cmd : Command) (nm : NewMessage) (s : Server) =
            let pm = parseMsg cmd nm.Message
            let goo = nm.Goo
            if (getPerm goo.User) >= cmd.RequiredPerm then cmd.PermSuccess pm goo s else cmd.PermFailure pm goo s

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
            if (q = 'Q' || q = 'q') then matchArray nm state.cmdStaticFilters else Continue nm

        //cmds I can run for testing....or memeing
        let searchCreator (nm : NewMessage) : ContinueOption<NewMessage, FoundMessage> =
            if (UserPermission.Creator) = isCreator nm.Goo.User then
                matchArray nm state.cmdCreatorFilters
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

        let searchTemp (nm : NewMessage) : ContinueOption<NewMessage, FoundMessage> =
            let now = DateTimeOffset.Now
            let msgGuild = nm.Goo.Guild.Id
            state.cmdTempFilters
            |> List.tryPick (fun filter -> if filter.TTL > now then matchFilter msgGuild nm filter else None)
            |> function
            | Some fm -> Found fm
            | None -> Continue nm

    module private reaction =

        let inline matchReaction (emoji : string) (item : ReAction) : ReactionAction option =
            if item.Emoji = emoji then Some item.Action else None

        let matchList (emoji : string) (items : ReAction list) : ReactionAction option =
            items |> List.tryPick (fun item -> matchReaction emoji item)

        let matchFilter (msg : uint64) (user : uint64) (emoji : string) (filter : ReactionFilter) : FoundReaction option =
            match filter.MessageID, filter.UserID with
            | fMsg, None when fMsg = msg -> matchList emoji filter.Items
            | fMsg, Some fUser when fMsg = msg && fUser = user -> matchList emoji filter.Items
            | _ -> None
            |> function
            | Some react -> Some(react, Some filter)
            | _ -> None

        let searchServer (mr : MessageReaction) : ContinueOption<MessageReaction, FoundReaction> =
            let searchFun (filter : ReactionFilter) =
                matchFilter mr.Message.Id mr.Reaction.UserId mr.Reaction.Emote.Name filter
            state.rtServerFilters
            |> List.tryPick searchFun
            |> function
            | Some (action, _) -> Found(action, None)
            | _ -> Continue mr

        let searchTemp (mr : MessageReaction) : ContinueOption<MessageReaction, FoundReaction> =
            //check filters collection
            let searchFun (filter : ReactionFilter) =
                if filter.TTL > DateTimeOffset.Now then
                    matchFilter mr.Message.Id mr.Reaction.UserId mr.Reaction.Emote.Name filter
                else
                    None
            state.rtTempFilters
            |> List.tryPick searchFun
            |> function
            | Some fr -> Found fr
            | _ -> Continue mr

    let private getServer (guild : SocketGuild) =
        state.Servers
        |> Map.tryFind guild.Id
        |> function
        | Some s -> s
        | None -> Server.create guild

    let private updateServerTTL (server : Server) = server.TTL <- DateTimeOffset.Now.AddHours(1.0)

    let runCommand (nm : NewMessage) ((cmd, filter) : FoundMessage) =
        match filter with
        | None -> ()
        | Some f -> f.TTL <- DateTimeOffset.MinValue
        let server = getServer nm.Goo.Guild
        if cmd.RequiredPerm = UserPermission.Admin then
            updateServerTTL server
        command.checkPermAndRun cmd nm server
        |> function
        | Done () -> ()
        | Async a -> Async.Start a
        | Server s -> state.Servers <- state.Servers |> Map.add s.Guild.Id s

    let runReaction (mr : MessageReaction) ((action, filter) : FoundReaction) : unit =
        match filter with
        | None -> ()
        | Some f -> f.TTL <- DateTimeOffset.MinValue
        getServer mr.Goo.Guild
        |> action mr
        |> function
        | Done () -> ()
        | Async a -> Async.Start a
        | Server s -> state.Servers <- state.Servers |> Map.add s.Guild.Id s

    let private matchMailbox (mm : MailboxMessage) =
        match mm with
        | NewMessage nm ->
            command.searchStatic nm
            |> bindCont command.searchCreator
            |> bindCont command.searchTemp
            |> runCont runCommand nm
        | MessageReaction mr ->
            reaction.searchServer mr
            |> bindCont reaction.searchTemp
            |> runCont runReaction mr
        | Task t -> t.Invoke &state

    let private processMail (inbox : MailboxProcessor<MailboxMessage>) =
        let rec msgLoop () =
            async {
                let! mm = inbox.Receive()
                matchMailbox mm
                return! msgLoop ()
            }
        msgLoop ()

    let private agent = MailboxProcessor.Start(processMail)

    let InitializeClient creatorFilters staticFilters =
        state.cmdCreatorFilters <- creatorFilters
        state.cmdStaticFilters <- staticFilters

    //once a message is handled, it returns
    let Receive (mm : MailboxMessage) = agent.Post mm

    let GetServer (guild : SocketGuild) : Server =
        state.Servers
        |> Map.tryFind guild.Id
        |> function
        | Some s -> s
        | None -> Server.create guild
