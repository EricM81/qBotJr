namespace qBotJr
open System.Text
open Discord.WebSocket
open System
open Discord.WebSocket

module qHere = 


//-c Announcement channel for players
//   Current Value:   -c #sub_chat_announcements

//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 
    let str = "QHERE"
    
    let lastAnnounceChannel (channel : SocketGuildChannel) =
        let announceID = config.GetGuildSettings(channel.Guild.Id).AnnounceChannel
        discoHelper.getChannelByID channel.Guild announceID
    let wrapn (sb : StringBuilder) (str : Printf.StringFormat<'T>) =
        sb.AppendLine str.Value
    
    let printMan (channel : SocketGuildChannel option) : string =
        let sb = new StringBuilder()
        let append format = Printf.kprintf (fun s -> sb.AppendLine s |> ignore) format
        
        
        append ">>> __Post a message to a channel (-a) and ping @ everyone (-e), @ here (-h), or no one (-n).__"
        append ""
        append "It's best to use a read-only, announcement style channel. The channel's permission determine who gets to play."
        append "```announcements = everyone, sub_announcements = subs, etc.```"
        append "Over time, people will leave.  You can re-run qHere for a fresh count."
        append "```This will not reset the \"games played\" stat."
        append "The bot remembers 'till it goes to sleep (1 hr without a qNew).```"

        append "```qHere -e|-h|-n -a #your_channel"
        append ""
        append "Pick One:"
        append "-e Ping @ everyone"
        append "-h Ping @ here"
        append "-n Ping no one, just post"
        append ""
        append "-a Announcement channel."
        match channel with
        | Some c -> 
            append "   Current Value: #%s" c.Name
            append "   This will be used if you omit the -a, but "
            append "   you always have to specify who to ping."
        | None ->
            append "   Current Value: None"
            append "   Your last used value will be stored here, but"
            append "   you have to provide a channel on the first run."
        append "```"
        
        
        
        sb.ToString()
        
   
    let Run (pm : ParsedMsg) (goo : GuildOO) : unit =
        //qhere
        //qhere -a <#544636678954811392>
        //qhere <#544636678954811392>
        //-c <#544636678954811392> -e -h 
//        
//        let x = lastAnnounceChannel channel
//        let gs = config.GetGuildSettings(channel.Guild.Id)
//        let gs' = {gs with AnnounceChannel = Some 760644069562646569UL; LobbiesCategory = Some 760643898833371146UL}
//        //config.SetGuildSettings gs'                   
//        
//
        
        (goo.Channel.Guild.Id
        |> config.GetGuildSettings).AnnounceChannel
        |> discoHelper.getChannelByID goo.Channel.Guild
        |> printMan
        |> discoHelper.sendMsg goo.Channel 
//        
//       
//        match pm.ParsedArgs with
//        | Some x, 
//        
//        ()
//        
    let noPerms (pm : ParsedMsg) (goo : GuildOO) : unit =
        ()