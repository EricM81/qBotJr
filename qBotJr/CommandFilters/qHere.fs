namespace qBotJr
open System.Net.NetworkInformation
open System.Text
open System.Threading.Channels
open Discord.WebSocket
open qBotJr.T



module qHere = 

//TODO: Save RestUserMessage for updating: ModifyAsync Action<MessageProperties> RequestOptions
    
    
//-c Announcement channel for players
//   Current Value:   -c #sub_chat_announcements

//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 
    
    
    let lastAnnounceChannel (guild : SocketGuild) =
        config.GetGuildSettings(guild.Id).AnnounceChannel

    let updateAnnounceChannel (guild : SocketGuild) (channelID : uint64 option) =
        let cfg = config.GetGuildSettings guild.Id
        if cfg.AnnounceChannel <> channelID then
            {cfg with AnnounceChannel = channelID} |> config.SetGuildSettings
            
    
    let printMan (channelID : uint64 option) : string =
        let channel = DiscordHelper.getChannelByID channelID

        let sb = new StringBuilder()
        let a format = Printf.kprintf (fun s -> sb.AppendLine s |> ignore) format
        
        
        a ">>> __Post a message to a channel (-a) and ping @ everyone (-e), @ here (-h), or no one (-n).__"
        a ""
        a "It's best to use a read-only, announcement style channel. The channel's permission determine who gets to play."
        a "```announcements = everyone, sub_announcements = subs, etc.```"
        a "Over time, people will leave.  You can re-run qHere for a fresh count."
        a "```This will not reset the \"games played\" stat."
        a "The bot remembers 'till it goes to sleep (1 hr without a qNew).```"

        a "```qHere -e|-h|-n -a #your_channel"
        a ""
        a "Pick One:"
        a "-e Ping @ everyone"
        a "-h Ping @ here"
        a "-n Ping no one, just post"
        a ""
        a "-a Announcement channel."
        match channel with
        | Some c -> 
            a "   Current Value: #%s" c.Name
            a "   This will be used if you omit the -a, but "
            a "   you always have to specify who to ping."
        | None ->
            a "   Current Value: None"
            a "   Your last used value will be stored here, but"
            a "   you have to provide a channel on the first run."
        a "```"
        
        
        
        sb.ToString()
        
    let postMan (goo : GuildOO) (param : qHereParameters) =
        DiscordHelper.sendMsg goo.Channel (printMan param.Announcements)
        
    let postAnnouncement (ping : PingType) (channelID : uint64) =
        
        ()
        
    let rec checkParams (xs : CommandLineArgs list) (prev : qHereParameters) : qHereParameters =
        //example input
        //qhere
        //qhere -a <#544636678954811392>
        //qhere <#544636678954811392>
        //-a <#544636678954811392> -e -h 
        match xs with
        | [] -> prev
        | x::xs when x.Switch = Some 'E' -> checkParams xs {prev with Ping = Some PingType.Everyone}
        | x::xs when x.Switch = Some 'H' -> checkParams xs {prev with Ping = Some PingType.Here} 
        | x::xs when x.Switch = Some 'N' -> checkParams xs {prev with Ping = Some PingType.NoOne} 
        | x::xs when x.Values <> [] ->
            match (DiscordHelper.parseDiscoChannel x.Values.Head) with
            | Some i -> {prev with Announcements = Some i}
            | _ -> prev
            |> checkParams xs
        | _ -> //ignore invalid input?
            checkParams xs prev
    
                
    let ReRun (previous : qHereParameters) (pm : ParsedMsg) (goo : GuildOO) : unit =
        ()
        
    let Run (pm : ParsedMsg) (goo : GuildOO) : unit =
        
        
//        let x = lastAnnounceChannel channel
//        let gs = config.GetGuildSettings(channel.Guild.Id)
//        let gs' = {gs with AnnounceChannel = Some 760644069562646569UL; LobbiesCategory = Some 760643898833371146UL}
        //config.SetGuildSettings gs'                   
        
        let param =
            lastAnnounceChannel goo.Guild
            |> qHereParameters.create  None 
            |> checkParams pm.ParsedArgs
       
        updateAnnounceChannel goo.Guild param.Announcements
        
        match param with
        | { Ping = Some p; Announcements = Some a} ->
            postAnnouncement p a
        | { Ping = p; Announcements = a } ->
            postMan goo param
            
            //TODO register dynamic listener
            
            
            
    
    let noPerms (pm : ParsedMsg) (goo : GuildOO) : unit =
        ()
        
    let Command = Command.create "QHERE" UserPermission.Admin Run DiscordHelper.reactDistrust
    