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
    Goo : GuildOO
    Message : Cacheable<IUserMessage, uint64>
    Reaction : SocketReaction
    IsAdd : bool
    }
    static member create msg reaction isAdd goo =
        {MessageReaction.Goo = goo; Message = msg; Reaction = reaction; IsAdd = isAdd}

type ActionResult =
    | Done of Done : unit
    | Async of Async : Async<unit>
    | Server of Server : Server

type MessageAction = Server -> GuildOO -> ParsedMsg -> ActionResult
type ReactionAction = Server -> MessageReaction -> ActionResult

[<Struct>]
type ReAction =
    {

    Emoji : string
    Action : ReactionAction
    }
    static member create emoji action =
        {ReAction.Emoji = emoji; Action = action}

type ReactionFilter =
    {
    GuildID : uint64
    MessageID : uint64
    mutable TTL : DateTimeOffset
    UserID : uint64 option
    Items : ReAction list
    }
    static member create guild msgID ttl uid items =
        {ReactionFilter.GuildID = guild; MessageID = msgID; TTL = ttl; UserID = uid; Items = items}


[<Struct>]
type Command =
    {
    PrefixUpper : string
    PrefixLength : int
    RequiredPerm : UserPermission
    PermSuccess : MessageAction
    PermFailure : MessageAction
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


