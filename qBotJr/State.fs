namespace qBotJr
open System
open Discord
open qBotJr.T
open qBotJr.helper

type State() =
    
   
    static let mutable guilds : Map<uint64, Server> = Map.empty
    static let mutable messageFilters : MessageFilter list = []
    static let mutable reactionFilters : ReactionFilter list = []
    
    static member MessageFilters with get() = messageFilters
    static member ReactionFilters with get() = reactionFilters
    static member Guilds with get() = guilds
    
    static member AddMessageFilterTask (mf : MessageFilter) : ScheduledTask =
        (fun () -> messageFilters <- mf::messageFilters)
        
    static member AddReactionFilterTask (rf : ReactionFilter) : ScheduledTask =
        (fun () -> reactionFilters <- rf::reactionFilters)
    
           
        
    static member CleanUpTask : ScheduledTask = (fun () -> 
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
      
    static member SetPlayerState (guild : uint64) (iUser : IGuildUser) (stateFunc : Player -> Player) =
        let server = guilds.Item guild
        let exists =
            server.Players
            |> List.exists
                (fun p ->
                    if p.UID = iUser.Id then
                        stateFunc p |> ignore
                        true
                    else
                        false)
        if not exists then
            server.Players <-
                Player.create iUser.Id iUser.Nickname
                |> stateFunc
                |> prepend server.Players
        
        server.PlayerListIsDirty <- true
        
            
    