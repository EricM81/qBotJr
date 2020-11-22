namespace qBotJr

open System
open System.Diagnostics.Tracing
open System.Text
open Discord.WebSocket
open qBotJr
open qBotJr.T
open Discord


module helper =
  let inline prepend xs x = x :: xs

  let inline bind f x =
    match x with
    | Some x -> f x
    | None -> None

  let inline bindV f x =
    match x with
    | ValueSome x -> f x
    | ValueNone -> ValueNone

  //https://forums.fsharp.org/t/thoughts-on-input-validation-pattern-from-a-noob/1541
  let inline bindResult f acc =
    match acc with
    | Ok (args, successFun) ->
        match f args with
        | Ok param -> Ok (args, (successFun param))
        | Error args' -> Error args'
    | Error args ->
        match f args with
        | Ok _ -> Error args
        | Error args' -> Error args'

  let (|>>) y x = bindResult x y

  let inline bindResultAndArgs f acc =
    match acc with
    | Ok (args, successFun) ->
        match f args with
        | Ok (args', p) -> Ok (args', (successFun p))
        | Error args' -> Error args'
    | Error args ->
        match f args with
        | Ok (args', _) -> Error args'
        | Error args' -> Error args'

  let (|+>) y x = bindResultAndArgs x y

  let bindB f x =
    match x with
    | true -> f
    | false -> false

  let inline tuple x y = x, y

  let inline map f xResult =
    match xResult with
    | Ok x -> Ok (f x)
    | Error ex -> Error ex

  let inline apply xResult yResult =
    match xResult, yResult with
    | Ok x, Ok y -> Ok (x, y)
    | Error ex, Ok _ -> Error ex
    | Ok _, Error ey -> Error ey
    | Error ex, Error ey -> Error (List.concat [ex; ey])

  let inline applyL xList yItem =
    match xList, yItem with
    | Ok x, Ok y -> Ok (y :: x)
    | Error ex, Ok _ -> Error ex
    | Ok _, Error ey -> Error [ey]
    | Error ex, Error ey -> Error (ey :: ex)

  let (<!>) = map
  let (<*>) = apply

  [<StructuralEquality; StructuralComparison>]
  [<Struct>]
  type ContinueOption<'T, 'U> =
    | Continue of Continue: 'T
    | Found of Completed: 'U

  let inline bindCont f x =
    match x with
    | Continue T' -> f T'
    | Found U' -> Found U'

  [<StructuralEquality; StructuralComparison>]
  [<Struct>]
  type Validate<'T, 'U> =
    | Success of Success: 'T
    | Fail of Fail: 'U

  let inline bindValid f args isValid =
    match isValid with
    | Success T' -> f args T'
    | Fail U' -> Fail U'

  let inline runCont f a b c d e =
    match e with
    | Found e' -> f a b c d e'
    | _ -> ()

  //if no perm has been found for a user, keep searching
  let inline bindPerms permSearch user currentPerm =
    match currentPerm with
    | UserPermission.None -> permSearch user //keep searching
    | _ -> currentPerm //perm found

  let isCreator (gUser: IGuildUser) = if gUser.Id = 442438729207119892UL then UserPermission.Creator else UserPermission.None

  let isDiscordAdmin (gUser: IGuildUser) =
    if gUser.GuildPermissions.Administrator = true then UserPermission.Admin else UserPermission.None

  let isRole (serverRoles: uint64 list) (gUser: IGuildUser) =
    serverRoles |> List.exists (fun serverRole -> gUser.RoleIds |> Seq.exists (fun userRole -> userRole = serverRole))

  let isGuildAdmin (gUser: IGuildUser) =
    if isRole (config.GetGuildSettings(gUser.Guild.Id).AdminRoles) gUser then UserPermission.Admin else UserPermission.None

  let isGuildCaptain (gUser: IGuildUser) =
    if isRole (config.GetGuildSettings(gUser.Guild.Id).CaptainRoles) gUser then
      UserPermission.Captain
    else
      UserPermission.None

  let getPerm gUser: UserPermission =
    isCreator gUser |> bindPerms isDiscordAdmin gUser |> bindPerms isGuildAdmin gUser |> bindPerms isGuildCaptain gUser

  let bprintfn (sb: StringBuilder) = Printf.kprintf (fun s -> sb.AppendLine s |> ignore)

  let inline quoteEscape (str: string): string = if str.Contains (' ') then "\"" + str + "\"" else str

  let printPlayer (widthT: int) (ph: PlayerHere) =
    let post =
      match ph.isHere with
      | true -> sprintf " (%i)" ph.GamesPlayed
      | false -> " (afk)"
    //length of GamesPlayed (" (%i)", x); games played (log10 + 1) + formatting (3 chars)
    //let widthG = ph.GamesPlayed |> float |> Math.Log10 |> int16 |> int |> (+) 1 |> (+) 3
    let widthP = post.Length
    let widthR = widthT - widthP //remaining len for name
    let widthN = ph.Player.Name.Length
    let name' = if widthN > widthR then ph.Player.Name.Substring (0, widthR) else ph.Player.Name
    name' + post + if widthR > widthN then String (' ', widthR - widthN) else ""

  let printPlayersList (pHere: PlayerHere list): string =
    let widthT = 17
    if pHere.Length > 0 then
      let sb = StringBuilder ()
      sb.Append "Who's Here: *Name (Games Played)* \n```" |> ignore

      let rec printPlayers (xs: PlayerHere list) (ys: PlayerHere list) =
        match xs, ys with
        | [], [] -> ()
        | x :: xs, y :: ys ->
            printPlayer widthT x + " | " + printPlayer widthT y |> sb.AppendLine |> ignore
            printPlayers xs ys
        | x :: _, [] -> printPlayer widthT x |> sb.AppendLine |> ignore
        | [], y :: _ -> String (' ', widthT) + " | " + printPlayer widthT y |> sb.AppendLine |> ignore
      //todo the sort is not working
      let players' = pHere |> List.sortBy (fun ph -> (not ph.isHere), ph.GamesPlayed, ph.Player.Name.ToUpper ())
      let (colA, colB) = players' |> List.splitAt (players'.Length / 2 + players'.Length % 2)
      printPlayers colA colB
      sb.Append "```" |> ignore
      sb.ToString ()
    else
      ""

  let printErrors (sb: StringBuilder) (errs: string list) =
    let rec print (errs: string list) =
      match errs with
      | [] -> ()
      | e :: ex ->
          "**" + e + "**" |> sb.AppendLine |> ignore
          print ex

    print errs

  let getServer (servers: Map<uint64, Server>) (guild: SocketGuild) =
    servers
    |> Map.tryFind guild.Id
    |> function
    | Some s -> s
    | None -> Server.create guild

  let printCommaSeparatedList (strList: string list) =
    let rec print firstRun xs acc =
      match firstRun, xs with
      | _, [] -> acc
      | true, p :: px -> quoteEscape p |> print false px
      | false, p :: px -> ", " + (quoteEscape p) |> print false px

    print true strList ""

  let filterSortPlayers (phs: PlayerHere list) =
    let rec isMatch (items: PlayerHere list) (acc: PlayerHere list) =
      match items with
      | [] -> acc
      | ph::px ->
        if ph.isHere = true && ph.isBanned = false then
          ph::acc
        else
          acc
        |> isMatch px
    isMatch phs []
    |> List.sortBy (fun p -> p.GamesPlayed)

  let filterSortModePlayers (mode: Mode) (players: PlayerHere list) =
    let rec tryFindPlayerHere (pid: uint64) (px: PlayerHere list): PlayerHere voption =
      match px with
      | [] -> ValueNone
      | p::px ->
        if p.Player.ID = pid then
          ValueSome p
        else
          tryFindPlayerHere pid px

    let rec matchMode (modePlayers: Player list) (acc: PlayerHere list) =
      match modePlayers with
      | [] -> acc
      | mp::mpx ->
        (match tryFindPlayerHere mp.ID players with
        | ValueSome ph -> ph::acc
        | ValueNone -> acc)
        |> matchMode mpx

    matchMode mode.Players []
    |> List.sortBy (fun p -> p.GamesPlayed)
