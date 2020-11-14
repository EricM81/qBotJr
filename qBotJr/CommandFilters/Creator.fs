namespace qBotJr

open qBotJr.T


module Creator =

//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit

    let HiJr = Command.create "HI JR" UserPermission.Creator (fun _ g _ -> discord.sendMsg g.Channel "hey pop!" |> ignore; None) discord.reactDistrust

