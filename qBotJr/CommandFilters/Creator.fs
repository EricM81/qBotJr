namespace qBotJr
open Discord.WebSocket
open System

module Creator = 

//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 

    let RunHiJr  (pm : ParsedMsg) (goo : GuildOO) : unit =
        pm.Message.Channel.SendMessageAsync("hey pop!")
        |> Async.AwaitTask
        |> ignore
    
 