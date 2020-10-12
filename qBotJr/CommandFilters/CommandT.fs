namespace qBotJr
open Discord.WebSocket
open System

type Action = SocketMessage -> unit

    

[<Struct>]
type ReAction = 
    {
    Emoji : string;
    Action : Action
    }
    static member create reaction action =
        {ReAction.Emoji = reaction; Action = action}

[<Struct>]
type ReActionFilter = 
    {
    GuildID: uint64;
    MsgID : uint64;
    Reactions : ReAction list;
    }
    static member create (guildID, msgID, reactions) =
        {ReActionFilter.GuildID = guildID; MsgID = msgID; Reactions = reactions}

[<Struct>]
type UserReActionFilter = 
    {
    GuildID: uint64;
    MsgID : uint64;
    UserID : uint64;
    Reactions : ReAction list;

    }
    static member create (guildID, msgID, userID, reactions) = 
        {UserReActionFilter.GuildID = guildID; MsgID = msgID; UserID = userID; Reactions = reactions}

[<Struct>]
type UserMessageFilter = 
    {   
    GuildID: uint64;
    UserID : uint64;
    prefix : string;
    TTL : DateTime;
    Action : Action
    }

[<Struct>]
type UserPermissions = 
    | None = 0
    | Captain = 1
    | Admin = 2
    | Creator = 3


//[<Struct>]
//type CmdParams = 
//    {
//        Msg : SocketMessage;
//        //UserPermissions : UserPermissions;
//
//    }
//    static member create msg = 
//        {CmdParams.Msg = msg}


//[<Struct>]
//type CmdGuildParams = 
//    {
//    Msg : SocketMessage
//    GuildUser : SocketGuildUser;
//    GuildChannel : SocketGuildChannel;
//    //BotPermissions : BotPermissions
//    //BotPermissions = botPerms
//    }
//    static member create msg gUser gChannel botPerms =
//        {CmdGuildParams.Msg = msg; GuildUser = gUser; GuildChannel = gChannel; }

//type ActionBasic = (CmdParams -> unit)
//type ActionGuild = (CmdGuildParams -> unit)



//[<Struct>]
//type DynamicCommandBasic = 
//    {
//    CmdParams : CmdParams
//    Action : ActionBasic
//    }
//    static member create cmdParams action = 
//        {DynamicCommandBasic.CmdParams = cmdParams; Action = action}



//[<Struct>]
//type CommandUserChannel = {
//    User : uint64;
//    Channel : uint64;
//    Expires : DateTime
//    Action : Action
//    }

//[<Struct>]
//type CommandUser = {
//    Command : string;
//    Action : Action
//}

//type DynamicFilter = {
//    User : DiscordEntity
//    //Channel

//}
