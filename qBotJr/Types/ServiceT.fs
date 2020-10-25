namespace qBotJr.T
open System
open System.Threading.Channels
open Discord
open Discord.WebSocket


//Mailbox Types

type NewMessage = SocketMessage

[<Struct>]    
type MessageReaction =
    {
    Message : Cacheable<IUserMessage, uint64>
    Channel : ISocketMessageChannel
    Reaction : IReaction
    }
    static member create msg channel reaction =
        {MessageReaction.Message = msg; Channel = channel; Reaction = reaction}

//TODO scheduled task definition
type ScheduledTask =
    {
    x : int        
    }
    

        

//New Message Types
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


[<Struct>]
type ParsedMsg =
    {
    
    Message : NewMessage
    ParsedArgs : CommandLineArgs list
    }
    static member create  msg pArgs =
        {ParsedMsg.Message = msg; ParsedArgs = pArgs}       
    

[<Struct>]
type GuildOO =
    {
    Guild : SocketGuild
    Channel : SocketGuildChannel
    User : SocketGuildUser
    }
    static member create guild channel user  =
        {Guild = guild;Channel = channel; User = user}
 

 
//partial application types
        
type MessageAction = ParsedMsg -> unit 
type GuildMessageAction = ParsedMsg -> GuildOO -> unit 
type ReactionAction = MessageReaction -> unit
type TaskAction = DateTime -> unit




//Dynamic filters and their partial applications
   
[<Struct>]
type ReAction = 
    {
    Emoji : string;
    Action : ReactionAction
    }
    static member create reaction action =
        {ReAction.Emoji = reaction; Action = action}

[<Struct>]
type ByReaction = 
    {
    MsgID : uint64;
    Actions : ReAction list;
    }
    static member create (guildID, msgID, reactions) =
        {ByReaction.MsgID = msgID; Actions = reactions}

[<Struct>]
type ByReactionAndUser = 
    {
    MsgID : uint64;
    UserID : uint64;
    Actions : ReAction list;

     } 
    static member create (msgID, userID, reactions) = 
        {ByReactionAndUser.MsgID = msgID; UserID = userID; Actions = reactions}
        
         
[<Struct>]
type ReactionFilterChoice =
    | ByReaction of ByReaction : ByReaction
    | ByReactionAndUser of ByReactionAndUser : ByReactionAndUser
    
[<Struct>]
type ReactionFilter =
    {
    GuildID : uint64
    mutable TTL : DateTime
    Item : ReactionFilterChoice
    }
    static member create guild ttl item =
        {ReactionFilter.GuildID = guild; TTL = ttl; Item = item}
          
[<Struct>]
type Command =
    {
    Prefix : string
    RequiredPerm : UserPermission
    PermSuccess : GuildMessageAction
    PermFailure : GuildMessageAction
    }
    static member create prefix perm success failure =
        {Prefix = prefix; RequiredPerm = perm; PermSuccess = success; PermFailure = failure}

[<Struct>]
type MessageFilter =
    {
    GuildID : uint64
    mutable TTL : DateTimeOffset
    User : uint64 option
    Items : Command list
    }
    static member create guild ttl user items =
        {MessageFilter.GuildID = guild; TTL = ttl; User = user; Items = items}

[<Struct>]    
type MailboxMessage =
    | NewMessage of NewMessage : NewMessage
    | MessageReaction  of MessageReaction : MessageReaction
    | MessageFilter of MessageFilter : MessageFilter
    | ReactionFilter of ReactionFilter : ReactionFilter
    | ScheduledTask of ScheduledTask : ScheduledTask
    static member createMessage msg : MailboxMessage =
        NewMessage msg
    static member createReaction msg channel reaction : MailboxMessage=
        MessageReaction (MessageReaction.create msg channel reaction)
    static member createMessageFilter guild ttl perms item : MailboxMessage =
        MessageFilter (MessageFilter.create  guild ttl perms item )
     static member createReactionFilter guild ttl item : MailboxMessage =
        ReactionFilter (ReactionFilter.create guild ttl item)
    static member createTask i : MailboxMessage =
        ScheduledTask {ScheduledTask.x = i}
        
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
//    
//[<Struct>]
//type ByPrefix =
//    {
//    Prefix : string
//    Success : GuildMessageAction
//    Failure : GuildMessageAction
//    }
//    static member create prefix success failure =
//        {ByPrefix.Prefix = prefix; Success = success; Failure = failure}
//    
//[<Struct>]
//type ByPrefixAndUser = 
//    {   
//    UserID : uint64;
//    Prefix : string;
//    Success : GuildMessageAction
//    Failure : GuildMessageAction
//
//    }
//    static member create user prefix success failure =
//        {ByPrefixAndUser.UserID = user; Prefix = prefix; Success = success; Failure = failure}
//    
//[<Struct>]
//type MessageFilterChoice =
//    | ByPrefix of ByPrefix : ByPrefix
//    | ByPrefixAndUser of ByPrefixAndUser : ByPrefixAndUser