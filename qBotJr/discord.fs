namespace qBotJr

open System
open System.Text
open Discord
open Discord.WebSocket
open System.Threading
open System.Threading.Tasks
open FSharpx.Control
open qBotJr
open qBotJr.T

module discord =

    let clientConfig =
        let intents = GatewayIntents.GuildMessages ||| GatewayIntents.GuildMessageReactions
        let tmp = DiscordSocketConfig()
        tmp.MessageCacheSize <- 100
        //tmp.AlwaysDownloadUsers <- true
        tmp.GatewayIntents <- Nullable<GatewayIntents>(intents)
        tmp

    let restClientOptions =
        let opt = RequestOptions.Default
        opt.Timeout <- Nullable<int> 5000
        opt.RetryMode <- Nullable<RetryMode> RetryMode.AlwaysRetry
        opt

    let client = new DiscordSocketClient(clientConfig)

    module private helper =


        let stripUID (prefix : string) (suffix : char) (value : string) : uint64 option =
            let len = value.Length
            let preLen = prefix.Length
            if (value.StartsWith(prefix)) && (value.EndsWith(suffix)) then
                let tmp = (value.Substring((prefix.Length), (len - preLen - 1)))
                match (UInt64.TryParse tmp) with
                | true, uid -> Some uid
                | _ -> None
            else
                None

        let downCastMsgOrIgnore (receiveFun : MailboxMessage -> unit) (msg : SocketMessage) =
            match msg.Author with
            | :? SocketGuildUser as gUser ->
                let gChannel = msg.Channel :?> SocketTextChannel

                GuildOO.create gChannel.Guild gChannel gUser |> MailboxMessage.createMessage msg |> receiveFun
            | _ ->
                //TODO check logs for things that fail to cast and remove later
                sprintf "NewMessage failed to cast:" |> logger.WriteLine
                sprintf "%O" msg.Author |> logger.WriteLine
                logger.WriteLine ""
            Task.CompletedTask

        let downCastReactionOrIgnore
            (receiveFun : MailboxMessage -> unit)
            (msg : Cacheable<IUserMessage, uint64>)
            (_ : ISocketMessageChannel)
            (sReaction : SocketReaction)
            (isHere : bool)
            =
            let inline foo receiveFun (sReaction : SocketReaction) msg iUser isHere : unit =
                let gChannel = sReaction.Channel :?> SocketTextChannel
                GuildOO.create gChannel.Guild gChannel iUser
                |> MailboxMessage.createReaction msg sReaction isHere
                |> receiveFun

            match sReaction.User.Value with
            | :? IGuildUser as iUser -> foo receiveFun sReaction msg iUser isHere
            | _ ->
                async {
                    //TODO check logs for things that fail to cast and remove later
                    sprintf "Reaction User Missing:" |> logger.WriteLine
                    sprintf "%O" sReaction.User |> logger.WriteLine

                    let! rUser =
                        client.Rest.GetGuildUserAsync(sReaction.UserId, sReaction.UserId, restClientOptions)
                        |> Async.AwaitTask

                    do foo receiveFun sReaction msg rUser isHere

                    sprintf "%O" rUser |> logger.WriteLine

                    logger.WriteLine ""
                }
                |> Async.Start
            Task.CompletedTask

    let initializeClient (receiveFun : MailboxMessage -> unit) =
        client.add_Log (fun log ->
            logger.WriteLine(sprintf "%s\n%s\n" log.Source log.Message)
            Task.CompletedTask)

        client.add_Ready (fun _ ->
            logger.WriteLine "Ready to receive...\n"
            Task.CompletedTask)

        //Important:  Discord.NET uses a lot of type and interface casting.
        //If the SocketChannel successfully casts to a SocketGuildChannel then all
        //other castings will succeed.
        client.add_MessageReceived (fun msg -> helper.downCastMsgOrIgnore receiveFun msg)
        client.add_ReactionAdded (fun msg channel reaction ->
            helper.downCastReactionOrIgnore receiveFun msg channel reaction true)
        client.add_ReactionRemoved (fun msg channel reaction ->
            helper.downCastReactionOrIgnore receiveFun msg channel reaction false)

    //TODO start scheduler
    //TODO listen for can't connect and disconnects and try agane after one minute

    let startClient =
        let foo =
            async {
                do! Async.AwaitTask(client.LoginAsync(TokenType.Bot, config.BotSettings.DiscordToken))
                do! Async.AwaitTask(client.StartAsync())
                do! Async.AwaitTask(Task.Delay(Timeout.Infinite))
                return ()
            }

        Async.RunSynchronously foo

    let parseDiscoUser (name : string) : uint64 option =
        let prefix = "<@!"
        let suffix = '>'
        helper.stripUID prefix suffix name

    let parseDiscoChannel (name : string) : uint64 option =
        let prefix = "<#"
        let suffix = '>'
        helper.stripUID prefix suffix name

    let getRolesByIDs (guild : SocketGuild) (ids : uint64 list) : SocketRole list =
        let rec getRolesByIDsInner (roles : uint64 list) (acc : SocketRole list) =
            match roles with
            | [] -> acc
            | head :: tail ->
                let y = guild.Roles |> Seq.find (fun x -> x.Id = head)
                getRolesByIDsInner tail (y :: acc)

        getRolesByIDsInner ids []

    let getCategoryByID (guild : SocketGuild) (id : uint64 option) : SocketCategoryChannel option =
        match id with
        | Some x ->
            let cat = guild.GetCategoryChannel x
            match cat with
            | null -> None
            | y -> Some y
        | None -> None

    let getCategoryByName (guild : SocketGuild) (name : string) : SocketCategoryChannel option =
        guild.CategoryChannels |> Seq.tryFind (fun y -> y.Name = name)


    let getChannelByID (id : uint64 option) : SocketGuildChannel option =

        match id with
        | Some x ->
            let channel = client.GetChannel x
            match channel with
            | null -> None
            | y ->
                match y with
                | :? SocketGuildChannel as z -> Some z
                | _ -> None
        | None -> None


    let getChannelByName (guild : SocketGuild) (name : string) : SocketGuildChannel option =
        guild.Channels |> Seq.tryFind (fun y -> y.Name = name)

    let sendMsg (channel : SocketChannel) (msg : string) =
        match channel with
        | :? SocketTextChannel as x -> x.SendMessageAsync msg |> Async.AwaitTask |> Some
        | _ -> None

    let reactDistrust (parsedM : ParsedMsg) (_ : GuildOO) : unit =
        emojis.Distrust |> Emoji |> parsedM.Message.AddReactionAsync |> Async.AwaitTask |> ignore

    let pingToString (p : PingType) =
        match p with
        | PingType.Everyone -> "@everyone"
        | PingType.Here -> "@here"
        | PingType.NoOne -> ""

    let bprintfn (sb : StringBuilder) = Printf.kprintf (fun s -> sb.AppendLine s |> ignore)
