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

    //client.add_Log (fun log -> logAsync log)
    //client.add_Ready (fun _ -> readyAsync "")
    client.add_MessageReceived (fun msg -> CommandService.messageReceivedAsync msg )
    //client.add_MessageUpdated (fun before after channel -> messageUpdatedAsync before after channel)
    //client.add_ReactionAdded (fun msg channel reaction -> messageReactedAsync msg channel reaction )
        
        //sprintf "%s" config.BotSettings.DiscordToken |> logger.WriteLine 
    let foo = 
        async{
            let x = Async.AwaitTask(client.LoginAsync(TokenType.Bot, config.BotSettings.DiscordToken))
            let y = Async.AwaitTask(client.StartAsync())
            let! z = Async.AwaitTask(Task.Delay(Timeout.Infinite))
            return ()
        }

    Async.RunSynchronously foo
    
    stuff.run |> ignore





    0 // return an integer exit code
