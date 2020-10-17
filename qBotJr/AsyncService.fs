namespace qBotJr

open Discord
open Discord.WebSocket
open System
open System.Threading.Tasks

//Async MailboxProcessor wrapper to accept Post commands
module AsyncService =
    //thread safe, internal handler for messages from MailboxProcessor.Receive() 
    module private _command =
        
        //I'm not sure if resulting workflow steps should be asynchronous.
        //The Discord.NET wrapper already handles all network communication asynchronously.
        //If needed, it's easy to make everything async after being filtered. Change:
        //CmdOption's "Completed" to "Completed of Async<unit>"
        //bind's "Completed -> Completed" to "Completed z -> Completed z"
        //Wrap the partial applications in: 
        //genericFail, noPermissions, and permissionsCheck
        //with async{} and let processMsg execute them 
        [<StructuralEquality; StructuralComparison>]
        [<Struct>]
        type CmdOption<'T> = 
            | Continue of Continue : 'T 
            | Completed 

        let bind f x = 
            match x with 
            | Continue y -> f y
            | Completed -> Completed

        let (|StaticCmd|_|) (p : string) (msg : SocketMessage) =
            if msg.Content.StartsWith(p, StringComparison.OrdinalIgnoreCase) then
                Some msg
            else
                None

        let (|IsCreator|_|) (gUser : SocketGuildUser) = 
            if gUser.Id = 442438729207119892UL then
                Some gUser
            else    
                None

        let (|IsAdmin|_|) (gUser : SocketGuildUser) = 
            if gUser.GuildPermissions.Administrator = true then
                Some gUser
            else
                None

        let (|IsRole|_|) (role : UserPermissions) (gUser : SocketGuildUser) = 
            let roles = 
                match role with 
                | UserPermissions.Admin -> config.GetGuildSettings(gUser.Guild.Id).AdminRoles
                | UserPermissions.Captain -> config.GetGuildSettings(gUser.Guild.Id).CaptainRoles
                | _ -> []

            let x = 
                roles    //server admin/captain roles
                |> List.exists     //see if any are granted to a user
                    (fun y -> 
                        gUser.Roles     //z = user's roles, y = matching role from config
                        |> Seq.exists 
                            (fun z ->
                                z.Id = y
                            )
                    )

            match x with 
            | true -> Some gUser
            | false -> None
        
       
        let genericFail (msg : SocketMessage) (channel : SocketGuildChannel) (user : SocketGuildUser) (perm : UserPermissions) : unit =
            Async.AwaitTask(msg.AddReactionAsync(Emoji(Emojis.Distrust))) |> ignore

        let noPermissions (msg : SocketMessage) (basicFunc : UserMessageAction) : CmdOption<'T> = 
            basicFunc msg
            Completed

        let permissionsCheck (minPerm : UserPermissions) (successFunc : PrivilegedMessageAction) (failFunc : PrivilegedMessageAction) (msg : SocketMessage) : CmdOption<'T> =
            match msg.Author with
            | :? SocketGuildUser as gUser  ->
                match msg.Channel with
                | :? SocketGuildChannel as gChannel ->
                    let perm = 
                        match gUser with 
                        | IsCreator x -> UserPermissions.Creator
                        | IsAdmin x -> UserPermissions.Admin
                        | IsRole UserPermissions.Admin x -> UserPermissions.Admin
                        | IsRole UserPermissions.Captain x -> UserPermissions.Captain
                        | _ -> UserPermissions.None
                    if perm >= minPerm then 
                        successFunc msg gChannel gUser perm
                    else 
                        failFunc msg gChannel gUser perm 

                    Completed

                | _ -> Completed    
            | _ -> Completed
        
        let filterStaticCommands (msg : SocketMessage) : CmdOption<SocketMessage> = 
            //all static bot commands start with a "q"
            //no Q, no need to check
            let q = msg.Content.[0]
            if (q = 'Q' || q = 'q') then 
                
                match msg with 
                | StaticCmd "QBOT" msg -> 
                    permissionsCheck UserPermissions.Admin qBot.Run genericFail msg 
                | StaticCmd "QHERE" msg -> 
                    permissionsCheck UserPermissions.Admin qHere.Run genericFail msg
                | StaticCmd "QNEW" msg -> 
                    permissionsCheck UserPermissions.Admin qNew.Run genericFail msg
                | StaticCmd "QGAMEMODE" msg -> 
                    permissionsCheck UserPermissions.Admin qGameMode.Run genericFail msg
                | StaticCmd "QSET" msg -> 
                    permissionsCheck UserPermissions.Admin qSet.Run genericFail msg
                | StaticCmd "QNEXT" msg -> 
                    permissionsCheck UserPermissions.Admin qNext.Run genericFail msg
                | StaticCmd "QAFK" msg -> 
                    permissionsCheck UserPermissions.Captain qAFK.Run genericFail msg
                | StaticCmd "QBAN" msg -> 
                    permissionsCheck UserPermissions.Captain qBan.Run genericFail msg
                | StaticCmd "QKICK" msg -> 
                    permissionsCheck UserPermissions.Captain qKick.Run genericFail msg
                | StaticCmd "QADD" msg -> 
                    permissionsCheck UserPermissions.Captain qAdd.Run genericFail msg
                | StaticCmd "QCLOSE" msg -> 
                    permissionsCheck UserPermissions.Captain qClose.Run genericFail msg
                | StaticCmd "QCUSTOMS" msg -> 
                    permissionsCheck UserPermissions.None qCustoms.Run genericFail msg
                | _ -> Continue msg
            else
                Continue msg
        
        let filterDynamicCommands (msg : SocketMessage) : CmdOption<SocketMessage> =
            //looking for a response to a request.  
            //all authentication needs to be done on the request
            Continue msg

        //cmds I can run for testing....or memeing
        let filterCreatorCommands (msg : SocketMessage) : CmdOption<SocketMessage> =
            match msg with
            | StaticCmd "HI JR" msg ->
                permissionsCheck UserPermissions.Creator Creator.RunHiJr genericFail msg
            
            |_ -> Continue msg

        let filterGuildDynamicCommands (msg : SocketMessage) : CmdOption<SocketMessage> =
            Completed

        let processMsg (inbox: MailboxProcessor<MailboxMessage>) = 
            let rec msgLoop() = 
                async{
                    let! mm = inbox.Receive()
                    match mm with
                    | NewMessage nm ->
                        Continue nm
                        |> bind filterStaticCommands
                        |> bind filterCreatorCommands 
                        |> bind filterDynamicCommands
                        |> ignore
                    | MessageReaction mr -> 
                        () //todo - message reaction filter
                    | ScheduledTask t ->
                        () //TODO - task scheduler
                    return! msgLoop()
                }
            msgLoop()

        
        let agent = MailboxProcessor.Start(processMsg)
        

    //once a message is handled, it returns 
    let Receive (mm : MailboxMessage) = 
        _command.agent.Post mm    
        Task.CompletedTask

