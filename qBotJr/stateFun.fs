namespace qBotJr
open System
open Discord
open qBotJr.T

module State =

    let inline private addToMM task =
        Task task |> AsyncClient.Receive
    let AddMessageFilter (mf : MessageFilter) =
        ScheduledTask(fun state -> state.cmdTempFilters <- mf :: state.cmdTempFilters)
        |> addToMM

    let AddReactionFilter (rf : ReactionFilter) =
        ScheduledTask(fun state -> state.reaTempFilters <- rf :: state.reaTempFilters)
        |> addToMM

    let CleanUp () =

        ScheduledTask(fun state ->
            let now = DateTimeOffset.Now
            state.Servers <- state.Servers |> Map.filter (fun _ v -> v.TTL > now)
            state.cmdTempFilters <-
                    state.cmdTempFilters
                    |> List.filter (fun filter -> filter.TTL > now)
            state.reaTempFilters <-
                state.reaTempFilters
                |> List.filter (fun filter -> filter.TTL > now)
        )
        |> addToMM

    let SetPlayerState (guild : uint64) (user : IGuildUser) (stateFunc : Player -> unit) =
        ScheduledTask(fun state ->
            let server = state.Servers.Item guild
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
