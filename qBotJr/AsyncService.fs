namespace qBotJr
open Discord.WebSocket
open System
open System.Threading.Tasks
open qBotJr.T

//Async MailboxProcessor wrapper to accept Post commands
module AsyncService =
 
            
        
        
    //thread safe, internal handler for messages from MailboxProcessor.Receive() 
    module private _service =
        
     
        
        
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

        let inline bind2 f g x = 
            match x with 
            | Continue y -> f g y
            | Completed -> Completed

        let inline matchPrefix (prefix : string) (msg : SocketMessage) : bool =
            if msg.Content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then true else false
                        
        let inline parseMsg (cmd : Command) (msg : SocketMessage) =
            Interpreter.parseInput cmd.Prefix msg.Content
            |> ParsedMsg.create msg
            
        let inline (|ParseMsg|_|) (cmd : Command) (msg : SocketMessage) =
            if matchPrefix cmd.Prefix msg then 
                parseMsg cmd msg |> Some
            else
                None

        let inline (|IsCreator|_|) (gUser : SocketGuildUser) = 
            if gUser.Id = 442438729207119892UL then
                Some gUser
            else    
                None

        let inline (|IsAdmin|_|) (gUser : SocketGuildUser) = 
            if gUser.GuildPermissions.Administrator = true then
                Some gUser
            else
                None

        let inline (|IsRole|_|) (role : UserPermission) (gUser : SocketGuildUser) = 
            let hasMatch = 
                match role with 
                | UserPermission.Admin -> config.GetGuildSettings(gUser.Guild.Id).AdminRoles
                | UserPermission.Captain -> config.GetGuildSettings(gUser.Guild.Id).CaptainRoles
                | _ -> []
                |> List.exists (fun serverRoles -> gUser.Roles     //z = user's roles, y = matching role from config
                                                |> Seq.exists (fun userRoles -> userRoles.Id = serverRoles)
                                )

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
            if perm >= cmd.RequiredPerm then 
                cmd.PermSuccess parsedMsg goo
            else 
                cmd.PermFailure parsedMsg goo

            Completed

        let filterStaticCommands (goo : GuildOO) (msg : SocketMessage) : CmdOption<SocketMessage> = 
            //all static bot commands start with a "q"
            //no Q, no need to check
            let q = msg.Content.[0]
            if (q = 'Q' || q = 'q') then 
                
                match msg with 
//                | ParseMsg qBot.str args -> 
//                    permissionsCheck UserPermission.Admin qBot.Run genericFail args
                | ParseMsg qHere.Command args -> 
                    checkPermAndRun goo qHere.Command args
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
                | _ -> Continue msg
            else
                Continue msg
              
        //cmds I can run for testing....or memeing
        let filterCreatorCommands (goo : GuildOO) (msg : SocketMessage) : CmdOption<SocketMessage> =
            match msg with
            | ParseMsg Creator.HiJr args ->
                checkPermAndRun goo Creator.HiJr args
            |_ -> Continue msg

        let filterDynamicCommands (goo : GuildOO) (msg : SocketMessage) : CmdOption<SocketMessage> =
            let now = DateTimeOffset.Now
            let guildID = goo.Guild.Id
            
//            let inline matchMessage (goo : GuildOO) (msg : SocketMessage) (cmd: Command) : CmdOption<SocketMessage> =
//                match msg with
//                | ParseMsg cmd args ->
//                    checkPermAndRun goo cmd args
//                | _ -> Continue msg
       
            let rec matchFilterItem (xs : Command list) : CmdOption<SocketMessage> =
                match xs with
                | [] -> Continue msg
                | x::xs -> 
                    match msg with
                    | ParseMsg x args ->
                        checkPermAndRun goo x args
                    | _ ->  matchFilterItem xs
                    
            let rec matchFilter (xs : MessageFilter list) : CmdOption<SocketMessage> =
                match xs with
                | [] -> Continue msg
                | x::xs when (guildID = x.GuildID && now < x.TTL) -> 
                
                    let items =
                        match x.User with
                        | Some user when user = msg.Author.Id -> x.Items  
                        | Some user when user <> msg.Author.Id -> []  
                        | _ -> x.Items
                    
                    let result = matchFilterItem items
                                        
                    match result with
                    | Completed ->
                        x.TTL <- DateTimeOffset.MinValue
                        Completed
                    | _ -> matchFilter xs
                    
                | _::xs -> matchFilter xs
                
            matchFilter State.MessageFilters
    
        let processMail (inbox: MailboxProcessor<MailboxMessage>) = 
            let rec msgLoop() = 
                async{
                    let! mm = inbox.Receive()
                    match mm with
                    | NewMessage nm ->
                        
                        //Important:  Discord.NET uses a lot of type and interface casting.
                        //If the SocketChannel successfully casts to a SocketGuildChannel then all
                        //other castings will succeed.        

                        match nm.Channel with
                        | :? SocketGuildChannel as gChannel ->
                            let goo = GuildOO.create gChannel.Guild gChannel (nm.Author :?> SocketGuildUser)
                            filterStaticCommands goo nm
                            |> bind2 filterCreatorCommands goo 
                            |> bind2 filterDynamicCommands goo
                            |> ignore
                        | _ -> ()
                        
                            
                    | MessageReaction mr ->
                        
                        () //todo - message reaction filter
                    | MessageFilter mf ->
                        //let gfilters = messageFilters.Item(mf.GuildID)
                        ()
                    | ReactionFilter rf ->
                        ()
                    | ScheduledTask t ->
                        () //TODO - task scheduler
                    return! msgLoop()
                }
            
            msgLoop()
        
        let agent = MailboxProcessor.Start(processMail)
        

    //once a message is handled, it returns 
    let Receive (mm : MailboxMessage) = 
        _service.agent.Post mm    
        Task.CompletedTask
    
    let CurrentServers () =
//        let x =
//            _service.guilds
//            |> Seq.map (fun x -> x)
//        
        
        //State.Guilds
      
        //|> Seq.fold (fun x y -> y)
        //for KeyValue(i,x) in z do
        ()
            
        
        
        
