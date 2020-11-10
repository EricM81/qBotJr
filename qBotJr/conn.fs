namespace qBotJr

open System
open Discord
open Discord.WebSocket
open System.Threading
open System.Threading.Tasks
open FSharpx.Control
open qBotJr
open qBotJr.T

module conn =

    let clientConfig =
        let intents = GatewayIntents.GuildMessages ||| GatewayIntents.GuildMessageReactions
        let tmp = DiscordSocketConfig()
        tmp.MessageCacheSize <- 100
        //tmp.AlwaysDownloadUsers <- true
        tmp.GatewayIntents <- Nullable<GatewayIntents>(intents)
        tmp

    let restClientOptions =
        let opt = RequestOptions.Default
        opt.Timeout <- Nullable<int> 5000
        opt.RetryMode <- Nullable<RetryMode> RetryMode.AlwaysRetry
        opt

    let client = new DiscordSocketClient(clientConfig)


    let startClient =
        let foo =
            async{
                do! Async.AwaitTask(client.LoginAsync(TokenType.Bot, config.BotSettings.DiscordToken))
                do! Async.AwaitTask(client.StartAsync())
                do! Async.AwaitTask(Task.Delay(Timeout.Infinite))
                return ()
            }
        Async.RunSynchronously foo
