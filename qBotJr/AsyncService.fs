namespace qBotJr
open System.Collections.Generic
open Discord
open Discord.WebSocket
open System
open System.Threading.Tasks
open qBotJr.T

//Async MailboxProcessor wrapper to accept Post commands
module AsyncService =
    //thread safe, internal handler for messages from MailboxProcessor.Receive() 
    module private _command =
        
        let mutable msgF : MessageFilter list = []
        let mutable reactionF : ReactionFilter list = []
        
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

        let inline matchPrefix (prefix : string) (msg : SocketMessage) : bool =
            if msg.Content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then true else false
                        
        let inline parseMsg (cmd : Command) (msg : SocketMessage) =
            Interpreter.parseInput cmd.Prefix msg.Content
            |> ParsedMsg.create msg
            
        let (|ParseMsg|_|) (cmd : Command) (msg : SocketMessage) =
            if matchPrefix cmd.Prefix msg then 
                parseMsg cmd msg |> Some
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
       
       

        let permissionsCheck (cmd : Command) (parsedMsg : ParsedMsg) : CmdOption<'T> =
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
                    if perm >= cmd.RequiredPerm then 
                        GuildOO.create gChannel.Guild gChannel gUser perm |> cmd.PermSuccess parsedMsg  
                    else 
                        GuildOO.create gChannel.Guild gChannel gUser perm |> cmd.PermFailure parsedMsg

                    Completed

                | _ -> Completed    
            | _ -> Completed
        
        let filterStaticCommands (msg : SocketMessage) : CmdOption<SocketMessage> = 
            //all static bot commands start with a "q"
            //no Q, no need to check
            let q = msg.Content.[0]
            if (q = 'Q' || q = 'q') then 
                
                match msg with 
//                | ParseMsg qBot.str args -> 
//                    permissionsCheck UserPermission.Admin qBot.Run genericFail args
                | ParseMsg qHere.Command args -> 
                    permissionsCheck qHere.Command args
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
        
        let inline matchMessage (msg : SocketMessage) (cmd: Command) : CmdOption<'T> =
            match msg with
            | ParseMsg cmd args ->
                permissionsCheck cmd args
            | _ -> Continue msg
           
        let filterDynamicCommands (msg : SocketMessage) : CmdOption<SocketMessage> =
            let rec findMatch (xs : MessageFilter list) : CmdOption<SocketMessage> =
                //goo upfront
                
                match xs with
                | [] -> Continue msg
                | x::xs when x.TTL > msg.CreatedAt -> 
                
                    let cmds =
                        match x.User with
                        | Some u when u = msg.Author.Id -> x.Items  
                        | _ -> x.Items
                    
                    
                    match result with
                    | 
    //
    
    
    // if isMatch then
//                        permissionsCheck UserPermission.None action genericFail args
//                    else
//                        findMatch xs
                        
            findMatch msgF    
            

        //cmds I can run for testing....or memeing
        let filterCreatorCommands (msg : SocketMessage) : CmdOption<SocketMessage> =
            match msg with
            | ParseMsg "HI JR" args ->
                permissionsCheck UserPermission.Creator Creator.RunHiJr genericFail args
            
            |_ -> Continue msg

        let filterGuildDynamicCommands (msg : SocketMessage) : CmdOption<SocketMessage> =
            Completed

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
                            
                            
                        Continue nm
                        |> bind filterStaticCommands
                        |> bind filterCreatorCommands 
                        |> bind filterDynamicCommands
                        |> ignore
                    | MessageReaction mr ->
                        
                        () //todo - message reaction filter
                    | MessageFilter mf ->
                        msgF <- mf::msgF
                    | ReactionFilter rf ->
                        reactionF <- rf::reactionF
                    | ScheduledTask t ->
                        () //TODO - task scheduler
                    return! msgLoop()
                }
            msgLoop()

        
        let agent = MailboxProcessor.Start(processMail)
        

    //once a message is handled, it returns 
    let Receive (mm : MailboxMessage) = 
        _command.agent.Post mm    
        Task.CompletedTask

