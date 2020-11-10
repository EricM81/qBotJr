namespace qBotJr.T
open Discord
open Discord.WebSocket

//pure .NET or Discord.NET types


[<Struct>]
type UserPermission =
    | None = 0
    | Captain = 1
    | Admin = 2
    | Creator = 3

type PingType =
    | Everyone
    | Here
    | NoOne

type AsyncTask<'T> = delegate of byref<'T> -> Async<unit>


[<Struct>]
type GuildOO =
    {
    Guild : SocketGuild
    Channel : SocketTextChannel
    User : IGuildUser //user of either socket or rest client
    }
    static member create guild channel user  =
        {Guild = guild;Channel = channel; User = user}

type NewMessage =
    {
    Goo : GuildOO
    Message : SocketMessage
    }
    static member create goo msg =
        {NewMessage.Goo = goo; Message = msg}


[<Struct>]
type MessageReaction =
    {
    Goo : GuildOO
    Message : Cacheable<IUserMessage, uint64>
    Reaction : SocketReaction
    IsHere : bool
    }
    static member create goo msg reaction isAdd =
        {MessageReaction.Goo = goo; Message = msg; Reaction = reaction; IsHere = isAdd}

[<Struct>]
type CommandLineArgs =
    {
    Switch : char option
    Values : string list
    }
    static member create switch values =
        {Switch = switch; Values = values}

[<Struct>]
type ParsedMsg =
    {
    Message : SocketMessage
    ParsedArgs : CommandLineArgs list
    }
    static member create  msg pArgs =
        {ParsedMsg.Message = msg; ParsedArgs = pArgs}


////Impure .NET and Discord.NET types
//
//[<Struct>]
//type MessageReaction =
//    {
//    Goo : GuildOO
//    Message : Cacheable<IUserMessage, uint64>
//    Reaction : SocketReaction
//    IsHere : bool
//    }
//    static member create goo msg reaction isAdd =
//        {MessageReaction.Goo = goo; Message = msg; Reaction = reaction; IsHere = isAdd}
//
//
//[<Struct>]
//type ParsedMsg =
//    {
//
//    Message : SocketMessage
//    ParsedArgs : CommandLineArgs list
//    }
//    static member create  msg pArgs =
//        {ParsedMsg.Message = msg; ParsedArgs = pArgs}
//
//type MessageAction = ParsedMsg -> unit
//type GuildMessageAction = ParsedMsg -> GuildOO -> unit
//type ReactionAction = MessageReaction -> unit
//
//[<Struct>]
//type ReAction =
//    {
//    MessageID : uint64
//    Emoji : string
//    Action : ReactionAction
//    }
//    static member create msgID reaction action =
//        {ReAction.MessageID = msgID ;Emoji = reaction; Action = action}
//
//type ReactionFilter =
//    {
//    MessageId : uint64
//    mutable TTL : DateTimeOffset
//    UserID : uint64 option
//    Items : ReAction list
//    }
//    static member create mid ttl uid items =
//        {ReactionFilter.MessageId = mid; TTL = ttl; UserID = uid; Items = items}
//
//[<Struct>]
//type Command =
//    {
//    PrefixUpper : string
//    PrefixLength : int
//    RequiredPerm : UserPermission
//    PermSuccess : GuildMessageAction
//    PermFailure : GuildMessageAction
//    }
//    static member create (prefix : string) perm success failure =
//        {PrefixUpper = prefix.ToUpper(); PrefixLength = prefix.Length; RequiredPerm = perm; PermSuccess = success; PermFailure = failure}
//
//type MessageFilter =
//    {
//    GuildID : uint64
//    mutable TTL : DateTimeOffset
//    User : uint64 option
//    Items : Command list
//    }
//    static member create guild ttl user items =
//        {MessageFilter.GuildID = guild; TTL = ttl; User = user; Items = items}
