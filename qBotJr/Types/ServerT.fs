namespace qBotJr.T

open System
open Discord
open Discord.WebSocket
open Discord.Rest

[<Struct>] //val type
type UserPermission =
  | None = 0
  | Captain = 1
  | Admin = 2
  | Creator = 3

[<Struct>]
type Player =
  {
    ID: uint64
    Name: string
  }

  static member create(user: IGuildUser) =
    let name = if (String.IsNullOrEmpty user.Nickname) then user.Username else user.Nickname
    {ID = user.Id; Name = name}

//ref type
type PlayerHere =
  {
    Player: Player
    Role: UserPermission
    mutable GamesPlayed: int16
    mutable isHere: bool
    mutable isBanned: bool
  }

  static member create (user: IGuildUser) here role =

    {PlayerHere.Player = Player.create user; GamesPlayed = 0s; isHere = here; isBanned = false; Role = role}

//ref type
type Lobby =
  {Name: string; Channel: ITextChannel; mutable Players: PlayerHere list}
  static member create (c: ITextChannel) px =
    {Lobby.Name = c.Name; Channel = c; Players = px}



[<Struct>] //val type
type HereMessage =
  {
    MessageID: uint64
    Emoji: string
    RestMsg: IUserMessage
    Header: string
  }

  static member create (restMsg: IUserMessage) emoji annHeader =
    {MessageID = restMsg.Id; Emoji = emoji; RestMsg = restMsg; Header = annHeader}

//ref type
type Mode = {Name: string; HereMsg: HereMessage; Players: Player list; PlayerListIsDirty: bool}

//ref type
type Server =
  {
    GuildID: uint64
    Guild: SocketGuild
    mutable TTL: DateTimeOffset
    HereMsg: HereMessage option
    PlayersHere: PlayerHere list
    mutable PlayerListIsDirty: bool
    Lobbies: Lobby list
    Modes: Mode list
  }

  static member create(guild: SocketGuild) =
    {
      Server.GuildID = guild.Id
      Guild = guild
      TTL = DateTimeOffset.Now.AddHours (1.0)
      HereMsg = None
      Lobbies = []
      PlayersHere = []
      PlayerListIsDirty = false
      Modes = []
    }

//val type
type PingType =
  | Everyone = 0
  | Here = 1
  | NoOne = 2

[<Struct>] //val type
type qBotParameters =
  {
    AdminRoles: uint64 list
    CaptainRoles: uint64 list
    LobbiesCategory: uint64 option
  }

  static member create admins captains cat = {AdminRoles = admins; CaptainRoles = captains; LobbiesCategory = cat}
