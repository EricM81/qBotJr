namespace qBotJr

open Discord.WebSocket
open System.Threading.Tasks
open System

module CommandInterpreter =
    
    [<StructuralEquality; StructuralComparison>]
    [<Struct>]
    type CmdOption<'T> = 
        | Continue of Continue : 'T 
        | Completed 

    let bind f x = 
        match x with 
        | Continue y -> f y
        | Completed -> Completed

    let (|StaticCmd|_|) (p:string) (s:string) =
        if s.StartsWith(p, StringComparison.OrdinalIgnoreCase) then
            Some p
        else
            None

    let (|IsAdmin|_|) (x : SocketGuildUser) = 
        if x.GuildPermissions.Administrator = true then
            Some true
        else
            None

    //let (|IsRole|_|) (x : BotPermissions) (g : SocketGuildUser) = 
        

    //qNew <@!442438729207119892>		qNew @QButtsSr
    //qnew <@!679018301543677959>		qnew @Queue Bot
    //qnew <@!760644805969313823>		qnew @QButtsJr
    //qnew <@!255387600246800395> 	qnew @hoooooowdyhow 
    //qnew -a <#544636678954811392>	qnew -a #sub_chat_announcements
    //qnew 👌 <:kamiUHH:715006017506639962>	qnew :ok_hand: :kamiUHH:
    //qnew <@!643435344355917854>		qnew @paniniham
    //qnew <@!257947314625314816>		qnew @Indulgence82

   

    //let isAdministratorRole (gUser : SocketGuildUser) : bool option = 
        
    //let getBotPermissions (x : CmdGuildParams) : CmdGuildParams = 
    //    if ( = true) then
    //        {x with BotPermissions = Some (BotPermissions.create true true)}
    //    else
    //        {x with BotPermissions = Some (BotPermissions.create false false)}

    //chance to do some pre filtering if needed
    let getBasicParams (msg : SocketMessage) : CmdOption<CmdParams> = 
        CmdParams.create msg
        |> Continue

    //cmds I can run for testing....or memeing
    let filterSuperAdminCommands (cmd : CmdParams) : CmdOption<CmdParams> =
        let superadmin = cmd.Msg.Author.Id = 442438729207119892UL
        match cmd.Msg.Content with
        | StaticCmd "hi jr" content ->
            Completed
        |_ -> Continue cmd
        
    
    //check for static commands that don't require a guild context or specific permissions
    let filterBasicStaticCommands (cmd : CmdParams) : CmdOption<CmdParams> = 
        //might add commands that anyone can run, like countdown to next games
        Continue cmd

    let filterBasicDynamicCommands (cmd : CmdParams) : CmdOption<CmdParams> =
        //looking for a response to a request.  
        //all authentication needs to be done on the request
        Completed

    //all static bot commands start with a "q"
    //no Q, no need to continue
    let qCheck (cmd : CmdParams) : CmdOption<CmdParams> =
        let q = cmd.Msg.Content.[0]
        if (q = 'Q' || q = 'q') then 
            Continue cmd
        else 
            Completed

    let getGuildParams (cmd : CmdParams) : CmdOption<CmdGuildParams> = 
        match cmd.Msg.Author with
        | :? SocketGuildUser as gUser  ->
            match cmd.Msg.Channel with
            | :? SocketGuildChannel as gChannel ->
                CmdGuildParams.create cmd.Msg gUser gChannel BotPermissions.Unknown
                |> Continue
            | _ -> Completed    
        | _ -> Completed

    let filterGuildStaticCommands (cmd : CmdGuildParams) : CmdOption<CmdGuildParams> = 
        
        if (cmd.GuildUser.GuildPermissions.Administrator = true) then 
            match cmd.Msg.Content with 
            | StaticCmd "QBOT" content ->
                Completed
            | StaticCmd "QHERE" content ->
                Completed
            | StaticCmd "QNEW" content -> 
                Completed
            | StaticCmd "QMODE" content -> 
                Completed
            | StaticCmd "QUNBAN" content -> 
                Completed
            | StaticCmd "QSET" content -> 
                Completed
            | StaticCmd "QNEW" content -> 
                Completed
            | StaticCmd "QNEW" content -> 
                Completed
            | _ -> Continue cmd
        else
            Continue cmd

    let filterGuildDynamicCommands (cmd : CmdGuildParams) : CmdOption<CmdGuildParams> =
        Completed

    
        
    let processMsg (inbox: MailboxProcessor<SocketMessage>) = 
        let rec msgLoop() = 
            async{
                let! msg = inbox.Receive()
                msg
                |> getBasicParams 
                |> bind filterSuperAdminCommands
                |> bind filterBasicStaticCommands
                |> bind filterBasicDynamicCommands
                |> bind qCheck
                |> bind getGuildParams
                |> bind filterGuildStaticCommands
                |> ignore
                return! msgLoop()
            }
        msgLoop()

    let agent = MailboxProcessor.Start(processMsg)

    //once a message is handled, it returns 
    let messageReceivedAsync (msg : SocketMessage) = 
        agent.Post msg    
        Task.CompletedTask

