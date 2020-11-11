namespace qBotJr

open System
open Discord.WebSocket
open FSharpx.Control
open qBotJr.T
open qBotJr.parser
open qBotJr.helper


//Threadsafe MailboxProcessor to filter commands and update state
module client =

    /// examples for maintaining state in F# are lacking, but I needs it
    ///
    /// the MailboxProcessor makes incoming messages single threaded, which gives
    /// thread safety but also creates a bottleneck.
    ///
    /// some fields on records held in the State obj are mutable, but they were
    /// chosen judiciously like marking something as dirty (akin to needing to redraw the UI)
    /// most are immutable and need to update the state field
    ///
    /// functions that do something with user input have 3 options :
    /// |> function
    /// | Done () -> ()   //Block the mailbox thread to quickly update mutable fields
    /// | Async a -> Async.Start a   //Run Async<unit> and update with an AsyncTask -> byref<State> -> ()
    /// | Server server' -> state.Servers <- state.Servers |> Map.add server'.Guild.Id server'
    ///         //Block the mailbox and update an entire record
    ///
    /// to keep byref<State> from every being handled by any other thread,
    /// ** state is a mutable value type **
    let mutable private state = State.create

    type private FoundMessage = Command * MessageFilter option
    type private FoundReaction = ReactionAction * ReactionFilter option

    /// these are you normal discord commands.  this bot uses 'q' as a prefix, but also allows for a
    /// temporary filter on any input.  for example, if someone runs qHere but forgets to specify a
    /// parameter, I can reply back that they need to specify -e to ping @everyone or -h for @here to
    /// keep them from having to retype an entire command
    module private command =

        let inline parseMsg (cmd : Command) (msg : SocketMessage) =
            parseInput cmd.PrefixUpper msg.Content |> ParsedMsg.create msg

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

    /// admins will learn and run commands, but I didn't want to force every user to have to do the same
    /// user actions, like signaling they want to play, are handled through reactions to announcement messages
    /// I also wanted the ability to let a command with insufficient info to accept a reaction for a missing param
    /// which uses temporary filters (just like temp command filter, i.e. react ⚽ for -e, 🏈 for -h)
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

    let inline private add1ServerTTL (server : Server) = server.TTL <- DateTimeOffset.Now.AddHours(1.0)
    let inline private expireMsgFilterTTL (filter : MessageFilter) = filter.TTL <- DateTimeOffset.MinValue
    let inline private expireRtFilterTTL (filter : ReactionFilter) = filter.TTL <- DateTimeOffset.MinValue

    let private getServer (guild : SocketGuild) =
        state.Servers
        |> Map.tryFind guild.Id
        |> function
        | Some s -> s
        | None -> Server.create guild

    let inline private execCmd (nm : NewMessage) (cmd : Command) server =
        if cmd.RequiredPerm = UserPermission.Admin then add1ServerTTL server
        let pm = command.parseMsg cmd nm.Message
        let goo = nm.Goo
        if (getPerm goo.User) >= cmd.RequiredPerm then cmd.PermSuccess pm goo server else cmd.PermFailure pm goo server

    let inline private execRt mr action server  =
        action mr server

    let inline private run expireFun execFun x guild (y, filter) =
        match filter with
        | None -> ()
        | Some filter' -> expireFun filter'
        let server = getServer guild
        execFun x y server
        |> function
        | Done () -> ()
        | Async a -> Async.Start a
        | Server server' -> state.Servers <- state.Servers |> Map.add server'.Guild.Id server'

    let private matchMailbox (mm : MailboxMessage) =
        match mm with
        | NewMessage nm ->
            command.searchStatic nm
            |> bindCont command.searchCreator
            |> bindCont command.searchTemp
            |> runCont run expireMsgFilterTTL execCmd nm nm.Goo.Guild
        | MessageReaction mr ->
            reaction.searchServer mr
            |> bindCont reaction.searchTemp
            |> runCont run expireRtFilterTTL execRt mr mr.Goo.Guild
        | UpdateState t -> t.Invoke &state

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

