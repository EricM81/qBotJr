// Learn more about F# at http://fsharp.org

open System
open qBotJr
open System.IO
open Discord
open Discord.WebSocket
open System.Threading.Tasks
open System.Threading

let clientConfig = 
    let tmp = new DiscordSocketConfig()
    tmp.MessageCacheSize <- 100
    tmp.AlwaysDownloadUsers <- true
    tmp

let client = new DiscordSocketClient(clientConfig)
        
[<EntryPoint>]
let main argv =

    client.add_Log (fun log ->
        logger.WriteLine (sprintf "%s\n%s\n" log.Source log.Message)
        Task.CompletedTask)
    
    client.add_Ready (fun _ ->
        logger.WriteLine "Ready to receive...\n"
        Task.CompletedTask)
    
    client.add_MessageReceived (fun msg ->
        AsyncService.Receive (NewMessage msg)
        )
    //client.add_MessageUpdated (fun before after channel -> messageUpdatedAsync before after channel)
    client.add_ReactionAdded (fun msg channel reaction ->
        AsyncService.Receive (MessageReaction (MessageReaction.create msg channel reaction))
        )
    //TODO start scheduler
    //TODO listen for can't connect and disconnects and try agane after one minute
        
     
    let foo = 
        async{
            let x = Async.AwaitTask(client.LoginAsync(TokenType.Bot, config.BotSettings.DiscordToken))
            let y = Async.AwaitTask(client.StartAsync())
            let! z = Async.AwaitTask(Task.Delay(Timeout.Infinite))
            return ()
        }
    
    Async.RunSynchronously foo
    
    




    0 // return an integer exit code
