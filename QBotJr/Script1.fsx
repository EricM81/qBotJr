#load @".paket\load\netcoreapp3.1\main.group.fsx"
#r @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Collections.Specialized.dll"

open Newtonsoft.Json
open System
open System.IO
open System.Text
open Newtonsoft.Json



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
   


//let loadGuild (guildID : uint64) : GuildSettings = 
//    let fileName = (sprintf "%s%u.json" @"C:\Users\EricM\Desktop\qBotJr\Guilds\" guildID)
//    if File.Exists(fileName) then
//        use f = File.Open(fileName, FileMode.Open, FileAccess.Read)
//        use sr = new StreamReader(f)
//        try 
//            JsonConvert.DeserializeObject<GuildSettings>(sr.ReadToEnd())
//        with
//        | _ -> GuildSettings.defaultGuild guildID
//    else
//        GuildSettings.defaultGuild guildID

let saveGuild (guildSettings : GuildSettings) = 
    let fileName = (sprintf "%s%u.json"  @"C:\Users\EricM\Desktop\qBotJr\Guilds\" guildSettings.GuildID)
    use f = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write)
    use sw = new StreamWriter(f)
    guildSettings |> JsonConvert.SerializeObject |> sw.Write
    sw.Flush()
    sw.Close()

let guild = {GuildID = 132359939363569664UL; AdminRoles = [178555702430793728UL; 222429919655886849UL]; CaptainRoles = [751273804155715604UL]; PlayersPerGame = 9; AnnounceChannel = Some {ID = 544636678954811392UL; Name = "#sub_chat_announcements"}}
saveGuild guild


        

