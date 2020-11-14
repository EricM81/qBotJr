namespace qBotJr
open System
open System.Text
open System.Threading.Tasks
open Discord
open Discord.Rest
open Discord.WebSocket
open FSharpx.Control
open qBotJr.T
open helper

module qHere =

    let lastAnnounceChannel (server : Server) =
        config.GetGuildSettings(server.GuildID).AnnounceChannel

    let updateAnnounceChannel (server : Server) (args : qHereArgs) =
        let cfg = config.GetGuildSettings server.GuildID
        if cfg.AnnounceChannel <> args.AnnounceID then
            {cfg with AnnounceChannel = args.AnnounceID} |> config.SetGuildSettings
        args

    let printAnnounceHeader (args : qHereArgsValidated) : string =
        let sb = StringBuilder()
        let a format = bprintfn sb format

        a "%s" <| discord.pingToString args.Ping
        a ">>> React here with %s to join the queue!" args.Emoji
        a ""
        a "You will get a ping in a new channel, made just for players in your match. You only have a few minutes to join before getting marked as afk, so please watch for the ping!"
        a ""
        a "**Please, Un-React to the message if you step away!**"
        a "```You won't lose your place in line."
        a "I'll just skip you until you react agane!```"

        sb.ToString()

    let printMan (args : qHereArgs) : string =

        let sb = StringBuilder()
        let a format = bprintfn sb format

        a ">>> __Post a message to a channel (-a) and ping @ everyone (-e), @ here (-h), or no one (-n).__"
        a ""
        a "It's best to use a read-only, announcement style channel. Use the channel's permissions to determine who gets to play."
        a "```announcements = everyone, sub_announcements = subs, etc.```"
        a "Over time, people will leave.  You can re-run qHere for a fresh count."
        a "```This will not reset the \"games played\" stat."
        a "The bot remembers 'till it goes to sleep (1 hr of inactivity).```"

        a "```qHere -e|-h|-n -a #your_channel"
        a ""
        match args.Ping with
        | Some p ->
            a "Pick one: (currently: %A)" p
        | None ->
            a "Pick one: (this is a required field)"
        a "-e Ping @ everyone"
        a "-h Ping @ here"
        a "-n Ping no one, just post"
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

    let rec initArgs (xs : CommandLineArgs list) (acc : qHereArgs) : qHereArgs =
        //example input
        //qhere
        //qhere -a <#544636678954811392>
        //qhere <#544636678954811392>
        //-a <#544636678954811392> -e -h
        match xs with
        | [] -> acc
        | x::xs when x.Switch = Some 'E' -> initArgs xs {acc with Ping = Some PingType.Everyone}
        | x::xs when x.Switch = Some 'H' -> initArgs xs {acc with Ping = Some PingType.Here}
        | x::xs when x.Switch = Some 'N' -> initArgs xs {acc with Ping = Some PingType.NoOne}
        | x::xs when x.Values <> [] ->
            discord.parseDiscoChannel x.Values.Head
            |> function
            | Some id -> {acc with AnnounceID = Some id}
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
            Some {server with PlayersHere = p::server.PlayersHere; PlayerListIsDirty = true}

    let private removeOldHereMsgFilter msgID (items : ReactionFilter list) =
        items |> List.filter (fun item -> msgID <> item.MessageID)

    let successAsync (server : Server) (_ : GuildOO) (args : qHereArgsValidated) announceHeader (t : Task<RestUserMessage>) : Server option =
        async {
            let! restMsg = t |> Async.AwaitTask
            let server' = {server with HereMsg = HereMessage.create restMsg args.Emoji announceHeader |> Some }
            AsyncTask(fun state ->
                //seed the reaction to say "I'm here"
                Emoji(args.Emoji) |> restMsg.AddReactionAsync |> ignore
                //if replacing a hereMsg, remove old reaction filter and reset everyone's isHere
                match server.HereMsg with
                | Some msg ->
                    state.rtServerFilters <- removeOldHereMsgFilter msg.MessageID state.rtServerFilters
                    server.PlayersHere |> List.iter (fun player -> player.isHere <- false)
                | None -> ()
                //create new hereMsg
                state.Servers <- state.Servers |> Map.add server.GuildID server'
                //register filter for one reactions
                [ ReAction.create args.Emoji updateHereList ]
                |> ReactionFilter.create server.GuildID restMsg.Id DateTimeOffset.MaxValue None
                |> client.AddServerReactionFiler
                )
            |> UpdateState |> client.Receive
        } |> Async.Start
        None

    let successFun (server : Server) (goo : GuildOO) (args : qHereArgsValidated) : Server option =
        let announceHeader = printAnnounceHeader args
        let restMsg = discord.sendMsg args.Announcements announceHeader
        match restMsg with
        | Some t ->
            successAsync server goo args announceHeader t
        | None -> //should never happen
            sprintf "Failed to send rest message to SocketTextChannel: %i in Guild %i" args.Announcements.Id goo.GuildID
            |> logger.WriteLine
            None

    let errorFun (goo : GuildOO) (args : qHereArgs)  =
        printMan args
        |> discord.sendMsg goo.Channel
        |> ignore
        None

    let validOptions (args : qHereArgs) : Validate<(uint64 * PingType), qHereArgs> =
         match args.AnnounceID, args.Ping with
         | Some channel, Some ping -> Success (channel, ping)
         | _ -> Fail args

    let validChannel (args : qHereArgs) (channelID : uint64, ping : PingType) : Validate<(SocketGuildChannel * PingType), qHereArgs> =
        match discord.getChannelByID channelID with
        | Some gChannel -> Success (gChannel, ping)
        | _ -> {args with AnnounceID = None} |> Fail

    let validTextChannel (args : qHereArgs) (gChannel : SocketGuildChannel, ping : PingType) : Validate<qHereArgsValidated, qHereArgs> =
        match gChannel with
        | :? SocketTextChannel as x -> qHereArgsValidated.create x ping emojis.RaiseHands |> Success
        | _ -> {args with AnnounceID = None} |> Fail

    let inline validate (server : Server) (goo : GuildOO) (args : qHereArgs) =
        validOptions args
        |> bindValid validChannel args
        |> bindValid validTextChannel args
        |> function
        | Success argsValid -> successFun server goo argsValid
        | Fail args -> errorFun goo args

    let Run (server : Server) (goo : GuildOO) (pm : ParsedMsg) : Server option =
        qHereArgs.create (lastAnnounceChannel server) None
        |> initArgs pm.ParsedArgs
        |> updateAnnounceChannel server
        |> validate server goo

    let Command = Command.create "QHERE" UserPermission.Admin Run discord.reactDistrust
