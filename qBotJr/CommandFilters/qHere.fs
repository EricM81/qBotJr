namespace qBotJr
open System.Net.NetworkInformation
open System.Text
open System.Threading.Channels
open Discord.WebSocket
open qBotJr.T
open helper



module qHere =

    let lastAnnounceChannel (guild : SocketGuild) =
        config.GetGuildSettings(guild.Id).AnnounceChannel

    let updateAnnounceChannel (server : Server) (args : qHereArgs) =
        let cfg = config.GetGuildSettings server.Guild.Id
        if cfg.AnnounceChannel <> args.AnnounceID then
            {cfg with AnnounceChannel = args.AnnounceID} |> config.SetGuildSettings
        args

    let printAnnouncement (args : qHereArgsValidated) : string =
        let sb = StringBuilder()
        let a format = discord.bprintfn sb format


        a "%s" <| discord.pingToString args.Ping
        a ">>>React here with %s to join the queue!" emojis.RaiseHands
        a ""
        a "You will get a ping in a new channel, made just for players in your match. You only have a few minutes to join before getting marked as afk, so please watch for the ping!"
        a ""
        a "**Please, Un-React to the message if you step away!**"
        a "```You won't lose your place in line."
        a "I'll just skip you until you react agane!```"
        a ""

        sb.ToString()

    let printMan (args : qHereArgs) : string =

        let sb = StringBuilder()
        let a format = discord.bprintfn sb format

        a ">>> __Post a message to a channel (-a) and ping @ everyone (-e), @ here (-h), or no one (-n).__"
        a ""
        a "It's best to use a read-only, announcement style channel. Use the channel's permission determine who gets to play."
        a "```announcements = everyone, sub_announcements = subs, etc.```"
        a "Over time, people will leave.  You can re-run qHere for a fresh count."
        a "```This will not reset the \"games played\" stat."
        a "The bot remembers 'till it goes to sleep (1 hr of inactivity).```"

        a "```qHere -e|-h|-n -a #your_channel"
        a ""
        a "Pick One:"
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
            a "   Current Value: None"
            a "   Your last used value will be stored here, but"
            a "   you have to provide a channel on the first run."
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

    let inline validate successFun errorFun (server : Server) goo (args : qHereArgs) =
        match args.AnnounceID, args.Ping with
        | Some channel, Some ping -> qHereArgsValidated.create channel ping |> successFun server goo
        | _ -> errorFun server goo args

    let successFun (server : Server) (goo : GuildOO) (args : qHereArgsValidated) : ActionResult =
        Done()

    let errorFun (server : Server) (goo : GuildOO) (args : qHereArgs) : ActionResult =
        Done()

    let Run (server : Server) (goo : GuildOO) (pm : ParsedMsg) : ActionResult =
        qHereArgs.create (lastAnnounceChannel goo.Guild) None
            |> initArgs pm.ParsedArgs
            |> updateAnnounceChannel server
            |> validate successFun errorFun server goo

    let Command = Command.create "QHERE" UserPermission.Admin Run discord.reactDistrust
