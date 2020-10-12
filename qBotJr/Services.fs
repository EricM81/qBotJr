namespace qBotJr

open System
open System.IO
open System.Threading.Tasks
open System.Collections.Generic
open Discord
open Discord.Commands
open Discord.WebSocket
open System.Threading
open System.Threading.Tasks
open Newtonsoft.Json
open System.Text

module stuff = 
    
    let clientConfig = 
        let tmp = new DiscordSocketConfig()
        tmp.MessageCacheSize <- 100
        tmp.AlwaysDownloadUsers <- true
        tmp

    let client = new DiscordSocketClient(clientConfig)



    let logAsync (log:LogMessage) = 
        //sprintf "++++ %s" (log.ToString()) |> logger.WriteLine
        Task.CompletedTask

    let readyAsync str = 
        sprintf "**** %s is connected!" (client.CurrentUser.ToString()) |> logger.WriteLine
        Task.CompletedTask

    let messageReceivedAsync (msg : SocketMessage) = 
        let gUser = msg.Author :?> SocketGuildUser 
        let gChannel = msg.Channel :?> SocketGuildChannel            

        match msg.Content.ToUpper() with 
        | "!VERB" ->
            Async.AwaitTask(msg.Channel.SendMessageAsync("get'em")) |> ignore
            Thread.Sleep(500)
            Async.AwaitTask(msg.Channel.SendMessageAsync("got'em, good")) |> ignore
            ()
        | "RIGHT JR?" ->
            if msg.Author.Id = 442438729207119892UL then
                Async.AwaitTask(msg.Channel.SendMessageAsync("right dad")) |> ignore
            else
                Async.AwaitTask(msg.AddReactionAsync(Emote.Parse(Emojis.dict.Item("Distrust")))) |> ignore

        | "THANKS JR" ->
            if msg.Author.Id = 442438729207119892UL then
                Async.AwaitTask(msg.Channel.SendMessageAsync("love ya dad")) |> ignore
            else
                Async.AwaitTask(msg.AddReactionAsync(Emote.Parse(Emojis.dict.Item("Distrust")))) |> ignore
        |"QPING" ->
            Async.AwaitTask(msg.Channel.SendMessageAsync(@"<@!442438729207119892> <@!643435344355917854> <@!257947314625314816>")) |> ignore
            ()
        |"QHERE" ->
            let x = Async.AwaitTask(msg.Channel.SendMessageAsync("blah blah blah\n\n👌\tOk\n❌\tCancel \n👈\tBack \n🖕\tFuck Off")) 
            let msg2 = Async.RunSynchronously(x)

            logger.WriteLine "+_+_+_+_+_+_+_ Starting react test"
            [Emojis.Ok ; Emojis.Cancel ; Emojis.Back ; Emojis.FU]
            |> Seq.iter 
                (fun x -> 
                    logger.WriteLine ((DateTime.Now.ToString("mm:ss:ffff")) + " - " + x )
                    Thread.Sleep 700
                    Async.AwaitTask(msg2.AddReactionAsync(new Emoji(x)))
                    |> ignore
                ) 
        |"QTEST" ->
            
            let guild = gChannel.Guild
            logger.WriteLine (DateTime.Now.ToString("mm:ss:ffff"))
           
            guild.Users
            |> Seq.iteri (fun i (x : SocketGuildUser) -> sprintf "%i\t%s - %s - %s" i x.Username x.Nickname (x.Status.ToString()) |> logger.WriteLine)
            
            logger.WriteLine (DateTime.Now.ToString("mm:ss:ffff"))

            ()

            //ROLE STUFF
            //let roles = Seq.toList gChannel.Guild.Roles
            //           let rec loop acc (items : SocketRole list) = 
            //               match items with 
            //               | head::tail -> 
            //                   match head.Name.Chars(0) with 
            //                   | '@' -> loop (acc) tail
            //                   | _ -> loop (acc + head.Name + "  ") tail
                               
            //               | [] -> (acc)
            //           let text = loop "" roles
            //           Async.AwaitTask(msg.Channel.SendMessageAsync(text))
            //           |> ignore
            //for item in Emojis.dict.Values
            //logger.WriteLine (DateTime.Now.ToString("mm:ss:ffff"))
            //logger.WriteLine pair()




            //let gChannel = msg.Channel :?> SocketGuildChannel
            //let guild = gChannel.Guild
            //let emotes = List.ofSeq guild.Emotes
            
            //let rec row (acc : string) (i : int) (emotes' : GuildEmote list) : string =
            //    let str = 
            //        match emotes' with 
            //        | head :: tail when i < 9 -> (row (acc + (head.ToString())) (i + 1) tail)
            //        | head :: tail -> acc + (head.ToString())
            //        | [] -> acc
            //    str

            //Async.AwaitTask(msg.Channel.SendMessageAsync(row "" 0 emotes)) |> ignore   
            

            //Async.AwaitTask(msg.Channel.SendMessageAsync(sprintf "React with %s if you want to play sub games!")) |> ignore
//        | "QBOT" ->
//            Async.AwaitTask(msg.Channel.SendMessageAsync(Test.qBotMain)) |> ignore
//            ()
        | _ -> 
            match msg.Author.Id with 
            | 442438729207119892UL -> logger.WriteLine msg.Content
            | _ -> ()
            ()

        //sprintf "    %s (%i) - %s (%i)\n    %s" msg.Author.Username msg.Author.Id msg.Channel.Name msg.Channel.Id msg.Content |> logger.WriteLine
        Task.CompletedTask
        

        //async {
        //    printfn "%s (%i) - %s" msg.Author.Username msg.Author.Id msg.Content
        //    return Task.CompletedTask

        //}
    let messageUpdatedAsync (before : Cacheable<IMessage, uint64>) (after : SocketMessage) (channel : ISocketMessageChannel) = 
        //sprintf "    +-  %s (%i) - %s (%i)\n    %s" after.Author.Username after.Author.Id after.Channel.Name after.Channel.Id after.Content |> logger.WriteLine
        Task.CompletedTask

    let messageReactedAsync (msg : Cacheable<IUserMessage, uint64>) (channel : ISocketMessageChannel) (reaction : IReaction) = 
        //sprintf "%s    react  %s (msg %i) - %s" (DateTime.Now.ToString("mm:ss:ffff")) channel.Name msg.Id reaction.Emote.Name |> logger.WriteLine
        Task.CompletedTask
        
    let run =
     
        client.add_Log (fun log -> logAsync log)
        client.add_Ready (fun _ -> readyAsync "")
        client.add_MessageReceived (fun msg -> messageReceivedAsync msg )
        client.add_MessageUpdated (fun before after channel -> messageUpdatedAsync before after channel)
        client.add_ReactionAdded (fun msg channel reaction -> messageReactedAsync msg channel reaction )
        
        //sprintf "%s" config.BotSettings.DiscordToken |> logger.WriteLine 
        let foo = 
            async{
                let x = Async.AwaitTask(client.LoginAsync(TokenType.Bot, config.BotSettings.DiscordToken))
                let y = Async.AwaitTask(client.StartAsync())
                let! z = Async.AwaitTask(Task.Delay(Timeout.Infinite))
                return ()
            }

        Async.RunSynchronously foo

       
        //let z = Async.AwaitTask(client.LoginAsync(TokenType.Bot, config.BotSettings.DiscordToken))
        //let y = Async.AwaitTask(client.StartAsync()) 
        //let x = Async.AwaitTask(Task.Delay(Timeout.Infinite))
        
        //sprintf "Didn't block before runSync- %s" (x.ToString()) |> logger.WriteLine

        //Async.RunSynchronously x

        //sprintf "after runSync- %s" (x.ToString()) |> logger.WriteLine

        ////System.Threading.Thread.Sleep(Timeout.Infinite)
       
        




        //async {
        //    let! result = client.LoginAsync(TokenType.Bot, configSettings.discoConnSettings.DiscoToken)
        //    return! result
        //}
        



    
    