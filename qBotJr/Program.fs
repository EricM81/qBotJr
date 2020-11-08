open qBotJr

[<EntryPoint>]
let main (_: string []): int =

    AsyncClient.InitializeClient commands.creatorFilters commands.staticFilters
    DiscordHelper.initializeClient AsyncClient.Receive
    DiscordHelper.startClient
    0
