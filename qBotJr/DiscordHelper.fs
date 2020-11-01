namespace qBotJr

open System.Text
open System.Threading.Tasks.Sources
open Discord
open Discord.WebSocket
open System.Threading
open System.Threading.Tasks
open qBotJr.T

module DiscordHelper =
    
    
    module private helper =
        let clientConfig = 
            let tmp = new DiscordSocketConfig()
            tmp.MessageCacheSize <- 100
            tmp.AlwaysDownloadUsers <- true
            tmp
        
        let client = new DiscordSocketClient(clientConfig)
        
        let stripUID (prefix : string) (suffix : char) (value : string) : uint64 option=
            let len = value.Length
            let preLen = prefix.Length 
            if (value.StartsWith(prefix)) then
                let tmp = (value.Substring((prefix.Length ), (len - preLen - 1)))
                match (System.UInt64.TryParse tmp) with
                | true, uid -> Some uid
                | _ -> None
            else
                None
    
   
    
    let initializeClient receiveFunc =
        helper.client.add_Log (fun log ->
            logger.WriteLine (sprintf "%s\n%s\n" log.Source log.Message)
            Task.CompletedTask)
        
        helper.client.add_Ready (fun _ ->
            logger.WriteLine "Ready to receive...\n"
            Task.CompletedTask)
      
        helper.client.add_MessageReceived
            (fun msg ->
                receiveFunc (MailboxMessage.createMessage msg))
       
        helper.client.add_ReactionAdded (fun msg channel reaction ->
            receiveFunc (MailboxMessage.createReaction msg channel reaction))
        
        helper.client.add_ReactionRemoved (fun msg channel reaction ->
            receiveFunc (MailboxMessage.createReaction msg channel reaction))
        
        //TODO start scheduler
        //TODO listen for can't connect and disconnects and try agane after one minute
    
    let startClient =
        //TODO: change all let -> let!
        let foo = 
            async{
                Async.AwaitTask(helper.client.LoginAsync(TokenType.Bot, config.BotSettings.DiscordToken))
                |> ignore
                Async.AwaitTask(helper.client.StartAsync())
                |> ignore
                do! Async.AwaitTask(Task.Delay(Timeout.Infinite))
                return ()
            }
        
        Async.RunSynchronously foo
  
    let parseDiscoUser (name : string) : uint64 option =
        let prefix = "<@!"
        let suffix = '>'
        helper.stripUID prefix suffix name
        
    let parseDiscoChannel (name : string) : uint64 option =
        let prefix = "<#"
        let suffix = '>'
        helper.stripUID prefix suffix name
     
    let getRolesByIDs (guild : SocketGuild) (ids : uint64 list) : SocketRole list = 
        let rec getRolesByIDsInner (roles : uint64 list) (acc : SocketRole list) =
            match roles with
            | [] -> acc
            | head::tail ->
                let y = guild.Roles |> Seq.find (fun x -> x.Id = head)
                getRolesByIDsInner tail (y::acc)
        getRolesByIDsInner ids []
    
    let getCategoryByID (guild : SocketGuild) (id : uint64 option) : SocketCategoryChannel option  =
        match id with
        | Some x ->
            let cat = guild.GetCategoryChannel x
            match cat with
            | null -> None
            | y -> Some y
        | None -> None
        
    let getCategoryByName (guild : SocketGuild) (name : string) : SocketCategoryChannel option =
        guild.CategoryChannels |> Seq.tryFind (fun y -> y.Name = name)
        
        
    let getChannelByID (id : uint64 option) : SocketGuildChannel option =
       
        match id with
        | Some x ->
            let channel = helper.client.GetChannel x
            match channel with
            | null -> None
            | y ->
                match y with
                | :? SocketGuildChannel as z -> Some z
                | _ -> None
        | None -> None
        
        
    let getChannelByName (guild : SocketGuild) (name : string) : SocketGuildChannel option =
        guild.Channels |> Seq.tryFind (fun y -> y.Name = name)
        
    let sendMsg (channel : SocketChannel) (msg : string) =
        match channel with
        | :? SocketTextChannel as x -> x.SendMessageAsync msg |> Some
        | _ -> None
            
    let reactDistrust (parsedM : ParsedMsg) (goo : GuildOO) : unit =
        Emojis.Distrust
        |> Emoji
        |> parsedM.Message.AddReactionAsync
        |> Async.AwaitTask
        |> ignore
        
    let pingToString (p : PingType) =
        match p with
        | PingType.Everyone -> "@everyone"
        | PingType.Here -> "@here"
        | PingType.NoOne -> ""
    
    let bprintfn (sb : StringBuilder) =
        Printf.kprintf (fun s -> sb.AppendLine s |> ignore)
        