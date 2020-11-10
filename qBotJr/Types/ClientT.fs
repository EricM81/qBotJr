namespace qBotJr.T
open System
open Discord
open Discord.WebSocket


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

[<Struct>]
type UserPermission =
    | None = 0
    | Captain = 1
    | Admin = 2
    | Creator = 3

[<Struct>]
type GuildOO =
    {
    Guild : SocketGuild
    Channel : SocketTextChannel
    User : IGuildUser //user of either the socket or rest variant
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
    Message : Cacheable<IUserMessage, uint64>
    Reaction : SocketReaction
    IsAdd : bool
    }
    static member create msg reaction isAdd =
        {MessageReaction.Message = msg; Reaction = reaction; IsAdd = isAdd}

type MessageAction = Server -> ParsedMsg -> GuildOO -> unit
type ReactionAction = Server -> MessageReaction -> GuildOO -> unit

[<Struct>]
type ReAction =
    {
    MessageID : uint64
    Emoji : string
    Action : ReactionAction
    }
    static member create msgID reaction action =
        {ReAction.MessageID = msgID ;Emoji = reaction; Action = action}

type ReactionFilter =
    {
    MessageId : uint64
    mutable TTL : DateTimeOffset
    UserID : uint64 option
    Items : ReAction list
    }
    static member create mid ttl uid items =
        {ReactionFilter.MessageId = mid; TTL = ttl; UserID = uid; Items = items}

[<Struct>]
type Command =
    {
    PrefixUpper : string
    PrefixLength : int
    RequiredPerm : UserPermission
    PermSuccess : GuildMessageAction
    PermFailure : GuildMessageAction
    }
    static member create (prefix : string) perm success failure =
        {PrefixUpper = prefix.ToUpper(); PrefixLength = prefix.Length; RequiredPerm = perm; PermSuccess = success; PermFailure = failure}

type MessageFilter =
    {
    GuildID : uint64
    mutable TTL : DateTimeOffset
    User : uint64 option
    Items : Command list
    }
    static member create guild ttl user items =
        {MessageFilter.GuildID = guild; TTL = ttl; User = user; Items = items}


