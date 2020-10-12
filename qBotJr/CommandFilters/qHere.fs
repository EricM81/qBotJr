namespace qBotJr
open Discord.WebSocket
open System

module qHere = 

//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 

    let Run (msg : SocketMessage) (channel : SocketGuildChannel) (user : SocketGuildUser) (perm : UserPermissions) : unit =
        msg.Channel.SendMessageAsync(sprintf "Hi %s!!" user.Nickname)
        |> ignore
    
    let noPerms (msg : SocketMessage) (channel : SocketGuildChannel) (user : SocketGuildUser) (perm : UserPermissions) : unit =
        ()