namespace qBotJr
open System.Threading.Channels
open Discord.WebSocket
open System
open discoHelper

module qBot = 
    //qNew <@!442438729207119892>		qNew @QButtsSr
    //qnew <@!679018301543677959>		qnew @Queue Bot
    //qnew <@!760644805969313823>		qnew @QButtsJr
    //qnew <@!255387600246800395> 	qnew @hoooooowdyhow 
    //qnew -a <#544636678954811392>	qnew -a #sub_chat_announcements
    //qnew 👌 <:kamiUHH:715006017506639962>	qnew :ok_hand: :kamiUHH:
    //qnew <@!643435344355917854>		qnew @paniniham
    //qnew <@!257947314625314816>		qnew @Indulgence82
    //https://www.google.com/search?q=current+time+zone
      
    let qBotMain = @"Server Settings

You can change all the settings at once or just one at a time.  

```-a Admin roles that can execute qBot, qHere, qNew, qGameMode, qSetUser
   Current Value:   -a ""Kami Commander"" ""Kami Mods!""

-c Captain roles that can execute qAdd, qAFK, qLeave, qBan, qClose
   Current Value:   -c ""Fake Moderator""

-p Players per game
   Current Value:   -p 9

-c Announcement channel for players
   Current Value:   -c #sub_chat_announcements

-l Lobbies are created in this category
   Current Value:   -l QSTUFF```

You can also type a parameter prefix for assistance.  For example calling ""-a"" will print a list of server roles to copy and paste."
//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 

    let Run (msg : SocketMessage) (channel : SocketGuildChannel) (user : SocketGuildUser) (perm : UserPermissions) : unit =
        let settings = config.GetGuildSettings channel.Guild.Id
        let adminRoles = getRolesByIDs channel settings.AdminRoles
        let captainRoles = getRolesByIDs channel settings.CaptainRoles
        
        
        
        ()
    
    let noPerms (msg : SocketMessage) (channel : SocketGuildChannel) (user : SocketGuildUser) (perm : UserPermissions) : unit =
        ()
        
    