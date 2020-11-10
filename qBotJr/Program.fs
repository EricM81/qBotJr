open System
open qBotJr
open qBotJr.T
open System.Threading.Tasks
open Discord
open Discord.WebSocket

let downCastMsgOrIgnore (msg : SocketMessage) =
    match msg.Author with
    | :? SocketGuildUser as gUser ->
        let gChannel = msg.Channel :?> SocketTextChannel

        GuildOO.create gChannel.Guild gChannel gUser
        |> MailboxMessage.createMessage msg
        |> client.Receive
    | _ ->
        //TODO check logs for things that fail to cast and remove later
        sprintf "NewMessage failed to cast:" |> logger.WriteLine
        sprintf "%O" msg.Author |> logger.WriteLine
        logger.WriteLine ""
    Task.CompletedTask


let downCastReactionOrIgnore (msg : Cacheable<IUserMessage, uint64>) (_ : ISocketMessageChannel) (sReaction : SocketReaction) (isAdd : bool) =
    let inline foo (sReaction : SocketReaction) msg iUser isAdd : unit =
        let gChannel = sReaction.Channel :?> SocketTextChannel
        GuildOO.create gChannel.Guild gChannel iUser
        |> MailboxMessage.createReaction msg sReaction isAdd
        |> client.Receive

    match sReaction.User.Value with
    | :? IGuildUser as iUser ->
        foo sReaction msg iUser isAdd
    | _ ->
        async {
            //TODO check logs for things that fail to cast and remove later
            sprintf "Reaction User Missing:" |> logger.WriteLine
            sprintf "%O" sReaction.User |> logger.WriteLine

            let! rUser =
                conn.client.Rest.GetGuildUserAsync(sReaction.UserId, sReaction.UserId, conn.restClientOptions)
                |> Async.AwaitTask
            do foo sReaction msg rUser isAdd

            sprintf "%O" rUser |> logger.WriteLine
            logger.WriteLine ""

        }
        |> Async.Start
    Task.CompletedTask

let addReaction (msg : Cacheable<IUserMessage,uint64>) (channel : ISocketMessageChannel) (reaction : SocketReaction) : Task =
    downCastReactionOrIgnore msg channel reaction true

let removeReaction (msg : Cacheable<IUserMessage,uint64>) (channel : ISocketMessageChannel) (reaction : SocketReaction) : Task =
    downCastReactionOrIgnore msg channel reaction false

[<EntryPoint>]
let main (_: string []): int =

    client.InitializeClient commands.creatorFilters commands.staticFilters

    conn.client.add_Log (Func<_,_>(logger.WriteConnectionLog))
    conn.client.add_Ready (fun _ ->
        logger.WriteLine "Ready to receive...\n"
        Task.CompletedTask)
    conn.client.add_MessageReceived (Func<_,_> (downCastMsgOrIgnore))
    conn.client.add_ReactionAdded (Func<_,_,_,_> (addReaction))
    conn.client.add_ReactionRemoved (Func<_,_,_,_> (removeReaction))

    //TODO start scheduler
    //TODO listen for can't connect and disconnects and try agane after one minute

    0
