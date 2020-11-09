namespace qBotJr.T


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


