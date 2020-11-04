open qBotJr
        
[<EntryPoint>]
let main (_: string []): int =
   DiscordHelper.initializeClient AsyncService.Receive
   DiscordHelper.startClient
   0 