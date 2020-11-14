﻿namespace qBotJr.T

open System
open Discord
open Discord.WebSocket
open Discord.Rest

[<Struct>]
type Player =
    {
        ID : uint64
        Name : string
    }
    static member create (user : IGuildUser) =
        let name = if (String.IsNullOrEmpty user.Nickname) then user.Username else user.Nickname
        { ID = user.Id; Name = name }
//ref type
type PlayerHere =
    {
    Player : Player
    mutable GamesPlayed : int16
    mutable isHere : bool
    mutable isBanned : bool
    }
    static member create (user : IGuildUser) here =
        { PlayerHere.Player = Player.create user ; GamesPlayed = 0s ; isHere = here ; isBanned = false }

//ref type
type Lobby = { Name : string ; Channel : SocketGuildChannel ; mutable Players : Player list }

[<Struct>] //val type
type HereMessage =
    {
    MessageID : uint64
    Emoji : string
    RestMsg : RestUserMessage
    Header : string
    }
    static member create (restMsg : RestUserMessage) emoji annHeader =
        {MessageID = restMsg.Id; Emoji = emoji; RestMsg = restMsg; Header = annHeader}

//ref type
type Mode =
    {
    Name : string
    HereMsg : HereMessage
    Players : Player list
    PlayerListIsDirty : bool
    }

//ref type
type Server =
    {
    GuildID : uint64
    Guild : SocketGuild
    mutable TTL : DateTimeOffset
    HereMsg : HereMessage option
    PlayersHere : PlayerHere list
    mutable PlayerListIsDirty : bool
    Lobbies : Lobby list
    Modes : Mode list
    }
    static member create (guild : SocketGuild) =
        {Server.GuildID = guild.Id; Guild = guild; TTL = DateTimeOffset.Now.AddHours(1.0); HereMsg = None; Lobbies = []; PlayersHere = []; PlayerListIsDirty = false; Modes = []}

//val type
type PingType =
    | Everyone = 0
    | Here = 1
    | NoOne = 2

[<Struct>] //val type
type qBotParameters =
    {
    AdminRoles : uint64 list
    CaptainRoles : uint64 list
    LobbiesCategory : uint64 option
    }
    static member create admins captains cat = { AdminRoles = admins ; CaptainRoles = captains ; LobbiesCategory = cat }

[<Struct>] //val type
type qHereArgs =
    {
    AnnounceID : uint64 option
    Ping : PingType option
    }
    static member create  announcements ping = { qHereArgs.Ping = ping ; AnnounceID = announcements }

[<Struct>] //val type
type qHereArgsValidated =
    {
    Announcements : SocketTextChannel
    Ping : PingType
    Emoji : string
    }
    static member create announcements ping emoji = { qHereArgsValidated.Ping = ping ; Announcements = announcements; Emoji = emoji}
