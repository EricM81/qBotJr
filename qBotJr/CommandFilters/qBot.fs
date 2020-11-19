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

    [<Struct>] //val type
    type private qBotArgs =
        {
            //todo add a man flag (-?)
            Server : Server
            Goo : GuildOO
            AdminRoles : string list
            CaptainRoles : string list
            LobbyCategory : string option
            Errors : string list
        }
        static member create s g admins captains cat =
            { qBotArgs.Server = s; Goo = g;AdminRoles = admins; CaptainRoles = captains; LobbyCategory = cat; Errors = []}
        static member createDefault s g =
            { qBotArgs.Server = s; Goo = g;AdminRoles = []; CaptainRoles = []; LobbyCategory = None; Errors =[] }

    [<Struct>] //val type
    type private qBotValid =
        {
            Server : Server
            Goo : GuildOO
            AdminRoles : SocketRole list
            CaptainRoles : SocketRole list
            LobbyCategory : SocketCategoryChannel
        }
        static member create s goo admins captains cat =
            { qBotValid.Server = s; Goo = goo;AdminRoles = admins; CaptainRoles = captains; LobbyCategory = cat }



    let private printMan (args : qBotArgs) : string =
        let sb = StringBuilder()
        let a format = bprintfn sb format


        let rec printRoles (firstRun : bool) (roles : string list) =
            match firstRun, roles with
            | false, [] -> "" |> sb.AppendLine |> ignore
            | true, [] -> "No Current Value." |> sb.AppendLine |> ignore
            | true, r::rs -> "    Current Value: " + (quoteEscape r) |> sb.Append |> ignore; printRoles false rs
            | false, r::rs -> ", " + (quoteEscape r) |> sb.Append |> ignore; printRoles false rs

        a ">>> **Configure Your Server's Settings**"
        a ""
        printErrors sb args.Errors
        a "```qBot -a @admins -c @mods @sub_carries -L \"lit category name\""
        a ""
        a "-a  @Roles (in addition to builtin) that run admin commands:"
        printRoles true args.AdminRoles
        a "    -- qHere, qNew, qMode, qSet, qBan"
        a ""
        a "-c  @Roles that setup your next game while you play"
        printRoles true args.CaptainRoles
        a "    -- setup the next lobby in game"
        a "    -- do matchmaking, assign teams"
        a "    -- qAFK, qKick, qAdd, qClose"
        a ""
        a "-L  #channel_category where the bot makes lobbies"
        match (bind (getCategoryByName args.Goo.Guild) args.LobbyCategory) with
        | Some chl -> a "    Current Value: #%s" <| quoteEscape chl.Name
        | _ -> a "    Current Value: None"
        a "    -- the bot does not modify existing users or channels"
        a "    -- the bot creates a new channel, grants the players"
        a "       permission to the channel, and deletes it on qClose"
        a "    Valid Values:"
        printCategoryNames args.Goo.Guild sb
        a "```"
        a "You can use the full command, but I'm also listening for a single option,"
        a "i.e. \"-a @admin @mod\"."
        sb.ToString()

    let private printValid (argsV : qBotValid) : string =


        let sb = StringBuilder()
        let a format = bprintfn sb format

        let rec printRoles (firstRun : bool) (roles : string list) =
            match firstRun, roles with
            | _, [] -> ()
            | true, r::rs -> r |> sb.Append |> ignore; printRoles false rs
            | false, r::rs -> ", " + r |> sb.Append |> ignore; printRoles false rs

        a "Current Setting:"
        sb.Append "qBot -a " |> ignore
        argsV.AdminRoles
        |> List.map socketRoleToStrId
        |> printRoles true
        sb.Append " -c " |> ignore
        argsV.CaptainRoles
        |> List.map socketRoleToStrId
        |> printRoles true
        a " -L %s" <|  quoteEscape argsV.LobbyCategory.Name

        sb.ToString()

    let private successFun (argsV : qBotValid) =
        let gSettings = config.GetGuildSettings argsV.Server.GuildID
        let aRoles = argsV.AdminRoles |> List.map (fun r -> r.Id)
        let cRoles = argsV.CaptainRoles |> List.map (fun r -> r.Id)
        {gSettings with AdminRoles = aRoles; CaptainRoles = cRoles; LobbiesCategory = Some argsV.LobbyCategory.Id}
        |> config.SetGuildSettings

        printValid argsV
        |> sendMsg argsV.Goo.Channel
        |> ignore
        None

    let private errorFun (args : qBotArgs) filter =
        //-a -c -l
        printMan args
        |> sendMsg args.Goo.Channel |> ignore
        client.AddMessageFilter filter
        None

    let inline private createFilters (guildID: uint64) (userID: uint64) (callBack:MessageAction) =
        let adminFilter = Command.create "-A" UserPermission.Admin callBack reactDistrust
        let captainFilter = Command.create "-C" UserPermission.Admin callBack reactDistrust
        let lobbyFilter = Command.create "-L" UserPermission.Admin callBack reactDistrust

        [adminFilter; captainFilter; lobbyFilter]
        |> MessageFilter.create guildID (DateTimeOffset.Now.AddMinutes(5.0)) (Some userID)

    let private cmdStrToArgs (cmdArgs : CommandLineArgs list) (configArgs:qBotArgs) : qBotArgs =
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

    let private validateAdmins (args:qBotArgs) =
        match validateRoles args.Goo.Guild args.AdminRoles with
        | Ok srList -> Ok srList
        | Error (strList, errList) -> Error ({args with AdminRoles = strList; Errors = List.concat [errList; args.Errors]})

    let private validateCaptains (args:qBotArgs) =
        match validateRoles args.Goo.Guild  args.CaptainRoles with
        | Ok srList -> Ok srList
        | Error (strList, errList) -> Error ({args with CaptainRoles = strList; Errors = List.concat [errList; args.Errors]})

    let private validateCategory (args:qBotArgs) =
        match args.LobbyCategory with
        | Some s ->
            match getCategoryByName args.Goo.Guild s with
                | Some catV -> Ok catV
                | None -> Error ({args with LobbyCategory = None; Errors = ("Invalid Category Name: " + s)::args.Errors})
        | None -> Error ({args with LobbyCategory = None; Errors = "Category Is Required"::args.Errors})

    let private validate (args : qBotArgs) =
        Ok (args, (qBotValid.create args.Server args.Goo))
        |>> validateAdmins |>> validateCaptains |>> validateCategory
        |> function
        | Ok (_,argsV) -> Ok (argsV)
        | Error ex -> Error ex

    let rec private _run (pm : ParsedMsg) (args : qBotArgs) =
        cmdStrToArgs pm.ParsedArgs args
        |> validate
        |> function
        | Ok argsV -> successFun argsV
        | Error args ->
            (fun (server : Server) (goo : GuildOO) (pm : ParsedMsg) ->
                {args with Errors = []}
                |> _run pm )
            |> createFilters args.Goo.GuildID args.Goo.User.Id
            |> errorFun args

    let Run (server : Server) (goo : GuildOO) (pm : ParsedMsg) : Server option =
        let settings = config.GetGuildSettings goo.Channel.Guild.Id
        let adminRoles =
            getRolesByIDs goo.Channel.Guild settings.AdminRoles
            |> List.map socketRoleToStrId
        let captainRoles =
            getRolesByIDs goo.Channel.Guild settings.CaptainRoles
            |> List.map socketRoleToStrId
        let lobbiesCat =
            settings.LobbiesCategory
            |> bind getCategoryById
            |> bind (fun cat -> Some cat.Name)
        qBotArgs.create server goo adminRoles captainRoles lobbiesCat
        |> _run pm

    let Command = Command.create "QBOT" UserPermission.Admin Run reactDistrust
