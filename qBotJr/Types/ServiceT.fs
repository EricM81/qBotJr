namespace qBotJr.T
open System

type MessageAction = ParsedMsg -> GuildOO -> unit
type ReactionAction = MessageReaction -> unit

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

