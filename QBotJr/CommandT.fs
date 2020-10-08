namespace qBotJr
open Discord.WebSocket
open System

type Action<'T> = ('T -> unit)


[<Struct>]
type Reaction<'T> = 
    {
    Emoji : string;
    State : 'T;
    Action : Action<'T>
    }
    static member create (reaction, state, action) =
        {Reaction.Emoji = reaction; State = state; Action = action}

[<Struct>]
type ReactionFilter<'T> = 
    {
    GuildID: uint64;
    MsgID : uint64;
    Reactions : Reaction<'T> list;
    }
    static member create (guildID, msgID, reactions) =
        {ReactionFilter.GuildID = guildID; MsgID = msgID; Reactions = reactions}

[<Struct>]
type UserReactionFilter<'T> = 
    {
    GuildID: uint64;
    MsgID : uint64;
    UserID : uint64;
    Reactions : Reaction<'T> list;

    }
    static member create (guildID, msgID, userID, reactions) = 
        {UserReactionFilter.GuildID = guildID; MsgID = msgID; UserID = userID; Reactions = reactions}

[<Struct>]
type UserResponseFilter<'T> = 
    {
        ChannelID : uint64;
        UserID : uint64;
        State : 'T;
        Action : Action<'T>

    }
//[<Struct>]
//type
[<Struct>]
type CmdParams = 
    {
        Msg : SocketMessage 
    }
    static member create msg = 
        {CmdParams.Msg = msg}

[<Struct>]
type BotPermissions = 
    | Unknown = 0
    | None = 1
    | Captain = 2
    | Admin = 3
    | Creator = 4

[<Struct>]
type CmdGuildParams = 
    {
    Msg : SocketMessage
    GuildUser : SocketGuildUser;
    GuildChannel : SocketGuildChannel;
    BotPermissions : BotPermissions
    }
    static member create msg gUser gChannel botPerms =
        {CmdGuildParams.Msg = msg; GuildUser = gUser; GuildChannel = gChannel; BotPermissions = botPerms}

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
