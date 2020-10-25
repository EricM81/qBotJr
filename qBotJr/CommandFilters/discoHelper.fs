namespace qBotJr
//open Discord
//open Discord
//open Discord.WebSocket
//open Discord.WebSocket
//open Discord.WebSocket
//open Discord.WebSocket
//open Discord.WebSocket
//open Discord.WebSocket
//
//
////qNew <@!442438729207119892>		qNew @QButtsSr
//    //qnew <@!679018301543677959>		qnew @Queue Bot
//    //qnew <@!760644805969313823>		qnew @QButtsJr
//    //qnew <@!255387600246800395> 	qnew @hoooooowdyhow 
//    //qnew -a <#544636678954811392>	qnew -a #sub_chat_announcements
//    //qnew 👌 <:kamiUHH:715006017506639962>	qnew :ok_hand: :kamiUHH:
//    //qnew <@!643435344355917854>		qnew @paniniham
//    //qnew <@!257947314625314816>		qnew @Indulgence82
//    //https://www.google.com/search?q=current+time+zone
//
////[<Struct>]
////type DiscordEntity =
////    {
////    ID : uint64 ;
////    Name : string
////    }
////    static member create id name =
////        {ID = id; Name = name}
////        
////
//
//
//module discoHelper =
//    
//    let stripUID (prefix : string) (suffix : char) (value : string) : uint64 option=
//        let len = value.Length
//        let preLen = prefix.Length 
//        if (value.StartsWith(prefix)) then
//            let tmp = (value.Substring((prefix.Length ), (len - preLen - 1)))
//            match (System.UInt64.TryParse tmp) with
//            | true, uid -> Some uid
//            | _ -> None
//        else
//            None
//    
//    let parseDiscoUser (name : string) : uint64 option =
//        let prefix = "<@!"
//        let suffix = '>'
//        stripUID prefix suffix name
//        
//    let parseDiscoChannel (name : string) : uint64 option =
//        let prefix = "<#"
//        let suffix = '>'
//        stripUID prefix suffix name
//        
//    let getRolesByIDs (guild : SocketGuild) (ids : uint64 list) : SocketRole list = 
//        let rec getRolesByIDsInner (roles : uint64 list) (acc : SocketRole list) =
//            match roles with
//            | [] -> acc
//            | head::tail ->
//                let y = guild.Roles |> Seq.find (fun x -> x.Id = head)
//                getRolesByIDsInner tail (y::acc)
//        getRolesByIDsInner ids []
//    
//    let getCategoryByID (guild : SocketGuild) (id : uint64 option) : SocketCategoryChannel option  =
//        match id with
//        | Some x ->
//            let cat = guild.GetCategoryChannel x
//            match cat with
//            | null -> None
//            | y -> Some y
//        | None -> None
//        
//    let getCategoryByName (guild : SocketGuild) (name : string) : SocketCategoryChannel option =
//        guild.CategoryChannels |> Seq.tryFind (fun y -> y.Name = name)
//        
//        
//    let getChannelByID (guild : SocketGuild) (id : uint64 option) : SocketGuildChannel option =
//        guild.Roles
//        |> Seq.iter (fun x ->
//            sprintf "%i - %s" x.Id x.Name
//            |> logger.WriteLine
//            )
//        
//        match id with
//        | Some x ->
//            let channel' = guild.GetChannel x
//            match channel' with
//            | null -> None
//            | y -> Some y
//        | None -> None
//        
//    let getChannelByName (guild : SocketGuild) (name : string) : SocketGuildChannel option =
//        guild.Channels |> Seq.tryFind (fun y -> y.Name = name)
//        
//    let sendMsg (channel : SocketChannel) (msg : string) =
//        let iChannel = channel :> IChannel
//        match iChannel with
//        | :? ISocketMessageChannel as x ->
//            x.SendMessageAsync(msg)
//            |> Async.AwaitTask
//            |> ignore
//        |_ -> ()
//                      
//    