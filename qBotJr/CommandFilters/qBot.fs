namespace qBotJr
open System.Threading.Channels
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

    let inline printRolesSB (sb : StringBuilder) (roles : SocketRole list) =
        let rec print (firstRun : bool) (roles : SocketRole list) =
            match firstRun, roles with
            | false, [] -> sb.AppendLine "" |> ignore
            | true, [] -> sb.AppendLine "No Current Value." |> ignore
            | false, r::rs -> ", @" + r.Name |> sb.Append |> ignore; print false rs
            | true, r::rs -> "    Current Value: @" + r.Name |> ignore; print false rs
        print true roles

    let printRoles (roles : SocketRole list) : string =
        let sb = StringBuilder()
        printRolesSB sb roles
        sb.ToString()


    let printMan (goo : GuildOO) (args : qBotArgs) : string =
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

    let printValid (argsV : qBotValid) : string =
        let sb = StringBuilder()
        let a format = bprintfn sb format

        a "Current Setting:"
        sb.Append "qBot -a " |> ignore
        printRolesSB sb argsV.AdminRoles
        sb.Append "-c " |> ignore
        printRolesSB sb argsV.AdminRoles |> ignore
        a " -l %s" <|  quoteEscape argsV.LobbyCategory.Name

        sb.ToString()
    let successFun (server: Server) (goo : GuildOO) (argsV : qBotValid) =
        ()

    let errorFun (server: Server) (goo : GuildOO) (argsO : qBotArgs) (argsV : qBotArgs) =
        ()
    let initArgs (args : CommandLineArgs list) : qBotArgs =
        let rec init (xs : CommandLineArgs list) (acc : qBotArgs) =
            match xs with
            | [] -> acc
            | x::xs when x.Switch = Some 'A' ->
                init xs {acc with qBotArgs.AdminRoles = x.Values }
            | x::xs when x.Switch = Some 'C' ->
                init xs {acc with qBotArgs.CaptainRoles = x.Values }
            | x::xs when x.Switch = Some 'L' && x.Values <> [] ->
                init xs {acc with qBotArgs.LobbyCategory = Some x.Values.Head}
            | x::xs -> init xs acc
        qBotArgs.createDefault |> init args

    let validate (server: Server) (goo : GuildOO) (args : qBotArgs) =
        let g = goo.Guild
        let validAdmins = validateRoles g args.AdminRoles
        let validCaps = validateRoles g args.CaptainRoles
        let validChl = validateCategory g args.LobbyCategory

        qBotValid.create <!> validAdmins <*> validCaps <*> validChl


    let str = "QBOT"
    let Run (pm : ParsedMsg) (goo : GuildOO) : unit =
        let settings = config.GetGuildSettings goo.Channel.Guild.Id
        let adminRoles = getRolesByIDs goo.Channel.Guild settings.AdminRoles
        let captainRoles = getRolesByIDs goo.Channel.Guild settings.CaptainRoles



        ()

    let noPerms  (pm : ParsedMsg) (goo : GuildOO) : unit =
        ()

