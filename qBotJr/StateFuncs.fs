namespace qBotJr
open System
open Discord
open Discord.WebSocket
open qBotJr.T
open qBotJr.helper

module State =

    let inline private addToMM task =
        Task task |> AsyncClient.Receive
    let AddMessageFilter (mf : MessageFilter) =
        ScheduledTask(fun state -> state.DynamicFilters <- mf :: state.DynamicFilters)
        |> addToMM

    let AddReactionFilter (rf : ReactionFilter) =
        ScheduledTask(fun state -> state.ReactionFilters <- rf :: state.ReactionFilters)
        |> addToMM

    let CleanUp () =

        ScheduledTask(fun state ->
            let now = DateTimeOffset.Now
            state.Guilds <- state.Guilds |> Map.filter (fun _ v -> v.TTL > now)
            state.DynamicFilters <-
                    state.DynamicFilters
                    |> List.filter (fun filter -> filter.TTL > now)
            state.ReactionFilters <-
                state.ReactionFilters
                |> List.filter (fun filter -> filter.TTL > now)
        )
        |> addToMM

    let SetPlayerState (guild : uint64) (user : IGuildUser) (stateFunc : Player -> unit) =
        ScheduledTask(fun state ->
            let server = state.Guilds.Item guild
            server.Players
            |> List.tryFind (fun p -> p.UID = user.Id)
            |> function
                | Some p -> p
                | None ->
                    let p = Player.create user.Id user.Nickname
                    server.Players <- p::server.Players
                    p
            |> stateFunc
            server.PlayerListIsDirty <- true)
        |> addToMM
