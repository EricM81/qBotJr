namespace qBotJr

open System
open System.Text
open Discord
open FSharp.Control.Tasks.V2
open qBotJr
open qBotJr.T
open helper
open discord

module qNew =
  [<Literal>]
  let _perm = UserPermission.Admin
  [<Literal>]
  let _name = "QNEW"

  module private T =

    [<Struct>] //val type
    type qNewArgs =
      {
        Server: Server
        Goo: GuildOO
        Count: string
        CategoryID: uint64 option
        Mode: string option
        AddPlayers: string list
        FilteredPlayers: PlayerHere list
        Errors: string list
        Captain: bool
        ShowMan: bool
      }
      static member create s g =
        {
          Server = s
          Goo = g
          Count = ""
          CategoryID = None
          AddPlayers = []
          FilteredPlayers= []
          Captain = false
          Mode = None
          Errors = []
          ShowMan = false
        }

    [<Struct>] //val type
    type qNewValid =
      {
        Server: Server
        Goo: GuildOO
        CategoryID: uint64
        Players: PlayerHere list
        Captain: PlayerHere option
        Mode: Mode option
      }

      static member create s g catID mode captain players =
        {qNewValid.Server = s; Goo = g; CategoryID = catID; Players = players; Captain = captain; Mode = mode}

    let getServerSettings (args: qNewArgs): qNewArgs =
      let settings = config.GetGuildSettings(args.Server.GuildID)
      { args with
          Count = settings.PlayersPerGame.ToString()
          CategoryID = settings.LobbiesCategory
          }

    let cmdStrToArgs (xs: CommandLineArgs list) (acc: qNewArgs): qNewArgs =
      let rec init (xs: CommandLineArgs list) (acc: qNewArgs): qNewArgs =
        match xs with
        | [] -> acc
        | x :: xs when x.Switch = Some 'C' -> init xs {acc with Captain = true}
        | x :: xs when x.Switch = Some 'I' && x.Values <> [] -> init xs {acc with Count = x.Values.Head}
        | x :: xs when x.Switch = Some 'P' && x.Values <> [] -> init xs {acc with AddPlayers = x.Values}
        | x :: xs when x.Switch = Some 'M' && x.Values <> [] -> init xs {acc with Mode = Some x.Values.Head}
        | x :: xs when x.Switch = Some '?' -> init xs {acc with ShowMan = true}
        | _ :: xs -> init xs acc
      init xs acc

  module private error =
    open T

    let inline createMsgFilters (args: qNewArgs) (callBack: MessageAction) =
      [
        Command.create "-C" _perm callBack reactDistrust
        Command.create "-I" _perm callBack reactDistrust
        Command.create "-P" _perm callBack reactDistrust
        Command.create "-M" _perm callBack reactDistrust
      ]
      |> MessageFilter.create args.Goo.GuildID (DateTimeOffset.Now.AddMinutes (5.0)) (Some args.Goo.User.Id)

    let printMan (args: qNewArgs): string =
      let sb = StringBuilder ()
      let a format = bprintfn sb format

      let printModes =
        if args.Server.Modes.Length > 0 then
          args.Server.Modes |> List.map (fun m -> m.Name) |> printCommaSeparatedList |> (+) "\n       "
        else ""

      a ">>> **Start a new lobby for the next gamers in the queue.**"
      a ""
      printErrors sb args.Errors
      a "```qNew -c -i 10 -p @thing1 @thing2 -m meme1"
      a ""
      a "-c Automatically add a captain "
      a "Current Value: %b" args.Captain
      a "    -- automatically search the queue for someone with the"
      a "       captain role"
      a "    -- play while someone you trust gets the next game ready"
      a ""
      a "-i Number of players"
      a "Current Value: #%s" args.Count
      a "    -- Defaults to the last used value."
      a "    -- Number of players the game accepts"
      a "    -- If it takes 10, say 10 and the bot will "
      a "       get the next 9 from the queue."
      a "    -- Yes, we save you a seat."
      a ""
      a "-p Players to manually add"
      printCommaSeparatedList args.AddPlayers |>
      a "Current Value: #%s"
      a "    ** if you cannot @ mention the user in this channel,"
      a "       do it in another channel **"
      a "       -- copy paste back here; they will not be pinged"
      a "       -- or run the partial command (\"-p @name\") anywhere"
      a "    -- let folks jump the line to play together"
      a "-m Run a custom game mode"
      (match args.Mode with | Some s -> s | None -> "None") |>
      a "Current Value: #%s"
      a "    -- user qMode to let players opt in for a game with special rules"
      a "    -- use the same \"name\" here to start the lobby with the meme squad"
      a "    -- current modes: %s" printModes
      a "```"

      sb.ToString ()

    let inline run msgCallback _ (args: qNewArgs) =
      printMan args |> sendMsg args.Goo.Channel |> ignore
      createMsgFilters args msgCallback
      |> client.AddMessageFilter
      None

  module private success =
    open T

    let printLobbyHeader (argsV: qNewValid): string =
      let sb = StringBuilder ()
      let a format = bprintfn sb format

      a ">>> **Get ready, you're on deck.**"
      argsV.Players |> List.map (fun p -> mentionUserID p.Player.ID) |> printCommaSeparatedList |>
      a "%s"
      a ""
      match argsV.Captain with
      | Some c ->
        a "**Your Captain is %s**" c.Player.Name
        a "```"
        a "Captains make sure the next game starts ASAP."
        a "    -- is everyone here?"
        a "    -- custom lobby in-game? matchmaking?"
        a "    -- user your noodle, make it happen cap'in"
      | None ->
        a "```"
        a "Lobby Commands (for Captains and Admins)"
      a ""
      a "qAFK    if someone doesn't check in, mark them afk"
      a "        and qAdd the next person"
      a "qKick   if someone wants to trade spots, qKick @thing1 and"
      a "        qAdd @thing2 (@thing1 will be in the next game)"
      a "qAdd    Gets next in line or someone specific"
      a "qClose  Close this Lobby Channel after the match"
      a "```"
      match argsV.Mode with
      | Some mode ->
        a "**This is a special game mode called %s.**" mode.Name
      | None -> ()

      sb.ToString ()

    let printValid (argsV: qNewValid): string =

      let captain = if argsV.Captain.IsSome then " -c" else ""
      let players =
          if argsV.Players.Length <> 0 then
            " -p " + (argsV.Players |> List.map (fun p -> p.Player.Name) |> printCommaSeparatedList )
          else
            ""
      let mode =
        match argsV.Mode with
        | Some m -> " -m " + m.Name
        | None -> ""

      sprintf "Current Setting:\nqNew -i %i%s%s%s\n\nReact to this message to run it again with the same parameters."
        argsV.Players.Length captain players mode

    let saveServerSettings (argsV:qNewValid) =
      let settings = config.GetGuildSettings(argsV.Goo.GuildID)
      let count = argsV.Players.Length
      if count <> settings.PlayersPerGame then
        { settings with
            PlayersPerGame = count }
        |> config.SetGuildSettings

    let createRtFilter (argsV: qNewValid) (msgID: uint64) (emoji: string) (callBack: ReactionAction) =
      [
        ReAction.create emoji callBack
      ]
      |> ReactionFilter.create argsV.Goo.GuildID msgID DateTimeOffset.MaxValue (Some argsV.Goo.User.Id)

    let createLobby (argsV: qNewValid) =
      let updateFun(t: TextChannelProperties) =
        t.CategoryId <-  argsV.CategoryID |> Nullable |> Optional
        t.PermissionOverwrites <-
          argsV.Players
          |> List.map (fun p -> p.Player.ID)
          |> perms.getLobbyPerms argsV.Goo.Guild |> Optional

      argsV.Goo.Guild.CreateTextChannelAsync(names.getRand(), (Action<_> updateFun), config.restClientOptions)

    let createAsyncTask (server: Server) : AsyncTask =
      AsyncTask
        (fun state ->
          state.Servers <- state.Servers |> Map.add server.GuildID server
        )

    let inline run _ rtCallback (argsV: qNewValid): Server option =
      task {
        //update server defaults
        saveServerSettings argsV
        //create lobby & set perms
        let! lobbyChl = createLobby argsV
        printLobbyHeader argsV |> sendMsg lobbyChl |> ignore
        //print success, seed re-action to rerun cmd
        let! successMsg = printValid argsV |> sendMsg argsV.Goo.Channel
        let emoji = emojis.GetRandomLetter()
        addReaction successMsg emoji |> ignore
        //add reaction filter
        createRtFilter argsV successMsg.Id emoji rtCallback
        |> client.AddReactionFilter
        //update server - player counts and new lobby
        argsV.Players |> List.iter (fun p -> p.GamesPlayed <- p.GamesPlayed + 1s)
        let lobby = Lobby.create lobbyChl argsV.Players
        let server = argsV.Server
        //async task to register new server state
        {server with
          Lobbies = lobby::server.Lobbies
          PlayerListIsDirty = true
          }
        |> createAsyncTask
        |> UpdateState
        |> client.Receive
      } |> ignore
      None

  module private validate =
    open T

    let validateShowMan (args: qNewArgs) =
      //Server -> GuildOO -> uint64 -> Mode option -> PlayerHere option -> PlayerHere list -> qNewValid
      match args.ShowMan with
      | false -> Ok (args, (qNewValid.create args.Server args.Goo))
      | true -> Error args

    let validateLobbyCat (args: qNewArgs) =
      match args.CategoryID with
      | Some id -> Ok id
      | None -> {args with Errors = "You must tell \"qBot\" where to create Lobbies"::args.Errors} |> Error

    let filterPlayersHere (args:qNewArgs) (modeOpt: Mode option): qNewArgs =
      let callerID = args.Goo.User.Id
      let isHere (p:PlayerHere) = p.isHere = true && p.isBanned = false && p.Player.ID <> callerID
      let isInMode (mode: Mode) (p:PlayerHere) =
        let playerID = p.Player.ID
        let rec matchMode (modePlayers: Player list) =
          match modePlayers with
          | [] -> false
          | x::xs -> if playerID = x.ID then true else matchMode xs
        isHere p && matchMode mode.Players
      let matchExp =
        match modeOpt with
        | Some mode -> isInMode mode
        | None -> isHere
      let sortExp p = p.GamesPlayed
      {args with
        FilteredPlayers =
          args.Server.PlayersHere
          |> List.filter matchExp
          |> List.sortBy sortExp }

    let validateMode (args: qNewArgs) =
      match args.Mode with
      | Some mode ->
        let modeUpper = mode.ToUpper()
        match (args.Server.Modes |> List.tryFind (fun m -> m.Name.ToUpper() = modeUpper)) with
        | Some mode' ->
          Some mode' |> Ok
        | None -> {args with Mode = None; Errors = "Mode not found; See \"current modes\""::args.Errors} |> Error
      | None -> None |> Ok

    let validateCaptain (args: qNewArgs) =
      let callerID = args.Goo.User.Id
      let matchExp p =
        p.isHere = true && // need to make sure someone is here
        p.isBanned = false && //that's not banned
        p.Role.HasFlag(UserPermission.Captain) && //with at least the captain permission
        p.Player.ID <> callerID  //that's not the person looking for a captain
      let captainOpt =
        match args.Captain with
        | true ->
          args.FilteredPlayers |> List.tryFind matchExp
        | _ -> None
      match args.Captain, captainOpt with
      | true, Some _ -> captainOpt |> Ok
      | true, None -> Error {args with Captain = false; Errors = "No captains are available"::args.Errors}
      | false, _ -> None |> Ok

    let validateCount (args: qNewArgs) =
      let mutable i = 0
      if Int32.TryParse(args.Count, &i) then
        let hereCount = args.FilteredPlayers.Length
        if i <= hereCount then
          args.FilteredPlayers |> List.take i |> Ok
        else
          Error {args with Errors = (sprintf "Need %i players, but only %i here" i hereCount)::args.Errors}
      else
        Error {args with Count = "10"; Errors = "Players Per Game Was Not Numeric"::args.Errors}

    let inline check (args: qNewArgs) =
      validateShowMan args
      |>> validateLobbyCat
      |+> (validateMode, filterPlayersHere)
      |>> validateCaptain
      |>> validateCount

  let inline private _run msgCallback rtCallback (args: T.qNewArgs) : Server option =
    validate.check args
    |> function
    | Ok (args, argsV) ->
      success.run (msgCallback args) (rtCallback args) argsV
    | Error args -> error.run (msgCallback args) (rtCallback args) args

  let rec private _rtRun (args: T.qNewArgs) (server: Server) (_: MessageReaction): Server option =
    {args with Server = server; Errors = []; ShowMan = false; FilteredPlayers = []}
    |> _run _msgRun _rtRun
  and private _msgRun (args: T.qNewArgs) (server: Server) (goo: GuildOO) (pm: ParsedMsg): Server option =
    {args with Server = server; Goo = goo; Errors = []; ShowMan = false; FilteredPlayers = []}
    |> T.cmdStrToArgs pm.ParsedArgs
    |> _run _msgRun _rtRun

  let Run (server: Server) (goo: GuildOO) (pm: ParsedMsg): Server option =
    let args = T.qNewArgs.create server goo |> T.getServerSettings
    _msgRun args server goo pm

  let Command = Command.create _name _perm Run reactDistrust
