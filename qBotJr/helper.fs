namespace qBotJr

open qBotJr.T
open Discord


module helper =
    let inline prepend xs x = x :: xs

    //I'm not sure if resulting workflow steps should be asynchronous.
    //The Discord.NET wrapper already handles all network communication asynchronously.
    //If needed, it's easy to make everything async after being filtered. Change:
    //CmdOption's "Completed" to "Completed of Async<unit>"
    //bind's "Completed -> Completed" to "Completed z -> Completed z"
    //The partial applications created in these functions:
    //genericFail, noPermissions, and permissionsCheck
    //Can be wrapped with async{} and let processMsg execute them

    [<StructuralEquality ; StructuralComparison>]
    [<Struct>]
    type ContinueOption<'T, 'U> =
        | Continue of Continue : 'T
        | Found of Completed : 'U

    let inline bind f x =
        match x with
        | Some x -> f x
        | None -> None
    //
//    let inline bind2 f g x =
//        match x with
//        | Continue y -> f g y
//        | Found y -> Found y

    let inline bindCont f x =
        match x with
        | Continue T' -> f T'
        | Found U' -> Found U'

    let inline runCont f y x =
        match x with
        | Found U' -> f y U'
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
