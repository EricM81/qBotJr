namespace qBotJr

open System
open Discord
open qBotJr.T

module stateFun =

  let inline private addToMM task = UpdateState task |> client.Receive

  let AddMessageFilter (mf: MessageFilter) =
    AsyncTask (fun state -> state.cmdTempFilters <- mf :: state.cmdTempFilters) |> addToMM

  let AddReactionFilter (rf: ReactionFilter) =
    AsyncTask (fun state -> state.rtTempFilters <- rf :: state.rtTempFilters) |> addToMM

  let private findAndExpireTmp (state: State) (now: DateTimeOffset) (serverID: uint64) (server: Server) =
    if server.TTL > now then
      true
    else
      state.rtServerFilters
      |> List.iter (fun filter ->
           if filter.GuildID = serverID then
             filter.TTL <- DateTimeOffset.MinValue)
      false

  let CleanUp () =
    AsyncTask (fun state ->
      let now = DateTimeOffset.Now
      state.Servers <- state.Servers |> Map.filter (findAndExpireTmp state now)
      state.cmdTempFilters <- state.cmdTempFilters |> List.filter (fun filter -> filter.TTL > now)
      state.rtServerFilters <- state.rtServerFilters |> List.filter (fun filter -> filter.TTL > now)
      state.rtTempFilters <- state.rtTempFilters |> List.filter (fun filter -> filter.TTL > now))
    |> addToMM



  let SetPlayerState (guild: uint64) (user: IGuildUser) (stateFunc: PlayerHere -> unit) =
    AsyncTask (fun state ->
      let server = state.Servers.Item guild
      let ph = server.PlayersHere |> List.tryFind (fun ph -> ph.Player.ID = user.Id)
      match ph with
      | Some ph' -> ph'
      | None ->
          let ph' = helper.getPerm user |> PlayerHere.create user false
          state.Servers <- Map.add server.Guild.Id {server with PlayersHere = ph' :: server.PlayersHere} state.Servers
          ph'
      |> stateFunc
      server.PlayerListIsDirty <- true)
    |> addToMM


//
//    let AddHereMsg (server : Server) (rest : RestUserMessage) (emoji : string) (announceHeader : string) =
//        AsyncTask(fun state ->
//            //seed the reaction to say "I'm here"
//            Emoji(emoji) |> rest.AddReactionAsync |> ignore
//            //if replacing a hereMsg, remove old reaction filter and reset everyone's isHere
//            match server.HereMsg with
//            | Some msg ->
//                state.rtServerFilters <- removeOldHereMsgFilter msg.MessageID state.rtServerFilters
//                server.PlayersHere |> List.iter (fun player -> player.isHere <- false)
//            | None -> ()
//            //create new hereMsg
//            state.Servers <-
//                state.Servers
//                |> Map.add server.GuildID {server with HereMsg = Some <| HereMessage.create rest.Id emoji rest announceHeader}
//            //register filter for reactions
//            [ ReAction.create emoji updateHereList ]
//            |> ReactionFilter.create server.GuildID rest.Id DateTimeOffset.MaxValue None
//            |> client.AddReactionFilter)
//        |> addToMM

//    let AddModeMsg (server : Server) (rest : RestUserMessage) (emoji : string) (anounceHeader : string) =
//        AsyncTask (fun state ->
//            Emoji(emoji) |> rest.AddReactionAsync |> ignore
//            let server = state.Servers.Item guildID
//             state.Servers <-
//                state.Servers
//                |> Map.add guildID {server with HereMsg = Some <| HereMessage.create rest.Id emoji rest announceHeader}
//            )
//        |> addToMM
