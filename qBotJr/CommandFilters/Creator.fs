namespace qBotJr

open qBotJr.T


module Creator = 

//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 

    let HiJr = Command.create "HI JR" UserPermission.Creator (fun _ y -> discord.sendMsg y.Channel "hey pop!" |> ignore) discord.reactDistrust
    
        