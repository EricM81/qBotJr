namespace qBotJr
open Discord.WebSocket
open System
open qBotJr.T

module qClose = 

//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 

    let str = "QCLOSE"

    let Run  (pm : ParsedMsg) (goo : GuildOO) : unit =
        ()
    
    let noPerms  (pm : ParsedMsg) (goo : GuildOO) : unit =
        ()