namespace qBotJr

open System
open System.Text
open Discord
open Discord.WebSocket
open qBotJr
open qBotJr.T
open helper
open discord

module qMode =
  [<Literal>]
  let _perm = UserPermission.Admin

  [<Literal>]
  let _name = "QMODE"

  module T =

    [<Struct>] //val type
    type qModeArgs =
      {
        Server: Server
        Goo: GuildOO
        AnnChnl: string option
        Ping: PingType option
        Errors: string list
        ShowMan: bool
      }
      static member create s g =
        {qModeArgs.Server = s; Goo = g; Ping = None; AnnChnl = None; Errors = []; ShowMan = false}

    [<Struct>] //val type
    type qModeValid =
      {
        Server: Server
        Goo: GuildOO
        AnnChnl: SocketTextChannel
        Ping: PingType
        Emoji: string
        Description: string
      }
      static member create s g desc ping announcements =
        {qModeValid.Server = s; Goo = g; Description = desc; Ping = ping; AnnChnl = announcements; Emoji = emojis.RaiseHands}

    let cmdStrToArgs (xs: CommandLineArgs list) (acc: qModeArgs): qModeArgs =
      let rec init (xs: CommandLineArgs list) (acc: qModeArgs): qModeArgs =
        match xs with
        | [] -> acc
        | x :: xs when x.Switch = Some 'E' -> init xs {acc with Ping = Some PingType.Everyone}
        | x :: xs when x.Switch = Some 'H' -> init xs {acc with Ping = Some PingType.Here}
        | x :: xs when x.Switch = Some 'N' -> init xs {acc with Ping = Some PingType.NoOne}
        | x :: xs when x.Switch = Some 'A' && x.Values <> [] -> init xs {acc with AnnChnl = Some x.Values.Head}
        | x :: xs when x.Switch = None && x.Values <> [] -> init xs {acc with AnnChnl = Some x.Values.Head}
        | x :: xs when x.Switch = Some '?' -> init xs {acc with ShowMan = true}
        | _ :: xs -> init xs acc

      init xs acc

    let getServerSettings (args: qModeArgs) =
      let x = config.GetGuildSettings(args.Server.GuildID).AnnounceChannel
      {args with
        AnnChnl =
          match bind getChannelByID x with
          | Some chl -> chl.Name |> Some
          | _ -> None
          }

  module private error =
    open T

    let inline private createMsgFilters (args: qModeArgs) (callBack: MessageAction) =
      [
        Command.create "-E" _perm callBack reactDistrust
        Command.create "-H" _perm callBack reactDistrust
        Command.create "-N" _perm callBack reactDistrust
        Command.create "-A" _perm callBack reactDistrust
      ]
      |> MessageFilter.create args.Goo.GuildID (DateTimeOffset.Now.AddMinutes (5.0)) (Some args.Goo.User.Id)

    let  printMan (args: qModeArgs): string =

      let sb = StringBuilder ()
      let a format = bprintfn sb format


      a ">>> **Post a message to a channel (-a) and ping @ everyone (-e), @ here (-h), or no one (-n).**"
      a ""
      printErrors sb args.Errors
      a
        "It's best to use a read-only, announcement style channel. Use the channel's permissions to determine who gets to play."
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
      a "Current Value: #%s"
      <| match args.Ping with
         | Some p -> p.ToString ()
         | None -> "Nothing Selected"
      a ""
      a "-a Announcement channel."
      match bind getChannelByStrID args.AnnChnl with
      | Some c ->
          a "Current Value: #%s" c.Name
          a "    -- this will be used if you omit the -a, but "
          a "       you always have to specify who to ping."
      | None ->
          a "Current Value: None"
          a "    -- Your last used value will be stored here, but "
          a "       you have to provide a channel on the first run."
          a "    -- Make sure it is a text channel, not a voice "
          a "       or channel category."
      a "```"

      sb.ToString ()

    let inline run msgCallback _ (args: qModeArgs) =
      printMan args |> sendMsg args.Goo.Channel |> ignore
      createMsgFilters args msgCallback
      |> client.AddMessageFilter
      None

  module private success =
    open T

    let saveServerSettings (argsV: qModeValid) =
      let cfg = config.GetGuildSettings argsV.Goo.GuildID
      let newVal = Some argsV.AnnChnl.Id
      if cfg.AnnounceChannel <> newVal then
        {cfg with AnnounceChannel = newVal} |> config.SetGuildSettings

    let printHeader (argsV: qModeValid): string =
      let sb = StringBuilder ()
      let a format = bprintfn sb format

      a "%s" <| pingToString argsV.Ping
      a ">>> **React with %s to sign up for a special game mode!**" argsV.Emoji
      a "```"
      a "%s" argsV.Description
      a "```"

      sb.ToString ()

    let printValid (argsV: qModeValid): string =
      sprintf "Current Setting:\nqMode %s -a %s -d \"%s\"" (argsV.Ping.ToString ()) argsV.AnnChnl.Name argsV.Description

    let updateHereList (modeID: uint64) (server: Server) (mr: MessageReaction): Server option =
      setPlayerMode server modeID mr.Goo.User mr.IsAdd |> Some

    let createAsyncTask (restMsg: IUserMessage) (argsV: qModeValid) : AsyncTask =
      let server = argsV.Server
      AsyncTask (fun state ->
          //seed the reaction to say "I'm here"
          addReaction restMsg argsV.Emoji |> ignore

          state.Servers <- state.Servers |> Map.add server.GuildID server
          //register filter for one reactions
          [ReAction.create argsV.Emoji (updateHereList restMsg.Id)]
          |> ReactionFilter.create server.GuildID restMsg.Id DateTimeOffset.MaxValue None
          |> client.AddServerReactionFilter)

    let inline run _ _ (argsV: qModeValid): Server option =
      async {
        saveServerSettings argsV
        let announceHeader = printHeader argsV
        let! restMsg = sendMsg argsV.AnnChnl announceHeader |> Async.AwaitTask
        {argsV with
          Server =
            {argsV.Server with HereMsg = HereMessage.create restMsg argsV.Emoji announceHeader |> Some}
        }
        |> createAsyncTask restMsg
        |> UpdateState
        |> client.Receive

        }
      |> Async.Start
      None

  module private validate =
    open T

    let validateName (args:qModeArgs) =
      //unique name
      ()
    let validateShowMan (args: qModeArgs) =
      match args.ShowMan with
      | false -> Ok (args, (qModeValid.create args.Server args.Goo))
      | true -> Error args

    let validateChannel (args: qModeArgs) =
      match args.AnnChnl with
      | Some str ->
          parseDiscoChannel str
          |> bind getChannelByID
          |> function
          | Some gChl ->
              match tryCastTextChannel gChl with
              | Some tChl -> Ok tChl
              | _ -> Error {args with AnnChnl = None; Errors = "Channel Is Not A Text Channel" :: args.Errors}
          | _ -> Error {args with AnnChnl = None; Errors = "Channel Not Found" :: args.Errors}
      | None -> Error {args with Errors = "Channel Is Required" :: args.Errors}

    let validatePing (args: qModeArgs) =
      match args.Ping with
      | Some p -> Ok p
      | None -> Error {args with Errors = "Type of Ping is Required" :: args.Errors}

    let check (args: qModeArgs) =
      validateShowMan args
      |>> validatePing
      |>> validateChannel

  let inline private _run msgCallback rtCallback (args: T.qModeArgs) : Server option =
    validate.check args
    |> function
    | Ok (args, argsV) ->
      success.run (msgCallback args) (rtCallback args) argsV
    | Error args ->
      error.run (msgCallback args) (rtCallback args) args

  let rec private _rtRun (args: T.qModeArgs) (server: Server) (_: MessageReaction): Server option =
    {args with Server = server; Errors = []; ShowMan = false}
    |> _run _msgRun _rtRun
  and private _msgRun (args: T.qModeArgs) (server: Server) (goo: GuildOO) (pm: ParsedMsg): Server option =
    {args with Server = server; Goo = goo; Errors = []; ShowMan = false}
    |> T.cmdStrToArgs pm.ParsedArgs
    |> _run _msgRun _rtRun

  let Run (server: Server) (goo: GuildOO) (pm: ParsedMsg): Server option =
    let args = T.qModeArgs.create server goo |> T.getServerSettings
    _msgRun args server goo pm

  let Command = Command.create _name _perm Run reactDistrust
