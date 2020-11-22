open FSharpx.Control
open qBotJr
open FSharp.Control.Tasks.V2

[<EntryPoint>]
let main (_: string []): int =

  client.registerFilters commands.creatorFilters commands.staticFilters
  discord.registerEvents ()
  discord.startClient () |> ignore
  Scheduler.init () |> Async.RunSynchronously
  0
