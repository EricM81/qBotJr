namespace qBotJr
open Discord
open Discord.WebSocket

module discoHelper =
    let getRolesByIDs (channel : SocketGuildChannel) (ids : uint64 list) : SocketRole list = 
        let rec getRolesByIDsInner (roles : uint64 list) (acc : SocketRole list) =
            match roles with
            | [] -> acc
            | head::tail ->
                let y = channel.Guild.Roles |> Seq.find (fun x -> x.Id = head)
                getRolesByIDsInner tail (y::acc)
        getRolesByIDsInner ids []
    
    let getCategoryNameByID (channel : SocketGuildChannel) (id : uint64 option) : string =
        match id with
        | Some x ->
            let cat = channel.Guild.CategoryChannels |> Seq.tryFind (fun y -> y.Id = x)
            match cat with
            | Some z -> z.Name
            | None -> ""
        | None -> ""
    