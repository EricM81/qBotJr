open qBotJr

[<EntryPoint>]
let main (_: string []): int =

    AsyncClient.InitializeClient commands.creatorFilters commands.staticFilters
    discord.initializeClient AsyncClient.Receive
    discord.startClient
    0
