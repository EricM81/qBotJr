namespace qBotJr

open Discord
open Discord.WebSocket
open System
open System.Threading.Tasks

module CommandService =
    
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

    let (|IsRole|_|) (botPerm : UserPermissions) (gUser : SocketGuildUser) = 
        let roles = 
            match botPerm with 
            | UserPermissions.Admin -> config.GetGuildSettings(gUser.Guild.Id).AdminRoles
            | UserPermissions.Captain -> config.GetGuildSettings(gUser.Guild.Id).CaptainRoles
            | _ -> []

        let x = 
            roles
            |> List.exists 
                (fun y -> 
                    gUser.Roles
                    |> Seq.exists 
                        (fun z ->
                            z.Id = y
                        )
                )

        match x with 
        | true -> Some gUser
        | false -> None
        
    type cmdBasicFunc = (SocketMessage) -> unit 

    type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> (UserPermissions) -> unit 

    let noPermissions (msg : SocketMessage) (basicFunc : cmdBasicFunc) : CmdOption<'T> = 
        basicFunc msg
        Completed

    let genericFail (msg : SocketMessage) (channel : SocketGuildChannel) (user : SocketGuildUser) (perm : UserPermissions) : unit =
        Async.AwaitTask(msg.AddReactionAsync(new Emoji(Emojis.Distrust))) |> ignore

    let permissionsCheck (minPerm : UserPermissions) (successFunc : cmdGuildFunc) (failFunc : cmdGuildFunc) (msg : SocketMessage) : CmdOption<'T> =
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

    
   

    //let isAdministratorRole (gUser : SocketGuildUser) : bool option = 
        
    //let getBotPermissions (x : CmdGuildParams) : CmdGuildParams = 
    //    if ( = true) then
    //        {x with BotPermissions = Some (BotPermissions.create true true)}
    //    else
    //        {x with BotPermissions = Some (BotPermissions.create false false)}

   

    


    //cmds I can run for testing....or memeing
    
    
        
    
    
    
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

    let filterCreatorCommands (msg : SocketMessage) : CmdOption<SocketMessage> =
        match msg with
        | StaticCmd "HI JR" msg ->
            permissionsCheck UserPermissions.Creator Creator.RunHiJr genericFail msg
            
        |_ -> Continue msg

    let filterGuildDynamicCommands (msg : SocketMessage) : CmdOption<SocketMessage> =
        Completed

    
        
    let processMsg (inbox: MailboxProcessor<SocketMessage>) = 
        let rec msgLoop() = 
            async{
                let! msg = inbox.Receive()
                Continue msg
                |> bind filterStaticCommands
                |> bind filterCreatorCommands 
                |> bind filterDynamicCommands
                |> ignore
                return! msgLoop()
            }
        msgLoop()

    let agent = MailboxProcessor.Start(processMsg)

    //once a message is handled, it returns 
    let messageReceivedAsync (msg : SocketMessage) = 
        agent.Post msg    
        Task.CompletedTask

