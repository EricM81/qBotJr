﻿namespace qBotJr
open Discord.WebSocket
open System

module qCustoms = 

//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 

    let Run (msg : SocketMessage) (channel : SocketGuildChannel) (user : SocketGuildUser) (perm : UserPermissions) : unit =
        ()
    
    let noPerms (msg : SocketMessage) (channel : SocketGuildChannel) (user : SocketGuildUser) (perm : UserPermissions) : unit =
        ()