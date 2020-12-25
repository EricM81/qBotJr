namespace qBotJr.T

open System
open Discord
open Discord.WebSocket


//ref type
type CommandLineArgs =
  {
    Switch: char option
    Values: string list
  }

  static member create switch values = {Switch = switch; Values = values}

[<Struct>] //val type
type ParsedMsg =
  {
    Message: SocketMessage
    ParsedArgs: CommandLineArgs list
  }

  static member create msg pArgs = {ParsedMsg.Message = msg; ParsedArgs = pArgs}


[<Struct>] //val type
type GuildOO =
  {
    GuildID: uint64
    Guild: SocketGuild
    Channel: SocketTextChannel
    User: IGuildUser //user of either the socket or rest variant
  }

  static member create user (channel: SocketTextChannel) =
    {GuildID = channel.Guild.Id; Guild = channel.Guild; Channel = channel; User = user}

type NewMessage =
  {
    Goo: GuildOO
    Message: SocketMessage
  }

  static member create msg goo = {NewMessage.Goo = goo; Message = msg}


[<Struct>]
type MessageReaction =
  {
    Goo: GuildOO
    Message: Cacheable<IUserMessage, uint64>
    Reaction: SocketReaction
    IsAdd: bool
  }

  static member create msg reaction isAdd goo =
    {MessageReaction.Goo = goo; Message = msg; Reaction = reaction; IsAdd = isAdd}

//[<Struct>] //val type
//type ActionResult =
//    | Done of unit : unit
//    | UpdateServer of Server : Server

type MessageAction = Server -> GuildOO -> ParsedMsg -> Server option
type ReactionAction = Server -> MessageReaction -> Server option

//ref type
type ReAction =
  {
    Emoji: string
    Action: ReactionAction
  }

  static member create emoji action = {ReAction.Emoji = emoji; Action = action}

//ref type
type ReactionFilter =
  {
    GuildID: uint64
    MessageID: uint64
    mutable TTL: DateTimeOffset
    UserID: uint64 option
    Items: ReAction list
  }

  static member create guild msgID ttl uid items =
    {ReactionFilter.GuildID = guild; MessageID = msgID; TTL = ttl; UserID = uid; Items = items}


//ref type
type Command =
  {
    PrefixUpper: string
    PrefixLength: int
    MinPermission: UserPermission
    PermSuccess: MessageAction
    PermFailure: MessageAction
  }

  static member create (prefix: string) perm success failure =
    {
      PrefixUpper = prefix.ToUpper ()
      PrefixLength = prefix.Length
      MinPermission = perm
      PermSuccess = success
      PermFailure = failure
    }

//ref type
type MessageFilter =
  {
    GuildID: uint64
    mutable TTL: DateTimeOffset
    User: uint64 option
    Items: Command list
  }

  static member create guild ttl user items = {MessageFilter.GuildID = guild; TTL = ttl; User = user; Items = items}
