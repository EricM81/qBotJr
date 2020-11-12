﻿namespace qBotJr.T

open System
open Discord.WebSocket
open Discord.Rest

type Player =
    {
    UID : uint64
    Name : string
    mutable GamesPlayed : byte
    mutable isHere : bool
    mutable isBanned : bool
    }
    static member create uid name =
        { Player.UID = uid ; Name = name ; GamesPlayed = 1uy ; isHere = true ; isBanned = false }

type Lobby = { Name : string ; Channel : SocketGuildChannel ; mutable PlayerIDs : uint64 list }

type HereMessage = { MessageID : uint64 ; Emoji : string ; RestMsg : RestMessage }

type Mode =
    {
    Name : string
    HereMsg : HereMessage
    PlayerIDs : uint64 list
    PlayerListIsDirty : bool
    }

type Server =
    {
    Guild : SocketGuild
    mutable TTL : DateTimeOffset
    HereMsg : HereMessage option
    Lobbies : Lobby list
    Players : Player list
    mutable PlayerListIsDirty : bool
    Modes : Mode list
    }
    static member create guild =
        {Guild = guild; TTL = DateTimeOffset.Now.AddHours(1.0); HereMsg = None; Lobbies = []; Players = []; PlayerListIsDirty = false; Modes = []}


type PingType =
    | Everyone
    | Here
    | NoOne

type qBotParameters =
    {
    AdminRoles : uint64 list
    CaptainRoles : uint64 list
    LobbiesCategory : uint64 option
    }
    static member create admins captains cat = { AdminRoles = admins ; CaptainRoles = captains ; LobbiesCategory = cat }

[<Struct>]
type qHereArgs =
    {
    AnnounceID : uint64 option
    Ping : PingType option
    }
    static member create  announcements ping = { qHereArgs.Ping = ping ; AnnounceID = announcements }

[<Struct>]
type qHereArgsValidated =
    {
    AnnounceID : uint64
    Ping : PingType

    }
    static member create announcements ping = { qHereArgsValidated.Ping = ping ; AnnounceID = announcements }
