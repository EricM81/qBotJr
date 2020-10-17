namespace qBotJr
open Discord.WebSocket
open System

module qNext = 
//https://www.google.com/search?q=time+zone
    
//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 

    let Run (msg : SocketMessage) (channel : SocketGuildChannel) (user : SocketGuildUser) (perm : UserPermissions) : unit =
        ()
    
    let noPerms (msg : SocketMessage) (channel : SocketGuildChannel) (user : SocketGuildUser) (perm : UserPermissions) : unit =
        ()