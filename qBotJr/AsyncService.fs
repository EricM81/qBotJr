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

        let (|ParseMsg|_|) (cmdName : string) (msg : SocketMessage) =
            if msg.Content.StartsWith(cmdName, StringComparison.OrdinalIgnoreCase) then
                let args = Interpreter.parseInput cmdName msg.Content
                ParsedMsg.create msg args |> Some
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

        let (|IsRole|_|) (role : UserPermission) (gUser : SocketGuildUser) = 
            let roles = 
                match role with 
                | UserPermission.Admin -> config.GetGuildSettings(gUser.Guild.Id).AdminRoles
                | UserPermission.Captain -> config.GetGuildSettings(gUser.Guild.Id).CaptainRoles
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
        
       
        let genericFail (parsedm : ParsedMsg) (goo : GuildOO) : unit =
            Async.AwaitTask(parsedm.Message.AddReactionAsync(Emoji(Emojis.Distrust))) |> ignore


        let permissionsCheck (minPerm : UserPermission) (successFunc : PrivilegedMessageAction) (failFunc : PrivilegedMessageAction) (parsedMsg : ParsedMsg) : CmdOption<'T> =
            match parsedMsg.Message.Author with
            | :? SocketGuildUser as gUser  ->
                match parsedMsg.Message.Channel with
                | :? SocketGuildChannel as gChannel ->
                    let perm = 
                        match gUser with 
                        | IsCreator x -> UserPermission.Creator
                        | IsAdmin x -> UserPermission.Admin
                        | IsRole UserPermission.Admin x -> UserPermission.Admin
                        | IsRole UserPermission.Captain x -> UserPermission.Captain
                        | _ -> UserPermission.None
                    if perm >= minPerm then 
                        GuildOO.create gChannel gUser perm |> successFunc parsedMsg  
                    else 
                        GuildOO.create gChannel gUser perm |> failFunc parsedMsg

                    Completed

                | _ -> Completed    
            | _ -> Completed
        
        let filterStaticCommands (msg : SocketMessage) : CmdOption<SocketMessage> = 
            //all static bot commands start with a "q"
            //no Q, no need to check
            let q = msg.Content.[0]
            if (q = 'Q' || q = 'q') then 
                
                match msg with 
                | ParseMsg qBot.str args -> 
                    permissionsCheck UserPermission.Admin qBot.Run genericFail args
                | ParseMsg qHere.str args -> 
                    permissionsCheck UserPermission.Admin qHere.Run genericFail args
                | ParseMsg qNew.str args -> 
                    permissionsCheck UserPermission.Admin qNew.Run genericFail args
                | ParseMsg qMode.str args -> 
                    permissionsCheck UserPermission.Admin qMode.Run genericFail args
                | ParseMsg qSet.str args -> 
                    permissionsCheck UserPermission.Admin qSet.Run genericFail args
                | ParseMsg qNext.str args -> 
                    permissionsCheck UserPermission.Admin qNext.Run genericFail args
                | ParseMsg qAFK.str args -> 
                    permissionsCheck UserPermission.Captain qAFK.Run genericFail args
                | ParseMsg qBan.str args -> 
                    permissionsCheck UserPermission.Captain qBan.Run genericFail args
                | ParseMsg qKick.str args -> 
                    permissionsCheck UserPermission.Captain qKick.Run genericFail args
                | ParseMsg qAdd.str args -> 
                    permissionsCheck UserPermission.Captain qAdd.Run genericFail args
                | ParseMsg qClose.str args -> 
                    permissionsCheck UserPermission.Captain qClose.Run genericFail args
                | ParseMsg qCustoms.str args -> 
                    permissionsCheck UserPermission.None qCustoms.Run genericFail args
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
            | ParseMsg "HI JR" args ->
                permissionsCheck UserPermission.Creator Creator.RunHiJr genericFail args
            
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

