namespace qBotJr

open System
open System.Text
open System.Threading.Tasks
open Discord
open Discord.WebSocket
open FSharp.Control
open qBotJr
open qBotJr.T
open FSharp.Control.Tasks.V2
open helper

module discord =

  [<Literal>]
  let botID = 760644805969313823uL

  module private config =
    let clientConfig =
      let intents = GatewayIntents.GuildMessages ||| GatewayIntents.GuildMessageReactions ||| GatewayIntents.Guilds
      let tmp = DiscordSocketConfig ()
      tmp.MessageCacheSize <- 100
      //tmp.AlwaysDownloadUsers <- true
      tmp.GatewayIntents <- Nullable<GatewayIntents> (intents)
      tmp

    let restClientOptions =
      let opt = RequestOptions.Default
      opt.Timeout <- Nullable<int> 5000
      opt.RetryMode <- Nullable<RetryMode> RetryMode.AlwaysRetry
      opt

  let discoClient = new DiscordSocketClient(config.clientConfig)

  module private receive =

    let inline private notBotOrNullMsg (msg: SocketMessage) =
      if msg.Author.IsBot = false && (String.IsNullOrEmpty msg.Content) = false then true else false

    let inline private isMsgDownCastAble (msg: SocketMessage) =
      if (msg.Author :? SocketGuildUser) && (msg.Channel :? SocketTextChannel) then
        true
      else
        sprintf "NewMessage failed to cast:" |> logger.WriteLine
        sprintf "%O" msg.Author |> logger.WriteLine
        sprintf "%O" msg.Channel |> logger.WriteLine
        logger.WriteLine ""
        false

    let inline private downCastMsg (msg: SocketMessage): NewMessage =
      msg.Channel :?> SocketTextChannel |> GuildOO.create (msg.Author :?> SocketGuildUser) |> NewMessage.create msg

    let receiveMsgAsync (msg: SocketMessage): Task =
      task {
        if notBotOrNullMsg msg && isMsgDownCastAble msg then
          let nm = downCastMsg msg
          if client.TestMsgFilters nm then
            NewMessage nm |> client.Receive
      } :> Task //upcast Task<unit> to base Task

    let inline private notBotOrNullRt (rt: SocketReaction) =
      if rt.UserId <> botID && (String.IsNullOrEmpty rt.Emote.Name) = false then true else false

    let inline private isRtChannelDownCastAble (rt: SocketReaction) = if rt.Channel :? SocketTextChannel then true else false

    let inline isUserDownloaded (rt: SocketReaction): bool =
      if rt.User.IsSpecified && (rt.User.Value :? IGuildUser) then true else false

    let getRtUser (rt: SocketReaction) (chl: SocketTextChannel) =
      task {
        let! user = discoClient.Rest.GetGuildUserAsync (chl.Guild.Id, rt.UserId, config.restClientOptions)
        return (user :> IGuildUser)
      }

    let inline private receiveRtAsync msg rt isAdd =
      task {
        if notBotOrNullRt rt && isRtChannelDownCastAble rt then
          let chl = rt.Channel :?> SocketTextChannel
          let! user = if isUserDownloaded rt then Task.FromResult (rt.User.Value :?> IGuildUser) else getRtUser rt chl
          let mr = GuildOO.create user chl |> MessageReaction.create msg rt isAdd

          if client.TestRtFilters mr then
            MessageReaction mr |> client.Receive
      } :> Task //upcast Task<unit> to base Task

    let addRtAsync (msg: Cacheable<IUserMessage, uint64>) (_: ISocketMessageChannel) (sRt: SocketReaction) =
      receiveRtAsync msg sRt true

    let removeRtAsync (msg: Cacheable<IUserMessage, uint64>) (_: ISocketMessageChannel) (sRt: SocketReaction) =
      receiveRtAsync msg sRt false

  module private _helper =

    let stripUID (prefix: string) (suffix: char) (value: string): uint64 option =
      let len = value.Length
      let preLen = prefix.Length
      if (value.StartsWith (prefix)) && (value.EndsWith (suffix)) then
        let tmp = (value.Substring ((prefix.Length), (len - preLen - 1)))
        match (UInt64.TryParse tmp) with
        | true, uid -> Some uid
        | _ -> None
      else
        None

  let registerEvents () =
    discoClient.add_Log (fun log ->
      logger.WriteLine (sprintf "%s\n%s\n" log.Source log.Message)
      Task.CompletedTask)

    discoClient.add_Ready (fun _ ->
      logger.WriteLine "Ready to receive...\n"
      Task.CompletedTask)
    //todo add error handler for connection issue
    Func<_, _> receive.receiveMsgAsync |> discoClient.add_MessageReceived
    Func<_, _, _, _> receive.addRtAsync |> discoClient.add_ReactionAdded
    Func<_, _, _, _> receive.removeRtAsync |> discoClient.add_ReactionRemoved

  //TODO listen for can't connect and disconnects and try agane after one minute

  let startClient () =
    task {
      do! discoClient.LoginAsync (TokenType.Bot, config.BotSettings.DiscordToken)
      do! discoClient.StartAsync ()
      return ()
    }

  let parseDiscoUser (name: string): uint64 option =
    let prefix = "<@!"
    let suffix = '>'
    _helper.stripUID prefix suffix name

  let parseDiscoRole (guild: SocketGuild) (role: string): Result<SocketRole, string> =
    let inline tryFind (id): SocketRole option = guild.Roles |> Seq.tryFind (fun role -> role.Id = id)
    let prefix = "<@&"
    let suffix = '>'
    _helper.stripUID prefix suffix role
    |> bind tryFind
    |> function
    | Some s -> Ok s
    | None -> Error role

  let socketRoleToStrId (sr: SocketRole): string = sprintf "<@&%i>" sr.Id

  let socketRoleToStrName (sr: SocketRole): string = sr.Name |> quoteEscape

  let validateRoles (guild: SocketGuild) (roles: string list): Result<SocketRole list, string list * string list> =
    let rec validate (roles: string list) (acc: Result<SocketRole list, string list * string list>) =
      match roles with
      | [] -> acc
      | strRole :: strList ->
          match acc, (parseDiscoRole guild strRole) with
          | Ok (srList), Ok (sr) -> Ok (sr :: srList) |> validate strList
          | Ok (srList), Error (err) ->
              let goodRoles = srList |> List.map (fun sr -> sr.Name)
              Error (goodRoles, [err]) |> validate strList
          | Error (strList, errList), Ok _ -> Error ((strRole :: strList), errList) |> validate strList
          | Error (strList, errList), Error (err) -> Error (strList, (err :: errList)) |> validate strList

    Ok [] |> validate roles

  let parseDiscoChannel (name: string): uint64 option =
    let prefix = "<#"
    let suffix = '>'
    _helper.stripUID prefix suffix name

  let getRolesByIDs (guild: SocketGuild) (ids: uint64 list): SocketRole list =
    let rec getRolesByIDsInner (roles: uint64 list) (acc: SocketRole list) =
      match roles with
      | [] -> acc
      | head :: tail ->
          let y = guild.Roles |> Seq.find (fun x -> x.Id = head)
          getRolesByIDsInner tail (y :: acc)

    getRolesByIDsInner ids []

  let getCategoryByID (guild: SocketGuild) (id: uint64 option): SocketCategoryChannel option =
    match id with
    | Some x ->
        let cat = guild.GetCategoryChannel x
        match cat with
        | null -> None
        | y -> Some y
    | None -> None

  let getCategoryByName (guild: SocketGuild) (name: string): SocketCategoryChannel option =
    guild.CategoryChannels |> Seq.tryFind (fun y -> y.Name = name)



  let printCategoryNames (guild: SocketGuild) (sb: StringBuilder) =
    let wrapAtLen = 50

    let rec print (cats: SocketCategoryChannel list) (lineLen: int) =
      match cats with
      | [] -> ()
      | c :: cx when lineLen = 0 ->
          quoteEscape c.Name |> (+) "   " |> sb.Append |> ignore
          c.Name.Length + 4 |> print cx
      | c :: cx when c.Name.Length + lineLen <= wrapAtLen ->
          quoteEscape c.Name |> (+) ", " |> sb.Append |> ignore
          c.Name.Length + 2 |> print cx
      | c :: cx ->
          quoteEscape c.Name |> (+) "\n   " |> sb.Append |> ignore
          c.Name.Length + 4 |> print cx

    print (guild.CategoryChannels |> Seq.toList) 0


  let getCategoryById (id: uint64): SocketCategoryChannel option =
    let channel = discoClient.GetChannel id
    match channel with
    | :? SocketCategoryChannel as z -> Some z
    | _ -> None


  let getChannelByID (id: uint64): SocketGuildChannel option =
    let channel = discoClient.GetChannel id
    match channel with
    | :? SocketGuildChannel as z -> Some z
    | _ -> None

  let tryCastTextChannel (gChl: SocketGuildChannel): SocketTextChannel option =
    match gChl with
    | :? SocketTextChannel as z -> Some z
    | _ -> None

  let getChannelByStrID (strID: string): SocketTextChannel option =
    parseDiscoChannel strID |> bind getChannelByID |> bind tryCastTextChannel

  let getChannelByName (guild: SocketGuild) (name: string): SocketGuildChannel option =
    guild.Channels |> Seq.tryFind (fun y -> y.Name = name)

  let sendMsg (channel: SocketChannel) (msg: string) =
    match channel with
    | :? SocketTextChannel as x -> x.SendMessageAsync msg |> Some
    | _ -> None

  let reactDistrust (_: Server) (_: GuildOO) (parsedM: ParsedMsg): Server option =
    emojis.Distrust |> Emoji |> parsedM.Message.AddReactionAsync |> Async.AwaitTask |> ignore
    None

  let pingToString (p: PingType) =
    match p with
    | PingType.Everyone -> "@everyone"
    | PingType.Here -> "@here"
    | _ -> ""
