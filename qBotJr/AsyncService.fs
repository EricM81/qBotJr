namespace qBotJr

open Discord
open Discord.WebSocket
open System
open qBotJr.Interpreter
open qBotJr.T
open qBotJr.helper


//Async MailboxProcessor wrapper to accept Post commands
module AsyncService =




    //thread safe, internal handler for messages from MailboxProcessor.Receive()
    module private _service =

        //I'm not sure if resulting workflow steps should be asynchronous.
        //The Discord.NET wrapper already handles all network communication asynchronously.
        //If needed, it's easy to make everything async after being filtered. Change:
        //CmdOption's "Completed" to "Completed of Async<unit>"
        //bind's "Completed -> Completed" to "Completed z -> Completed z"
        //The partial applications created in these functions:
        //genericFail, noPermissions, and permissionsCheck
        //Can be wrapped with async{} and let processMsg execute them

        let inline matchPrefix (prefix : string) (msg : SocketMessage) : bool =
            if msg.Content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
                true
            else
                false

        let inline parseMsg (cmd : Command) (msg : SocketMessage) =
            parseInput cmd.Prefix msg.Content
            |> ParsedMsg.create msg

        let inline (|ParseMsg|_|) (cmd : Command) (msg : SocketMessage) =
            if matchPrefix cmd.Prefix msg then parseMsg cmd msg |> Some else None

        let inline (|IsCreator|_|) (gUser : IGuildUser) =
            if gUser.Id = 442438729207119892UL then Some gUser else None

        let inline (|IsAdmin|_|) (gUser : IGuildUser) =
            if gUser.GuildPermissions.Administrator = true then
                Some gUser
            else
                None

        let inline (|IsRole|_|) (role : UserPermission) (gUser : IGuildUser) =
            let hasMatch =
                match role with
                | UserPermission.Admin -> config.GetGuildSettings(gUser.Guild.Id).AdminRoles
                | UserPermission.Captain -> config.GetGuildSettings(gUser.Guild.Id).CaptainRoles
                | _ -> []
                |> List.exists (fun serverRoles ->
                    gUser.RoleIds //z = user's roles, y = matching role from config
                    |> Seq.exists (fun userRole -> userRole = serverRoles))

            match hasMatch with
            | true -> Some gUser
            | false -> None

        let checkPermAndRun (goo : GuildOO) (cmd : Command) (parsedMsg : ParsedMsg) : CmdOption<'T> =

            let perm =
                match goo.User with
                | IsCreator _ -> UserPermission.Creator
                | IsAdmin _ -> UserPermission.Admin
                | IsRole UserPermission.Admin _ -> UserPermission.Admin
                | IsRole UserPermission.Captain _ -> UserPermission.Captain
                | _ -> UserPermission.None

            if perm >= cmd.RequiredPerm then cmd.PermSuccess parsedMsg goo else cmd.PermFailure parsedMsg goo

            Completed

        let filterStaticCommands (nm : NewMessage) : CmdOption<NewMessage> =
            //all static bot commands start with a "q"
            //no Q, no need to check
            let q = nm.Message.Content.[0]
            if (q = 'Q' || q = 'q') then

                match nm.Message with
//                | ParseMsg qBot.str args ->
//                    permissionsCheck UserPermission.Admin qBot.Run genericFail args
                | ParseMsg qHere.Command args -> checkPermAndRun nm.GuildOO qHere.Command args
//                | ParseMsg qNew.str args ->
//                    permissionsCheck UserPermission.Admin qNew.Run genericFail args
//                | ParseMsg qMode.str args ->
//                    permissionsCheck UserPermission.Admin qMode.Run genericFail args
//                | ParseMsg qSet.str args ->
//                    permissionsCheck UserPermission.Admin qSet.Run genericFail args
//                | ParseMsg qNext.str args ->
//                    permissionsCheck UserPermission.Admin qNext.Run genericFail args
//                | ParseMsg qAFK.str args ->
//                    permissionsCheck UserPermission.Captain qAFK.Run genericFail args
//                | ParseMsg qBan.str args ->
//                    permissionsCheck UserPermission.Captain qBan.Run genericFail args
//                | ParseMsg qKick.str args ->
//                    permissionsCheck UserPermission.Captain qKick.Run genericFail args
//                | ParseMsg qAdd.str args ->
//                    permissionsCheck UserPermission.Captain qAdd.Run genericFail args
//                | ParseMsg qClose.str args ->
//                    permissionsCheck UserPermission.Captain qClose.Run genericFail args
//                | ParseMsg qCustoms.str args ->
//                    permissionsCheck UserPermission.None qCustoms.Run genericFail args
                | _ -> Continue nm
            else
                Continue nm

        //cmds I can run for testing....or memeing
        let filterCreatorCommands (nm : NewMessage) : CmdOption<NewMessage> =
            match nm.Message with
            | ParseMsg Creator.HiJr args -> checkPermAndRun nm.GuildOO Creator.HiJr args
            | _ -> Continue nm

        let filterDynamicCommands (nm : NewMessage) : CmdOption<NewMessage> =
            let now = DateTimeOffset.Now
            let guildID = nm.GuildOO.Guild.Id

            let rec matchFilterItem (xs : Command list) : CmdOption<SocketMessage> =
                match xs with
                | [] -> Continue nm.Message
                | x :: xs ->
                    match nm.Message with
                    | ParseMsg x args -> checkPermAndRun nm.GuildOO x args
                    | _ -> matchFilterItem xs

            let rec matchFilter (xs : MessageFilter list) : CmdOption<NewMessage> =
                match xs with
                | [] -> Continue nm
                | x :: xs when (guildID = x.GuildID && now < x.TTL) ->

                    let items =
                        match x.User with
                        | Some user when user = nm.GuildOO.User.Id -> x.Items
                        | Some user when user <> nm.GuildOO.User.Id -> []
                        | _ -> x.Items

                    let result = matchFilterItem items

                    match result with
                    | Completed ->
                        x.TTL <- DateTimeOffset.MinValue
                        Completed
                    | _ -> matchFilter xs

                | _ :: xs -> matchFilter xs

            matchFilter State.MessageFilters

        let inline updateModeList (player : uint64) (players : uint64 list) (isHere : bool) : uint64 list =
            let isInList =
                players |> List.exists (fun id -> player = id)

            match isInList, isHere with
            | true, true
            | false, false -> players
            | true, false -> players |> List.filter (fun id -> player <> id)
            | false, true -> player :: players

        let inline matchGameModes (server : Server) (mr : MessageReaction) : bool =
            let isMatch (mode : Mode) : bool =
                if mode.ModeMsg.Id = mr.Message.Id then
                    mode.PlayerIDs <- updateModeList mr.Reaction.UserId mode.PlayerIDs mr.IsHere
                    true
                else
                    false

            server.Modes |> List.exists isMatch

        let inline updateHereList (server : Server) (mr : MessageReaction) (player : Player option) =
            match player, mr.IsHere with
            | Some p, true
            | Some p, false -> p.isHere <- mr.IsHere
            | None, true ->
                let iUser = mr.GuildOO.User
                server.Players <-
                    (Player.create iUser.Id iUser.Nickname)
                    :: server.Players
            | None, false -> ()

            server.PlayerListIsDirty <- true

        let inline matchHereMsg (server : Server) (mr : MessageReaction) : bool =
            match server.HereMsg with
            | Some msg when mr.Message.Id = msg.Id ->
                server.Players
                |> List.tryFind (fun player -> if player.UID = mr.Reaction.UserId then true else false)
                |> updateHereList server mr
                true
            | _ -> matchGameModes server mr

        let filterStaticReactions (mr : MessageReaction) : CmdOption<MessageReaction> =
            let reactionFound =
                State.Guilds
                |> Map.exists (fun _ v -> if v.Guild.Id = mr.GuildOO.Guild.Id then matchHereMsg v mr else false)

            if reactionFound then Completed else Continue mr

        //let inline matchFilterChoice (filterChoice : ReactionFilterChoice) (mr : MessageReaction) : bool =

        let inline matchReActions (ras : ReAction list) (mr : MessageReaction) : bool =
            let inline matchAction (ra : ReAction) : bool =
                if mr.Reaction.Emote.Name = ra.Emoji then
                    ra.Action mr
                    true
                else
                    false

            ras |> List.exists matchAction

        let inline matchFilter (filter : ReactionFilter) (mr : MessageReaction) : bool =
            let isMatch =
                match filter.FilterChoice with
                | ByReaction br when br.MsgID = mr.Message.Id -> matchReActions br.Actions mr
                | ByReactionAndUser bru when bru.MsgID = mr.Message.Id
                                             && bru.UserID = mr.Reaction.UserId -> matchReActions bru.Actions mr
                | _ -> false

            if isMatch then
                filter.TTL <- DateTimeOffset.MinValue
                true
            else
                false


        let filterDynamicReactions (mr : MessageReaction) : CmdOption<MessageReaction> =
            let isMatch =
                State.ReactionFilters
                |> List.exists (fun filter ->
                    if filter.GuildID = mr.GuildOO.Guild.Id then matchFilter filter mr else false)

            if isMatch then Completed else Continue mr

        let processMail (inbox : MailboxProcessor<MailboxMessage>) =
            let rec msgLoop () =
                async {
                    let! mm = inbox.Receive()

                    match mm with
                    | NewMessage nm ->
                        filterStaticCommands nm
                        |> bind filterCreatorCommands
                        |> bind filterDynamicCommands
                        |> ignore
                    | MessageReaction mr ->
                        filterStaticReactions mr
                        |> bind filterDynamicReactions
                        |> ignore
                    | ScheduledTask t -> t ()

                    return! msgLoop ()
                }

            msgLoop ()

        let agent = MailboxProcessor.Start(processMail)


    //once a message is handled, it returns
    let Receive (mm : MailboxMessage) = _service.agent.Post mm
