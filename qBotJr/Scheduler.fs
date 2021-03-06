﻿namespace qBotJr

open FSharpx.Control
open qBotJr.T
open discord


module Scheduler =

  module here =

    let qHereSleep: int = 120 * 1000

    let rec tryPickPlayersHere (p: Player) (searchH: PlayerHere list): PlayerHere option =
      match searchH with
      | [] -> None
      | h :: hs -> if p.ID = h.Player.ID && h.isHere = true then Some h else tryPickPlayersHere p hs

    let rec filterPlayersHere (hereP: PlayerHere list) (modeP: Player list) (acc: PlayerHere list) =
      match modeP with
      | [] -> acc
      | m :: ms ->
          match tryPickPlayersHere m hereP with
          | Some ph -> ph :: acc |> filterPlayersHere hereP ms
          | _ -> filterPlayersHere hereP ms acc


    let rec iterMode (pHere: PlayerHere list) (ms: Mode list) =
      match ms with
      | [] -> ()
      | m :: ms ->
          if m.PlayerListIsDirty = true then
            filterPlayersHere pHere m.Players []
            |> helper.printPlayersList
            |> (+) m.HereMsg.Header
            |> updateMsg m.HereMsg.RestMsg
          iterMode pHere ms

    let iterServer (_: uint64) (server: Server) =
      match server.HereMsg with
      | Some h when server.PlayerListIsDirty = true ->
          helper.printPlayersList server.PlayersHere |> (+) h.Header |> updateMsg h.RestMsg
          iterMode server.PlayersHere server.Modes
      | _ -> ()

    let qHereMsgScheduler () =
      let rec msgLoop () =
        async {
          do! Async.Sleep qHereSleep
          AsyncTask (fun state -> state.Servers |> Map.iter iterServer) |> UpdateState |> client.Receive
          return! msgLoop ()
        }

      msgLoop ()

  let init () = here.qHereMsgScheduler ()











//            servers
//            |> Seq.fold (fun x y -> y)
//
//collectMsgs





//get reactions
//wait for async result
//register task
