namespace qBotJr
//
//open System
//open System.Text
//open System.Threading.Tasks
//open Discord
//open Discord.Rest
//open Discord.WebSocket
//open qBotJr.T
//open helper
//open discord
//
//module qMode =
//  [<Literal>]
//  let _perm = UserPermission.Admin
//  [<Literal>]
//  let _name = "QNEW"
//
//  [<Struct>] //val type
//  type private qNewArgs =
//    {
//      Server: Server
//      Goo: GuildOO
//      AnnChnl: string option
//      Ping: PingType option
//      Errors: string list
//      ShowMan: bool
//    }
//
//    static member create s g ping announcements =
//      {qNewArgs.Server = s; Goo = g; Ping = ping; AnnChnl = announcements; Errors = []; ShowMan = false}
//
//  [<Struct>] //val type
//  type private qNewValid =
//    {
//      Server: Server
//      Goo: GuildOO
//      AnnChnl: SocketTextChannel
//      Ping: PingType
//      Emoji: string
//    }
//
//    static member create s g ping announcements =
//      {qNewValid.Server = s; Goo = g; Ping = ping; AnnChnl = announcements; Emoji = emojis.RaiseHands}
//
//  let getServerSettings (args: qNewArgs): qNewArgs =
//    { args with
//        Count = config.GetGuildSettings(args.Server.GuildID).PlayersPerGame.ToString()}
//
//  let saveServerSettings (argsV:qNewValid) =
//    let settings = config.GetGuildSettings(argsV.Goo.GuildID)
//    { settings with
//        PlayersPerGame = argsV.Count }
//    |> config.SetGuildSettings
//
//  let inline private createFilters (args: qNewArgs) (userID: uint64) (callBack: MessageAction) =
//    [
//      Command.create "" _perm callBack reactDistrust
//    ]
//    |> MessageFilter.create args.Goo.Channel.Id (DateTimeOffset.Now.AddMinutes (5.0)) (Some args.Goo.User.Id)
//  let private printMan (args: qHereArgs): string =
//
//    let sb = StringBuilder ()
//    let a format = bprintfn sb format
//
//
//    a ">>> **Post a message to a channel (-a) and ping @ everyone (-e), @ here (-h), or no one (-n).**"
//    a ""
//    printErrors sb args.Errors
//    a
//      "It's best to use a read-only, announcement style channel. Use the channel's permissions to determine who gets to play."
//    a "```announcements = everyone, sub_announcements = subs, etc.```"
//    a "Over time, people will leave.  You can re-run qHere for a fresh count."
//    a "```This will not reset the \"games played\" stat."
//    a "The bot remembers 'till it goes to sleep (1 hr of inactivity).```"
//
//    a "```qHere -e|-h|-n -a #your_channel"
//    a ""
//    a "Pick one:"
//    a "-e Ping @ everyone"
//    a "-h Ping @ here"
//    a "-n Ping no one, just post"
//    a "Current Value: #%s"
//    <| match args.Ping with
//       | Some p -> p.ToString ()
//       | None -> "Nothing Selected"
//    a ""
//    a "-a Announcement channel."
//    match bind getChannelByStrID args.AnnChnl with
//    | Some c ->
//        a "Current Value: #%s" c.Name
//        a "    -- this will be used if you omit the -a, but "
//        a "       you always have to specify who to ping."
//    | None ->
//        a "Current Value: None"
//        a "    -- Your last used value will be stored here, but "
//        a "       you have to provide a channel on the first run."
//        a "    -- Make sure it is a text channel, not a voice "
//        a "       or channel category."
//    a "```"
//
//    sb.ToString ()
//
//  let private printValid (argsV: qHereValid): string =
//    sprintf "Current Setting:\nqHere %s -a %s" (argsV.Ping.ToString ()) argsV.AnnChnl.Name
//
//   let private cmdStrToArgs (xs: CommandLineArgs list) (acc: qHereArgs): qHereArgs =
//    let rec init (xs: CommandLineArgs list) (acc: qHereArgs): qHereArgs =
//      match xs with
//      | [] -> acc
//      | x :: xs when x.Switch = Some 'E' -> init xs {acc with Ping = Some PingType.Everyone}
//      | x :: xs when x.Switch = Some 'H' -> init xs {acc with Ping = Some PingType.Here}
//      | x :: xs when x.Switch = Some 'N' -> init xs {acc with Ping = Some PingType.NoOne}
//      | x :: xs when x.Switch = Some 'A' && x.Values <> [] -> init xs {acc with AnnChnl = Some x.Values.Head}
//      | x :: xs when x.Switch = None && x.Values <> [] -> init xs {acc with AnnChnl = Some x.Values.Head}
//      | x :: xs when x.Switch = Some '?' -> init xs {acc with ShowMan = true}
//      | _ :: xs -> init xs acc
//
//    init xs acc
//  let private successFun (argsV: qHereValid): Server option =
//    updateAnnChl argsV
//    let announceHeader = printHeader argsV
//    let restMsg = sendMsg argsV.AnnChnl announceHeader
//    match restMsg with
//    | Some t ->
//        printValid argsV |> sendMsg argsV.Goo.Channel |> ignore
//        successAsync argsV announceHeader t
//    | None -> None
//
//  let private errorFun (args: qHereArgs) filter =
//    printMan args |> sendMsg args.Goo.Channel |> ignore
//    client.AddMessageFilter filter
//    None
//
//  let private validatePing (args: qHereArgs) =
//    match args.Ping with
//    | Some p -> Ok p
//    | None -> Error {args with Errors = "Type of Ping is Required" :: args.Errors}
//
//  let inline private validate (args: qHereArgs) =
//    if args.ShowMan then Error args else Ok (args, (qHereValid.create args.Server args.Goo))
//    |>> validatePing
//    |>> validateChannel
//    |> function
//    | Ok (_, argsV) -> Ok argsV
//    | Error ex -> Error ex
//
//  let rec private _run (pm: ParsedMsg) (args: qHereArgs) =
//    cmdStrToArgs pm.ParsedArgs args
//    |> validate
//    |> function
//    | Ok argsV -> successFun argsV
//    | Error args ->
//        (fun (server: Server) (goo: GuildOO) (pm: ParsedMsg) ->
//          {args with Server = server; Goo = goo; Errors = []; ShowMan = false} |> _run pm)
//        |> createFilters args.Goo.GuildID args.Goo.User.Id
//        |> errorFun args
//
//  let Run (server: Server) (goo: GuildOO) (pm: ParsedMsg): Server option =
//    lastAnnChl server |> qHereArgs.create server goo None |> _run pm
//
//  let Command = Command.create _name _perm Run reactDistrust
