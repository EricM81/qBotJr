namespace qBotJr

open System
open Discord.WebSocket
open System.Text
open discord
open qBotJr.T
open helper

module qBot =
  //admin roles
  //capt roles
  //lobbies cat
  [<Literal>]
  let _perm = UserPermission.Admin

  [<Literal>]
  let _name = "QNEW"

  [<Struct>] //val type
  type private qBotArgs =
    {
      Server: Server
      Goo: GuildOO
      AdminRoles: string list
      CapRoles: string list
      LobbyCat: string option
      Errors: string list
      ShowMan: bool
    }

    static member create s g admin caps cat =
      {qBotArgs.Server = s; Goo = g; AdminRoles = admin; CapRoles = caps; LobbyCat = cat; Errors = []; ShowMan = false}

  [<Struct>] //val type
  type private qBotValid =
    {
      Server: Server
      Goo: GuildOO
      AdminRoles: SocketRole list
      CaptainRoles: SocketRole list
      LobbyCategory: SocketCategoryChannel
    }

    static member create s goo admins captains cat =
      {qBotValid.Server = s; Goo = goo; AdminRoles = admins; CaptainRoles = captains; LobbyCategory = cat}

  let inline private createFilters (args: qBotArgs) (callBack: MessageAction) =
    [
      Command.create "-A" _perm callBack reactDistrust
      Command.create "-C" _perm callBack reactDistrust
      Command.create "-L" _perm callBack reactDistrust
    ]
    |> MessageFilter.create args.Goo.Channel.Id (DateTimeOffset.Now.AddMinutes (5.0)) (Some args.Goo.User.Id)

  let private printMan (args: qBotArgs): string =
    let sb = StringBuilder ()
    let a format = bprintfn sb format


    let printRoles (roles: string list) =
      let rec print firstRun roles acc =
        match firstRun, roles with
        | _, [] -> acc
        | true, r :: rs -> acc + r |> print false rs
        | false, r :: rs -> ", " + r |> print false rs

      print true roles ""

    a ">>> **Configure Your Server's Settings**"
    a ""
    printErrors sb args.Errors
    a "```qBot -a @admins -c @mods @sub_carries -L \"lit category name\""
    a ""
    a "-a  @Roles (in addition to builtin) that run admin commands:"
    printRoles args.AdminRoles |> a "Current Value: %s"
    a "    -- qHere, qNew, qMode, qSet, qBan"
    a ""
    a "-c  @Roles that setup your next game while you play"
    printRoles args.CapRoles |> a "Current Value: %s"
    a "    -- create private match in game"
    a "    -- do matchmaking, assign teams"
    a "    -- qAFK, qKick, qAdd, qClose"
    a ""
    a "-L  #channel_category where the bot makes lobbies"
    match (bind (getCategoryByName args.Goo.Guild) args.LobbyCat) with
    | Some chl -> quoteEscape chl.Name
    | _ -> "None"
    |> a "Current Value: %s"
    a "    -- the bot does not modify existing users or channels"
    a "    -- the bot creates a new channel, grants the players"
    a "       permission to the channel, and deletes it on qClose"
    a "    Valid Values:"
    printCategoryNames args.Goo.Guild sb
    a "```"
    a "You can use the full command, but I'm also listening for a single option, i.e.:"
    a "-a @admin @mod"
    sb.ToString ()

  let private printValid (argsV: qBotValid): string =
    let rec printRoles (firstRun: bool) (roles: string list) (acc: string) =
      match firstRun, roles with
      | _, [] -> acc
      | true, r :: rs -> acc + r |> printRoles false rs
      | false, r :: rs -> acc + ", " + r |> printRoles false rs

    sprintf "Current Setting:\nqBot -a %s -c %s -L %s" (printRoles true (argsV.AdminRoles |> List.map socketRoleToStrId) "")
      (printRoles true (argsV.CaptainRoles |> List.map socketRoleToStrId) "") (quoteEscape argsV.LobbyCategory.Name)

  let private successFun (argsV: qBotValid) =
    let gSettings = config.GetGuildSettings argsV.Server.GuildID
    let aRoles = argsV.AdminRoles |> List.map (fun r -> r.Id)
    let cRoles = argsV.CaptainRoles |> List.map (fun r -> r.Id)
    {gSettings with AdminRoles = aRoles; CaptainRoles = cRoles; LobbiesCategory = Some argsV.LobbyCategory.Id}
    |> config.SetGuildSettings

    printValid argsV |> sendMsg argsV.Goo.Channel |> ignore
    None

  let private errorFun (args: qBotArgs) filter =
    //-a -c -l
    printMan args |> sendMsg args.Goo.Channel |> ignore
    client.AddMessageFilter filter
    None

  let private cmdStrToArgs (cmdArgs: CommandLineArgs list) (configArgs: qBotArgs): qBotArgs =
    let rec init (xs: CommandLineArgs list) (acc: qBotArgs) =
      match xs with
      | [] -> acc
      | x :: xs when x.Switch = Some 'A' -> init xs {acc with qBotArgs.AdminRoles = x.Values}
      | x :: xs when x.Switch = Some 'C' -> init xs {acc with qBotArgs.CapRoles = x.Values}
      | x :: xs when x.Switch = Some 'L' && x.Values <> [] -> init xs {acc with qBotArgs.LobbyCat = Some x.Values.Head}
      | x :: xs when x.Switch = Some '?' -> init xs {acc with qBotArgs.ShowMan = true}
      | _ :: xs -> init xs acc

    init cmdArgs configArgs

  let private validateAdmins (args: qBotArgs) =
    match validateRoles args.Goo.Guild args.AdminRoles with
    | Ok srList -> Ok srList
    | Error (strList, errList) -> Error ({args with AdminRoles = strList; Errors = List.concat [errList; args.Errors]})

  let private validateCaptains (args: qBotArgs) =
    match validateRoles args.Goo.Guild args.CapRoles with
    | Ok srList -> Ok srList
    | Error (strList, errList) -> Error ({args with CapRoles = strList; Errors = List.concat [errList; args.Errors]})

  let private validateCategory (args: qBotArgs) =
    match args.LobbyCat with
    | Some s ->
        match getCategoryByName args.Goo.Guild s with
        | Some catV -> Ok catV
        | None -> Error ({args with LobbyCat = None; Errors = ("Invalid Category Name: " + s) :: args.Errors})
    | None -> Error ({args with LobbyCat = None; Errors = "Category Is Required" :: args.Errors})

  let private validate (args: qBotArgs) =
    if args.ShowMan then Error args else Ok (args, (qBotValid.create args.Server args.Goo))
    |>> validateAdmins
    |>> validateCaptains
    |>> validateCategory
    |> function
    | Ok (_, argsV) -> Ok (argsV)
    | Error ex -> Error ex

  let rec private _run (pm: ParsedMsg) (args: qBotArgs) =
    cmdStrToArgs pm.ParsedArgs args
    |> validate
    |> function
    | Ok argsV -> successFun argsV
    | Error args ->
        (fun (server: Server) (goo: GuildOO) (pm: ParsedMsg) ->
          {args with Server = server; Goo = goo; Errors = []; ShowMan = false} |> _run pm)
        |> createFilters args
        |> errorFun args

  let Run (server: Server) (goo: GuildOO) (pm: ParsedMsg): Server option =
    let settings = config.GetGuildSettings goo.Channel.Guild.Id
    let adminRoles = getRolesByIDs goo.Channel.Guild settings.AdminRoles |> List.map socketRoleToStrId
    let captainRoles = getRolesByIDs goo.Channel.Guild settings.CaptainRoles |> List.map socketRoleToStrId
    let lobbiesCat = settings.LobbiesCategory |> bind getCategoryById |> bind (fun cat -> Some cat.Name)

    qBotArgs.create server goo adminRoles captainRoles lobbiesCat |> _run pm

  let Command = Command.create _name _perm Run reactDistrust
