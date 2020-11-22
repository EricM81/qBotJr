namespace qBotJr.T

[<Struct>]
type BotSettings =
  {
    DiscordToken: string
    LogFileRoot: string
    GuildSettingsRoot: string
    CleanUpTaskDelay: int
    UpdateHereMsgsDelay: int
  }

[<Struct>]
type GuildSettings =
  {
    GuildID: uint64
    AdminRoles: uint64 list
    CaptainRoles: uint64 list
    LobbiesCategory: uint64 option
    PlayersPerGame: int
    AnnounceChannel: uint64 option
  }

  static member defaultGuild id =
    {
      GuildSettings.GuildID = id
      AdminRoles = []
      CaptainRoles = []
      LobbiesCategory = None
      PlayersPerGame = 9
      AnnounceChannel = None
    }
