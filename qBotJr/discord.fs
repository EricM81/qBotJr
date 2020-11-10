﻿namespace qBotJr
open System
open System.Text
open Discord
open Discord.WebSocket
open FSharpx.Control
open qBotJr
open qBotJr.T

module discord =

    let private stripUID (prefix : string) (suffix : char) (value : string) : uint64 option =
        let len = value.Length
        let preLen = prefix.Length
        if (value.StartsWith(prefix)) && (value.EndsWith(suffix)) then
            let tmp = (value.Substring((prefix.Length), (len - preLen - 1)))
            match (UInt64.TryParse tmp) with
            | true, uid -> Some uid
            | _ -> None
        else
            None

    let parseDiscoUser (name : string) : uint64 option =
        let prefix = "<@!"
        let suffix = '>'
        stripUID prefix suffix name

    let parseDiscoChannel (name : string) : uint64 option =
        let prefix = "<#"
        let suffix = '>'
        stripUID prefix suffix name

    let getRolesByIDs (guild : SocketGuild) (ids : uint64 list) : SocketRole list =
        let rec getRolesByIDsInner (roles : uint64 list) (acc : SocketRole list) =
            match roles with
            | [] -> acc
            | head::tail ->
                let y = guild.Roles |> Seq.find (fun x -> x.Id = head)
                getRolesByIDsInner tail (y::acc)
        getRolesByIDsInner ids []

    let getCategoryByID (guild : SocketGuild) (id : uint64 option) : SocketCategoryChannel option  =
        match id with
        | Some x ->
            let cat = guild.GetCategoryChannel x
            match cat with
            | null -> None
            | y -> Some y
        | None -> None

    let getCategoryByName (guild : SocketGuild) (name : string) : SocketCategoryChannel option =
        guild.CategoryChannels |> Seq.tryFind (fun y -> y.Name = name)


    let getChannelByID (id : uint64) : SocketGuildChannel option =
        conn.client.GetChannel id
        |> function
            | :? SocketGuildChannel as z -> Some z
            | _ -> None

    let getChannelByName (guild : SocketGuild) (name : string) : SocketGuildChannel option =
        guild.Channels |> Seq.tryFind (fun y -> y.Name = name)

    let sendMsg (channel : SocketChannel) (msg : string) =
        match channel with
        | :? SocketTextChannel as x ->
            x.SendMessageAsync msg |> Async.AwaitTask |> Some
        | _ -> None

    let reactDistrust (parsedM : ParsedMsg) (_ : GuildOO) : unit =
        emojis.Distrust
        |> Emoji
        |> parsedM.Message.AddReactionAsync
        |> Async.AwaitTask
        |> ignore

    let pingToString (p : PingType) =
        match p with
        | PingType.Everyone -> "@everyone"
        | PingType.Here -> "@here"
        | PingType.NoOne -> ""

    let bprintfn (sb : StringBuilder) =
        Printf.kprintf (fun s -> sb.AppendLine s |> ignore)
