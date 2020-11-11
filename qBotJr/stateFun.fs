namespace qBotJr

open System
open Discord
open qBotJr.T

module stateFun =

    let inline private addToMM task = Task task |> client.Receive

    let AddMessageFilter (mf : MessageFilter) =
        AsyncTask(fun state -> state.cmdTempFilters <- mf :: state.cmdTempFilters) |> addToMM

    let AddReactionFilter (rf : ReactionFilter) =
        AsyncTask(fun state -> state.rtTempFilters <- rf :: state.rtTempFilters) |> addToMM

    let CleanUp () =
        AsyncTask(fun state ->
            let now = DateTimeOffset.Now
            state.Servers <- state.Servers |> Map.filter (fun _ v -> v.TTL > now)
            //todo remove state.rtServerFilters
            state.cmdTempFilters <- state.cmdTempFilters |> List.filter (fun filter -> filter.TTL > now)
            state.rtTempFilters <- state.rtTempFilters |> List.filter (fun filter -> filter.TTL > now))
        |> addToMM

    let SetPlayerState (guild : uint64) (user : IGuildUser) (stateFunc : Player -> unit) =
        AsyncTask(fun state ->
            let server = state.Servers.Item guild
            let p = server.Players |> List.tryFind (fun p -> p.UID = user.Id)
            match p with
            | Some p -> p
            | None ->
                let p = Player.create user.Id user.Nickname
                state.Servers <- Map.add server.Guild.Id { server with Players = p :: server.Players } state.Servers
                p
            |> stateFunc
            server.PlayerListIsDirty <- true)
        |> addToMM
