namespace qBotJr
open System.Collections.Generic
open System
open Discord.WebSocket
open qBotJr.T

type State() =
    static let prepend xs x = x::xs

   
    static let mutable guilds : Map<uint64, Server> = Map.empty
    static let mutable messageFilters : MessageFilter list = []
    static let mutable reactionFilters : ReactionFilter list = []
    
    static member MessageFilters with get() = messageFilters
    static member ReactionFilters with get() = reactionFilters
    static member Guilds with get() = guilds
    
    
        
    static member CleanUpFiltersTask : ScheduledTask = (fun () -> 
        let now = DateTimeOffset.Now
        let guildsToRemove =
            guilds
            |> Map.fold (fun acc k v -> if v.TTL < now then k::acc else acc) []

        guilds <- guilds |> Map.filter (fun _ v -> v.TTL > now)
        
        messageFilters <-
                messageFilters
                |> List.filter (
                    fun filter ->
                        filter.TTL > now ||
                        guildsToRemove |> List.exists (fun uid -> uid = filter.GuildID))
        reactionFilters <-
            reactionFilters
            |> List.filter (
                    fun filter ->
                        filter.TTL > now ||
                        guildsToRemove |> List.exists (fun uid -> uid = filter.GuildID))
        )
      
    static member SetPlayerState (guild : uint64) (gUser : SocketGuildUser) (state : Player -> Player) =
        let server = guilds.Item guild
        let exists =
            server.Players
            |> List.exists
                (fun p ->
                    if p.UID = gUser.Id then
                        state p |> ignore
                        true
                    else
                        false)
        if not exists then
            server.Players <-
                Player.create gUser.Id gUser.Nickname
                |> state
                |> prepend server.Players
            
    