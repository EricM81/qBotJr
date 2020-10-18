﻿namespace qBotJr
open System
open System.Threading.Channels
open Discord
open Discord.WebSocket


[<Struct>]
type UserPermission = 
    | None = 0
    | Captain = 1
    | Admin = 2
    | Creator = 3
    
    
[<Struct>]
type CommandLineArgs =
    {
    Switch : char option
    Values : string list
    }
    static member create prefix values =
        {Switch = prefix; Values = values}

//[<Struct>]
//type CommandLineArgs =
//    {
//    Name : string
//    Arguments : CommandLineOption list
//    }
//    static member create name args =
//        {Name = name; Arguments = args} 

[<Struct>]
type ParsedMsg =
    {
    Message : SocketMessage
    ParsedArgs : CommandLineArgs list
    }
    static member create msg pArgs =
        {Message = msg; ParsedArgs = pArgs}

[<Struct>]
type GuildOO =
    {
    Channel : SocketGuildChannel
    User : SocketGuildUser
    UserPerms : UserPermission
    }
    static member create channel user perms =
        {Channel = channel; User = user; UserPerms = perms}
type UserMessageAction = ParsedMsg -> unit 
type PrivilegedMessageAction = ParsedMsg -> GuildOO -> unit 
type UserReactionAction = (Cacheable<IUserMessage, uint64>) -> (ISocketMessageChannel) ->  (IReaction) -> unit
type ScheduledTask = (int) -> unit

type MessageReaction =
    {
    Message : Cacheable<IUserMessage, uint64>
    Channel : ISocketMessageChannel
    Reaction : IReaction
    }
    static member create msg channel reaction =
        {Message = msg; Channel = channel; Reaction = reaction}


type MailboxMessage =
    | NewMessage of SocketMessage 
    | MessageReaction of MessageReaction 
    | ScheduledTask of ScheduledTask  


   
[<Struct>]
type ReAction = 
    {
    Emoji : string;
    Action : UserReactionAction
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