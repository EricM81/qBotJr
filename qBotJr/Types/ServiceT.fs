namespace qBotJr.T
open System
open Discord
open Discord.WebSocket
open Discord.Rest


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
    IsHere : bool
    }
    static member create goo msg reaction isAdd =
        {MessageReaction.Goo = goo; Message = msg; Reaction = reaction; IsHere = isAdd}

type MessageAction = ParsedMsg -> unit
type GuildMessageAction = ParsedMsg -> GuildOO -> unit
type ReactionAction = MessageReaction -> unit
type AsyncTask<'T> = delegate of 'T -> Async<unit>


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

type Player =
    {
    UID : uint64
    Name : string
    mutable GamesPlayed : byte
    mutable isHere : bool
    mutable isBanned : bool
    }
    static member create  uid name=
        {Player.UID = uid; Name = name; GamesPlayed = 1uy; isHere = true; isBanned = false}

type Lobby =
    {
    Name : string
    Channel : SocketGuildChannel
    mutable PlayerIDs : uint64 list
    }
    //static member create


type HereMessage<'T> =
    {
    MessageID : uint64
    Emoji : string
    RestMsg : RestMessage
    ModifyAsync : AsyncTask<'T>
    ReAction : ReAction
    }

type Mode<'T> =
    {
    Name : string
    HereMsg : HereMessage<'T>
    mutable PlayerIDs : uint64 list
    mutable PlayerListIsDirty : bool
    }

type Server =
    {
    Guild : SocketGuild
    mutable TTL : DateTimeOffset
    mutable HereMsg : HereMessage<Server> option
    mutable Lobbies : Lobby list
    mutable Players : Player list
    mutable PlayerListIsDirty : bool
    mutable Modes : Mode<Server> list
    }


