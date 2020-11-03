﻿namespace qBotJr.T

open System
open Discord.WebSocket
open Discord.Rest

type PingType =
    | Everyone
    | Here
    | NoOne

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
    Players : Player list
    }
    //static member create
type Mode =
    {
    Name : string
    ModeMsg : RestUserMessage
    }
type Server =
    {
    Guild : SocketGuild
    TTL : DateTimeOffset
    isDirty: bool
    mutable HereMsg : RestUserMessage option
    mutable Lobbies : Lobby list
    mutable Players : Player list
    mutable Modes : Mode list
    }
    
        
type qBotParameters =
    {
    AdminRoles : uint64 list
    CaptainRoles : uint64 list
    LobbiesCategory : uint64 option
    }
    static member create admins captains cat =
        {AdminRoles = admins; CaptainRoles = captains; LobbiesCategory = cat}
    
[<Struct>]    
type qHereParameters =
    {
    Ping : PingType option
    Announcements : uint64 option
    }
    static member create ping announcements =
        {qHereParameters.Ping = ping; Announcements = announcements}

