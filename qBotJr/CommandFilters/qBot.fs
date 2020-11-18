namespace qBotJr
open Discord.WebSocket
open System.Text
open discord
open qBotJr.T
open helper

module qBot =
    //admin roles
    //capt roles
    //lobbies cat

    [<Struct>] //val type
    type private qBotArgs =
        {
            AdminRoles : string list
            CaptainRoles : string list
            LobbyCategory : string option
        }
        static member create admins captains cat =
            { qBotArgs.AdminRoles = admins; CaptainRoles = captains; LobbyCategory = cat }
        static member createDefault =
            { qBotArgs.AdminRoles = []; CaptainRoles = []; LobbyCategory = None }

    [<Struct>] //val type
    type private qBotValid =
        {
            AdminRoles : SocketRole list
            CaptainRoles : SocketRole list
            LobbyCategory : SocketCategoryChannel
        }
        static member create admins captains cat =
            { qBotValid.AdminRoles = admins; CaptainRoles = captains; LobbyCategory = cat }

    let inline printRolesSB (sb : StringBuilder) (roles : string list) =
        let rec print (firstRun : bool) (roles : string list) =
            match firstRun, roles with
            | false, [] -> "" |> sb.AppendLine |> ignore
            | true, [] -> "No Current Value." |> sb.AppendLine |> ignore
            | true, r::rs -> "    Current Value: " + (quoteEscape r) |> sb.Append |> ignore; print false rs
            | false, r::rs -> ", " + (quoteEscape r) |> sb.Append |> ignore; print false rs
        print true roles

    let private printMan (goo : GuildOO) (args : qBotArgs) : string =
        let sb = StringBuilder()
        let a format = bprintfn sb format

        a ">>> **Configure Your Server's Settings**"
        a "```qBot -a @admins -c @mods @sub_carries -L \"lit category name\""
        a ""
        a "-a  @Roles (in addition to builtin) that run admin commands:"
        printRolesSB sb args.AdminRoles
        a "    -- qHere, qNew, qMode, qSet, qBan"
        a ""
        a "-c  @Roles that setup your next game while you play"
        printRolesSB sb args.CaptainRoles
        a "    -- setup the next lobby in game"
        a "    -- do matchmaking, assign teams"
        a "    -- qAFK, qKick, qAdd, qClose"
        a ""
        a "-L  #channel_category where the bot makes lobbies"
        match args.LobbyCategory, (bind (getCategoryByName goo.Guild) args.LobbyCategory) with
        | _, Some chl -> a "    Current Value: #%s" chl.Name
        | Some name, None -> a "    %s is not a channel category" <|  quoteEscape name
        | _ -> ()
        a "    -- the bot does not modify existing users or channels"
        a "    -- the bot creates a new channel, grants the players"
        a "       permission to the channel, and deletes it on qClose"
        a "    Valid Values:"
        printCategoryNames goo.Guild sb
        a "```"
        a "You can use the full command, but I'm also listening for a single option, i.e. \"-a @admin @mod\"."
        sb.ToString()

    let private printValid (argsV : qBotValid) : string =
        let sb = StringBuilder()
        let a format = bprintfn sb format

        a "Current Setting:"
        sb.Append "qBot -a " |> ignore
        argsV.AdminRoles
        |> List.map (fun role -> role.Name)
        |> printRolesSB sb
        sb.Append " -c " |> ignore
        argsV.CaptainRoles
        |> List.map (fun role -> role.Name)
        |> printRolesSB sb
        a " -l %s" <|  quoteEscape argsV.LobbyCategory.Name

        sb.ToString()

    let private successFun (server: Server) (goo : GuildOO) (argsV : qBotValid) =
        Some server

    let private errorFun (server: Server) (goo : GuildOO) (args : qBotArgs) (errs : string list) =
        None

    let private initArgs (cmdArgs : CommandLineArgs list) (configArgs:qBotArgs) : qBotArgs =
        let rec init (xs : CommandLineArgs list) (acc : qBotArgs) =
            match xs with
            | [] -> acc
            | x::xs when x.Switch = Some 'A' ->
                init xs {acc with qBotArgs.AdminRoles = x.Values }
            | x::xs when x.Switch = Some 'C' ->
                init xs {acc with qBotArgs.CaptainRoles = x.Values }
            | x::xs when x.Switch = Some 'L' && x.Values <> [] ->
                init xs {acc with qBotArgs.LobbyCategory = Some x.Values.Head}
            | _::xs -> init xs acc
        init cmdArgs configArgs

    let private validateAdmins (g :SocketGuild, args:qBotArgs) =
        match validateRoles g args.AdminRoles with
        | Ok srList -> Ok srList
        | Error (strList, errList) -> Error ((g, {args with AdminRoles = strList}), errList)

    let private validateCaptains (g :SocketGuild, args:qBotArgs) =
        match validateRoles g args.CaptainRoles with
        | Ok srList -> Ok srList
        | Error (strList, errList) -> Error ((g, {args with CaptainRoles = strList}), errList)

    let private validateCategory (g :SocketGuild, args:qBotArgs) =
        match args.LobbyCategory with
        | Some s ->
            match getCategoryByName g s with
                | Some catV -> Ok catV
                | None -> Error ((g, {args with LobbyCategory = None}), ["Invalid Category Name: " + s])
        | None -> Error ((g, {args with LobbyCategory = None}), ["Category Is Required"])

    let private validate (g : SocketGuild) (args : qBotArgs) =
        Ok ((g, args), qBotValid.create)
        |> bindR validateAdmins
        |> bindR validateCaptains
        |> bindR validateCategory

    let Run (server : Server) (goo : GuildOO) (pm : ParsedMsg) : Server option =
        let settings = config.GetGuildSettings goo.Channel.Guild.Id
        let adminRoles =
            getRolesByIDs goo.Channel.Guild settings.AdminRoles
            |> List.map (fun sr -> sr.Name)
        let captainRoles =
            getRolesByIDs goo.Channel.Guild settings.CaptainRoles
            |> List.map (fun sr -> sr.Name)
        let lobbiesCat =
            settings.LobbiesCategory
            |> bind getCategoryById
            |> bind (fun cat -> Some cat.Name)

        qBotArgs.create adminRoles captainRoles lobbiesCat
        |> initArgs pm.ParsedArgs
        |> validate goo.Guild
        |> function
        | Ok (_, argsV) -> successFun server goo argsV
        | Error ((_, args), errs) -> errorFun server goo args errs
