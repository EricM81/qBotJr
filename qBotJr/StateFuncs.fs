namespace qBotJr
open System
open Discord
open qBotJr.T
open qBotJr.helper

module StateFuncs =

    let inline private addToMM task =
        Task task |> AsyncClient.Receive
    let AddMessageFilterTask (mf : MessageFilter) =
        ScheduledTask(fun state -> state.DynamicFilters <- mf :: state.DynamicFilters)
        |> addToMM

    let AddReactionFilterTask (rf : ReactionFilter) =
        ScheduledTask(fun state -> state.ReactionFilters <- rf :: state.ReactionFilters)
        |> addToMM

    let CleanUpTask () =

        ScheduledTask(fun state ->
            let now = DateTimeOffset.Now
            let guildsToRemove =
                state.Guilds
                |> Map.fold (fun acc k v -> if v.TTL < now then k::acc else acc) []
            state.Guilds <- state.Guilds |> Map.filter (fun _ v -> v.TTL > now)
            state.DynamicFilters <-
                    state.DynamicFilters
                    |> List.filter (
                        fun filter ->
                            filter.TTL > now ||
                            guildsToRemove |> List.exists (fun uid -> uid = filter.GuildID))
            state.ReactionFilters <-
                state.ReactionFilters
                |> List.filter (
                        fun filter ->
                            filter.TTL > now ||
                            guildsToRemove |> List.exists (fun uid -> uid = filter.GuildID))
        )
        |> addToMM

    let SetPlayerState (guild : uint64) (iUser : IGuildUser) (stateFunc : Player -> Player) =
        ScheduledTask(fun state ->
            let server = state.Guilds.Item guild
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

            server.PlayerListIsDirty <- true)
        |> addToMM
