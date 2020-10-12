namespace qBotJr
open Discord.WebSocket
open System

module Creator = 

//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 

    let RunHiJr (msg : SocketMessage) (channel : SocketGuildChannel) (user : SocketGuildUser) (perm : UserPermissions) : unit =
        msg.Channel.SendMessageAsync("hey pop!")
        |> Async.AwaitTask
        |> ignore
    
 