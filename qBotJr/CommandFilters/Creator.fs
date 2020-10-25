namespace qBotJr

open qBotJr.T


module Creator = 

//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 

    let RunHiJr  (pm : ParsedMsg) (goo : GuildOO) : unit =
        pm.Message.Channel.SendMessageAsync("hey pop!")
        |> Async.AwaitTask
        |> ignore
    
 