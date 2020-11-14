open FSharpx.Control
open qBotJr

[<EntryPoint>]
let main (_: string []): int =

    client.InitializeClient commands.creatorFilters commands.staticFilters
    discord.initializeClient client.Receive
    Scheduler.init ()
    discord.startClient ()

    0
