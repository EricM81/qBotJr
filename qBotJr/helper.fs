namespace qBotJr

open System
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

    [<StructuralEquality ; StructuralComparison>]
    [<Struct>]
    type ContinueOption<'T, 'U> =
        | Continue of Continue : 'T
        | Found of Completed : 'U

    let inline bindCont f x =
        match x with
        | Continue T' -> f T'
        | Found U' -> Found U'


    [<StructuralEquality ; StructuralComparison>]
    [<Struct>]
    type Validate<'T, 'U> =
        | Success of Success : 'T
        | Fail of Fail : 'U

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

    let isCreator (gUser : IGuildUser) =
        if gUser.Id = 442438729207119892UL then UserPermission.Creator else UserPermission.None

    let isDiscordAdmin (gUser : IGuildUser) =
        if gUser.GuildPermissions.Administrator = true then UserPermission.Admin else UserPermission.None

    let isRole (serverRoles : uint64 list) (gUser : IGuildUser) =
        serverRoles
        |> List.exists (fun serverRole -> gUser.RoleIds |> Seq.exists (fun userRole -> userRole = serverRole))

    let isGuildAdmin (gUser : IGuildUser) =
        if isRole (config.GetGuildSettings(gUser.Guild.Id).AdminRoles) gUser then
            UserPermission.Admin
        else
            UserPermission.None

    let isGuildCaptain (gUser : IGuildUser) =
        if isRole (config.GetGuildSettings(gUser.Guild.Id).CaptainRoles) gUser then
            UserPermission.Captain
        else
            UserPermission.None

    let getPerm gUser : UserPermission =
        isCreator gUser
        |> bindPerms isDiscordAdmin gUser
        |> bindPerms isGuildAdmin gUser
        |> bindPerms isGuildCaptain gUser

    let bprintfn (sb : StringBuilder) = Printf.kprintf (fun s -> sb.AppendLine s |> ignore)

    let printPlayer (ph : PlayerHere) =
        let widthT = 20 //max width
        let post =
            match ph.isHere with
            | true -> sprintf " (%i)" ph.GamesPlayed
            | false -> " (afk)"
        //length of GamesPlayed (" (%i)", x); games played (log10 + 1) + formatting (3 chars)
        //let widthG = ph.GamesPlayed |> float |> Math.Log10 |> int16 |> int |> (+) 1 |> (+) 3
        let widthP = post.Length
        let widthR = widthT - widthP //remaining len for name
        let widthN = ph.Player.Name.Length
        let name' = if widthN > widthR then ph.Player.Name.Substring(0, widthR) else ph.Player.Name
        name' + post + if widthR > widthN then String(' ', widthR - widthN) else ""

    let printPlayersList (pHere : PlayerHere list) : string =
        if pHere.Length > 0 then
            let sb = StringBuilder()
            sb.Append "Who's Here: \n```" |> ignore
            let rec printPlayers (xs : PlayerHere list) (ys : PlayerHere list) =
                match xs, ys with
                | [], [] -> ()
                | x :: xs, y :: ys ->
                    printPlayer x + " | " +  printPlayer y |> sb.AppendLine |> ignore
                    printPlayers xs ys
                | x :: _, [] -> printPlayer x |> sb.AppendLine |> ignore
                | [], y :: _ -> printPlayer y |> sb.AppendLine |> ignore
            //todo the sort is not working
            let players' = pHere |> List.sortBy (fun ph -> (not ph.isHere), ph.GamesPlayed, ph.Player.Name)
            let (colB, colA) = players' |> List.splitAt (players'.Length / 2)
            printPlayers colA colB
            sb.Append "```" |> ignore
            sb.ToString()
        else ""

    let getServer (servers: Map<uint64, Server>) (guild : SocketGuild) =
        servers
        |> Map.tryFind guild.Id
        |> function
        | Some s -> s
        | None -> Server.create guild



