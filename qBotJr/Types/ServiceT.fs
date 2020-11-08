﻿namespace qBotJr.T
open System
open Discord
open Discord.WebSocket

//
//Mailbox Types
//
[<Struct>]
type GuildOO =
    {
    Guild : SocketGuild
    Channel : SocketTextChannel
    User : IGuildUser //don't ask
    }
    static member create guild channel user  =
        {Guild = guild;Channel = channel; User = user}

type NewMessage =
    {
    GuildOO : GuildOO
    Message : SocketMessage
    }
    static member create goo msg =
        {NewMessage.GuildOO = goo; Message = msg}


[<Struct>]
type MessageReaction =
    {
    GuildOO : GuildOO
    Message : Cacheable<IUserMessage, uint64>
    Reaction : SocketReaction
    IsHere : bool
    }
    static member create goo msg reaction isAdd =
        {MessageReaction.GuildOO = goo; Message = msg; Reaction = reaction; IsHere = isAdd}

//
//New Message Types
//
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



//
//partial application types
//
type MessageAction = ParsedMsg -> unit
type GuildMessageAction = ParsedMsg -> GuildOO -> unit
type ReactionAction = MessageReaction -> unit


//
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
    static member create (msgID, reactions) =
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

type ReactionFilter =
    {
    GuildID : uint64
    mutable TTL : DateTimeOffset
    FilterChoice : ReactionFilterChoice
    }
    static member create guild ttl item =
        {ReactionFilter.GuildID = guild; TTL = ttl; FilterChoice = item}



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


[<Struct>]
type State =
    {
    mutable Guilds : Map<uint64, Server>
    mutable CreatorFilters : Command array
    mutable StaticFilters : Command array
    mutable DynamicFilters : MessageFilter list
    mutable ReactionFilters : ReactionFilter list
    }
    static member create  =
        {State.Guilds = Map.empty; CreatorFilters = Array.empty; StaticFilters = Array.empty; DynamicFilters = []; ReactionFilters = []}



type ScheduledTask = delegate of byref<State> -> unit

type MailboxMessage =
    | NewMessage of NewMessage //: NewMessage
    | MessageReaction  of MessageReaction //: MessageReaction
    | Task of ScheduledTask //: ScheduledTask
    static member createMessage goo msg : MailboxMessage =
        NewMessage (NewMessage.create msg goo)
    static member createReaction  msg reaction isAdd goo : MailboxMessage=
        MessageReaction (MessageReaction.create goo msg reaction isAdd)
    static member createTask i : MailboxMessage =
        Task i

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
