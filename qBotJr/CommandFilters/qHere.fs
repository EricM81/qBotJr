namespace qBotJr

open System
open System.Text
open System.Threading.Tasks
open Discord
open Discord.Rest
open Discord.WebSocket
open qBotJr.T
open helper

module qHere =

    [<Struct>] //val type
    type private qHereArgs =
        {
            Server : Server
            Goo : GuildOO
            AnnounceID : uint64 option
            Ping : PingType option
            Errors : string list
        }
        static member create s g announcements ping =
            { qHereArgs.Server = s ; Goo = g ; Ping = ping ; AnnounceID = announcements ; Errors = [] }

    [<Struct>] //val type
    type private qHereValid =
        {
            Server : Server
            Goo : GuildOO
            Announcements : SocketTextChannel
            Ping : PingType
            Emoji : string
        }
        static member create s g announcements ping =
            { qHereValid.Server = s ; Goo = g ; Ping = ping ; Announcements = announcements ; Emoji = emojis.RaiseHands }

    let lastAnnounceChannel (server : Server) = config.GetGuildSettings(server.GuildID).AnnounceChannel

    let private updateAnnounceChannel (server : Server) (args : qHereArgs) =
        let cfg = config.GetGuildSettings server.GuildID
        if cfg.AnnounceChannel <> args.AnnounceID then
            { cfg with AnnounceChannel = args.AnnounceID } |> config.SetGuildSettings
        args

    let private printHeader (args : qHereValid) : string =
        let sb = StringBuilder()
        let a format = bprintfn sb format

        a "%s" <| discord.pingToString args.Ping
        a ">>> **React with %s to join the queue!**" args.Emoji
        a "```"
        a
            "You will get a ping in a new channel, made just for players in your match. You only have a few minutes to join before getting marked as afk, so please watch for the ping!"
        a "```"
        a "**Please, Un-React to the message if you step away!**"
        a "```You won't lose your place in line."
        a "I'll just skip you until you react agane!```"

        sb.ToString()

    let private printMan (args : qHereArgs) : string =

        let sb = StringBuilder()
        let a format = bprintfn sb format


        a ">>> **Post a message to a channel (-a) and ping @ everyone (-e), @ here (-h), or no one (-n).**"
        a ""
        printErrors sb args.Errors
        a "It's best to use a read-only, announcement style channel. Use the channel's permissions to determine who gets to play."
        a "```announcements = everyone, sub_announcements = subs, etc.```"
        a "Over time, people will leave.  You can re-run qHere for a fresh count."
        a "```This will not reset the \"games played\" stat."
        a "The bot remembers 'till it goes to sleep (1 hr of inactivity).```"

        a "```qHere -e|-h|-n -a #your_channel"
        a ""
        a "Pick one:"
        a "-e Ping @ everyone"
        a "-h Ping @ here"
        a "-n Ping no one, just post"
        a "   Current Value: #%s" <|
            match args.Ping with | Some p -> p.ToString(); | None -> "Nothing Selected"
        a ""
        a "-a Announcement channel."
        match bind discord.getChannelByID args.AnnounceID with
        | Some c ->
            a "   Current Value: #%s" c.Name
            a "   This will be used if you omit the -a, but "
            a "   you always have to specify who to ping."
        | None ->
            a "   Current Value: None "
            a "   Your last used value will be stored here, but you"
            a "   have to provide a channel on the first run. Make"
            a "   sure it is a text channel, not a voice or category."
        a "```"

        sb.ToString()

    let rec private initArgs (xs : CommandLineArgs list) (acc : qHereArgs) : qHereArgs =
        //example input
        //qhere
        //qhere -a <#544636678954811392>
        //qhere <#544636678954811392>
        //-a <#544636678954811392> -e -h
        match xs with
        | [] -> acc
        | x :: xs when x.Switch = Some 'E' -> initArgs xs { acc with Ping = Some PingType.Everyone }
        | x :: xs when x.Switch = Some 'H' -> initArgs xs { acc with Ping = Some PingType.Here }
        | x :: xs when x.Switch = Some 'N' -> initArgs xs { acc with Ping = Some PingType.NoOne }
        | x :: xs when x.Values <> [] ->
            discord.parseDiscoChannel x.Values.Head
            |> function
            | Some id -> { acc with AnnounceID = Some id }
            | _ -> acc
            |> initArgs xs
        //ignore invalid input?
        | _ -> initArgs xs acc

    let private updateHereList (server : Server) (mr : MessageReaction) : Server option =
        let user = mr.Goo.User
        server.PlayersHere
        |> List.tryFind (fun player -> player.Player.ID = user.Id)
        |> function
        | Some p ->
            p.isHere <- mr.IsAdd
            server.PlayerListIsDirty <- true
            None
        | None ->
            let p = PlayerHere.create user mr.IsAdd
            Some { server with PlayersHere = p :: server.PlayersHere ; PlayerListIsDirty = true }

    let private removeOldHereMsgFilter msgID (items : ReactionFilter list) =
        items |> List.filter (fun item -> msgID <> item.MessageID)

    let private successAsync (argsV : qHereValid) announceHeader (t : Task<RestUserMessage>) : Server option =
        async {
            let server = argsV.Server
            let! restMsg = t |> Async.AwaitTask
            let server' = { server with HereMsg = HereMessage.create restMsg argsV.Emoji announceHeader |> Some }

            AsyncTask(fun state ->
                //seed the reaction to say "I'm here"
                Emoji(argsV.Emoji) |> restMsg.AddReactionAsync |> ignore
                //if replacing a hereMsg, remove old reaction filter and reset everyone's isHere
                match server.HereMsg with
                | Some msg ->
                    state.rtServerFilters <- removeOldHereMsgFilter msg.MessageID state.rtServerFilters
                    server.PlayersHere |> List.iter (fun player -> player.isHere <- false)
                | None -> ()
                //create new hereMsg
                state.Servers <- state.Servers |> Map.add server.GuildID server'
                //register filter for one reactions
                [ ReAction.create argsV.Emoji updateHereList ]
                |> ReactionFilter.create server.GuildID restMsg.Id DateTimeOffset.MaxValue None
                |> client.AddServerReactionFiler)
            |> UpdateState
            |> client.Receive
        }
        |> Async.Start
        None

    let private successFun (argsV : qHereValid) : Server option =
        let announceHeader = printHeader argsV
        let restMsg = discord.sendMsg argsV.Announcements announceHeader
        match restMsg with
        | Some t -> successAsync argsV announceHeader t
        | None ->
            sprintf
                "Failed to send rest message to SocketTextChannel: %i in Guild %i"
                argsV.Announcements.Id
                argsV.Goo.GuildID
            |> logger.WriteLine
            None

    let private errorFun (args : qHereArgs) =
        printMan args |> discord.sendMsg args.Goo.Channel |> ignore
        None

    let private validateChannel (args : qHereArgs) =
        match args.AnnounceID with
        | Some id ->
            match discord.getChannelByID id with
            | Some gChl ->
                if (gChl :? SocketTextChannel) then
                    gChl :?> SocketTextChannel |> Ok
                else
                    Error { args with AnnounceID = None ; Errors = "Channel Is Not A Text Channel" :: args.Errors }
            | _ -> Error { args with AnnounceID = None ; Errors = "Channel Not Found" :: args.Errors }
        | None -> Error { args with Errors = "Channel Is Required" :: args.Errors }

    let private validatePing (args:qHereArgs) =
        match args.Ping with
        | Some p -> Ok p
        | None -> Error {args with Errors = "Type of Ping is Required"::args.Errors}

    let inline private validate (args : qHereArgs) =
        Ok(args, (qHereValid.create args.Server args.Goo))
        |>> validateChannel |>> validatePing
        |> function
            | Ok (_, argsV) -> Ok argsV
            | Error ex -> Error ex

    let Run (server : Server) (goo : GuildOO) (pm : ParsedMsg) : Server option =
        qHereArgs.create server goo (lastAnnounceChannel server) None
        |> initArgs pm.ParsedArgs
        |> updateAnnounceChannel server
        |> validate
        |> function
        | Ok argsV -> successFun argsV
        | Error args -> errorFun args

    let Command = Command.create "QHERE" UserPermission.Admin Run discord.reactDistrust
