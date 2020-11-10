namespace qBotJr.T
open System
open Discord
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
    static member create  uid name=
        {Player.UID = uid; Name = name; GamesPlayed = 1uy; isHere = true; isBanned = false}

type Lobby =
    {
    Name : string
    Channel : SocketGuildChannel
    mutable PlayerIDs : uint64 list
    }
    //static member create


type HereMessage =
    {
    MessageID : uint64
    Emoji : string
    RestMsg : RestMessage
    }

type Mode =
    {
    Name : string
    HereMsg : HereMessage
    mutable PlayerIDs : uint64 list
    mutable PlayerListIsDirty : bool
    //todo how does the here msg get updated???
    }

type Server =
    {
    Guild : SocketGuild
    mutable TTL : DateTimeOffset
    mutable HereMsg : HereMessage option
    mutable Lobbies : Lobby list
    mutable Players : Player list
    mutable PlayerListIsDirty : bool
    mutable Modes : Mode list
    }

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


