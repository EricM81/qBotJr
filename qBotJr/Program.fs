﻿// Learn more about F# at http://fsharp.org

open qBotJr




        
[<EntryPoint>]
let main (argv: string []): int =

   DiscordHelper.initializeClient AsyncService.Receive
   DiscordHelper.startClient

   0 // return an integer exit code
