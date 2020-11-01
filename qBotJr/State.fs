namespace qBotJr
open System.Collections.Generic
open System
open qBotJr.T

type State() =

   
    static let guilds = Dictionary<uint64, Server>()
    static let mutable messageFilters : MessageFilter list = []
    static let mutable reactionFilters : ReactionFilter list = []
    
    static member MessageFilters with get() = messageFilters
    static member ReactionFilters with get() = reactionFilters
    static member Guilds with get() = guilds
    
    static member CleanUpFilters : ScheduledTask = (fun () -> 
        let now = DateTimeOffset.Now
        messageFilters <-
                messageFilters
                |> List.filter (fun filter -> filter.TTL > now)
        reactionFilters <-
            reactionFilters
            |> List.filter (fun filter -> filter.TTL > now)
        )
      
    