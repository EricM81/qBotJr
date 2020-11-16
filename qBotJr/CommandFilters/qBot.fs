namespace qBotJr
open System.Threading.Channels
open Discord.WebSocket
open System
open discord
open qBotJr.T

module qBot =
    //admin roles
    //capt roles
    //lobbies cat

    [<Struct>] //val type
    type qBotArgs =
        {
            AdminRoles : Role list
            CaptainRoles : Role list
            LobbyCategory : uint64 option
        }
        static member create admins captains cat =
            { qBotArgs.AdminRoles = admins; CaptainRoles = captains; LobbyCategory = cat }

    [<Struct>] //val type
    type qBotValid =
        {
            AdminRoles : Role list
            CaptainRoles : Role list
            LobbyCategory : uint64
        }
        static member create admins captains cat =
            { qBotValid.AdminRoles = admins; CaptainRoles = captains; LobbyCategory = cat }

    let printMan (args : qHereArgs) : string =

        let sb = StringBuilder()
        let a format = bprintfn sb format

        a ">>> **Post a message to a channel (-a) and ping @ everyone (-e), @ here (-h), or no one (-n).**"
        a ""
    let qBotMain = @"Server Settings

You can change all the settings at once or just one at a time.

```-a Admin roles that can execute qBot, qHere, qNew, qGameMode, qSetUser
   Current Value:   -a ""Kami Commander"" ""Kami Mods!""

-c Captain roles that can execute qAdd, qAFK, qLeave, qBan, qClose
   Current Value:   -c ""Fake Moderator""


-l Lobbies are created in this category
   Current Value:   -l QSTUFF```

You can also type a parameter prefix for assistance.  For example calling ""-a"" will print a list of server roles to copy and paste."


//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit
    let str = "QBOT"
    let Run (pm : ParsedMsg) (goo : GuildOO) : unit =
        let settings = config.GetGuildSettings goo.Channel.Guild.Id
        let adminRoles = getRolesByIDs goo.Channel.Guild settings.AdminRoles
        let captainRoles = getRolesByIDs goo.Channel.Guild settings.CaptainRoles



        ()

    let noPerms  (pm : ParsedMsg) (goo : GuildOO) : unit =
        ()

