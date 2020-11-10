namespace qBotJr.T
open System
open Discord.WebSocket
open Discord.Rest

type Player =
    {
    UID : uint64
    Name : string
    mutable GamesPlayed : uint16
    mutable isHere : bool
    mutable isBanned : bool
    }
    static member create uid name =
        {Player.UID = uid; Name = name; GamesPlayed = 0us; isHere = true; isBanned = false}

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

[<Struct>]
type Server =
    {
    Guild : SocketGuild
    TTL : DateTimeOffset
    HereMsg : HereMessage<Server> option
    Lobbies : Lobby list
    Players : Player list
    PlayerListIsDirty : bool
    Modes : Mode<Server> list
    }
    static member create guild =
        {Guild = guild; TTL = DateTimeOffset.Now.AddHours(1.0); HereMsg = None; Lobbies = []; Players = []; PlayerListIsDirty = false; Modes = []}


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

