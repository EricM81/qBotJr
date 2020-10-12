namespace qBotJr
open System.IO
open Newtonsoft.Json
open System.Collections.Generic


[<Struct>]
type DiscordEntity = {
    ID : uint64 ;
    Name : string
    }

[<Struct>]
type BotSettings = {
    DiscordToken : string;
    LogFileRoot : string;
    GuildSettingsRoot : string
    }
[<Struct>]
type GuildSettings = 
    {
    GuildID : uint64;
    AdminRoles : uint64 list;
    CaptainRoles : uint64 list;
    PlayersPerGame : int;
    AnnounceChannel : DiscordEntity option
    }
    static member defaultGuild id = 
        {GuildSettings.GuildID = id ; AdminRoles = []; CaptainRoles = []; PlayersPerGame = 9; AnnounceChannel = None}

type config() = 

    static let guilds = new Dictionary<uint64, GuildSettings>()

    static let loadGuild (guildID : uint64) : GuildSettings = 
        let fileName = (sprintf "%s%u.json" config.BotSettings.GuildSettingsRoot guildID)
        if File.Exists(fileName) then
            use f = File.Open(fileName, FileMode.Open, FileAccess.Read)
            use sr = new StreamReader(f)
            try 
                JsonConvert.DeserializeObject<GuildSettings>(sr.ReadToEnd())
            with
            | _ -> GuildSettings.defaultGuild guildID
        else
            GuildSettings.defaultGuild guildID

    static let saveGuild (guildSettings : GuildSettings) = 
        let fileName = (sprintf "%s%u.json" config.BotSettings.GuildSettingsRoot guildSettings.GuildID)
        use f = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write)
        use sw = new StreamWriter(f)
        guildSettings |> JsonConvert.SerializeObject |> sw.Write
        sw.Flush()
        sw.Close()

        

    static let loadBotSettings = 
        use f = File.Open(Directory.GetCurrentDirectory() + "\\settings.json", FileMode.Open, FileAccess.Read)
        use sr = new StreamReader(f)
               
        let tmp2 = JsonConvert.DeserializeObject<BotSettings>(sr.ReadToEnd())
        sr.Close()

        tmp2

    static member BotSettings = loadBotSettings

    static member GetGuildSettings (guildID : uint64) = 
        if guilds.ContainsKey(guildID) then
            guilds.Item(guildID)
        else
            let tmp = loadGuild(guildID)
            guilds.Add(tmp.GuildID, tmp)
            tmp
        
    static member SetGuildSettings (guildSettings : GuildSettings) = 
        saveGuild guildSettings
        guilds.Item(guildSettings.GuildID) <- guildSettings

        


       
    

    //static member 

    
    
    
    //save settings for later
    //let f = File.Open(Directory.GetCurrentDirectory() + "\settings.json", FileMode.OpenOrCreate, FileAccess.Write)
    //let sw = new StreamWriter(f)
    //let str = JsonConvert.SerializeObject(tmp)
    //sw.Write str
    //sw.Close ()
    //f.Close ()
    