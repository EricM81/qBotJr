namespace qBotJr

open System
open System.Text
open System.Threading.Tasks
open Discord
open Discord.WebSocket
open System.Collections
open FSharp.Control
open qBotJr
open qBotJr.T
open FSharp.Control.Tasks.V2
open helper

module discord =

  [<Literal>]
  let botID = 760644805969313823uL

  [<Literal>]
  let userPrefix = "<@!"

  [<Literal>]
  let channelPrefix = "<#"

  [<Literal>]
  let rolePrefix = "<@&"

  [<Literal>]
  let suffix = ">"

  module config =
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

    let stripUID (prefix: string) (suffix: string) (value: string): uint64 option =
      let len = value.Length
      let preLen = prefix.Length
      let sufLen = suffix.Length
      if (value.StartsWith (prefix)) && (value.EndsWith (suffix)) then
        let tmp = (value.Substring ((prefix.Length), (len - preLen - sufLen)))
        match (UInt64.TryParse tmp) with
        | true, uid -> Some uid
        | _ -> None
      else
        None

  module perms =

    let everyonePerms =
      let createInstantInvite = PermValue.Deny
      let manageChannel = PermValue.Deny
      let addReactions = PermValue.Deny
      let viewChannel = PermValue.Allow
      let sendMessages = PermValue.Deny
      let sendTTSMessages = PermValue.Deny
      let manageMessages = PermValue.Deny
      let embedLinks = PermValue.Deny
      let attachFiles = PermValue.Deny
      let readMessageHistory = PermValue.Allow
      let mentionEveryone = PermValue.Deny
      let useExternalEmojis = PermValue.Deny
      let connect = PermValue.Deny
      let speak = PermValue.Deny
      let muteMembers = PermValue.Deny
      let deafenMembers = PermValue.Deny
      let moveMembers = PermValue.Deny
      let useVoiceActivation = PermValue.Deny
      let manageRoles = PermValue.Deny
      let manageWebhooks = PermValue.Deny
      let prioritySpeaker = PermValue.Deny
      let stream = PermValue.Deny

      OverwritePermissions(
        createInstantInvite,
        manageChannel,
        addReactions,
        viewChannel,
        sendMessages,
        sendTTSMessages,
        manageMessages,
        embedLinks,
        attachFiles,
        readMessageHistory,
        mentionEveryone,
        useExternalEmojis,
        connect,
        speak,
        muteMembers,
        deafenMembers,
        moveMembers,
        useVoiceActivation,
        manageRoles,
        manageWebhooks,
        prioritySpeaker,
        stream)

    let captainPerms =
      let createInstantInvite = PermValue.Deny
      let manageChannel = PermValue.Allow
      let addReactions = PermValue.Allow
      let viewChannel = PermValue.Allow
      let sendMessages = PermValue.Allow
      let sendTTSMessages = PermValue.Allow
      let manageMessages = PermValue.Allow
      let embedLinks = PermValue.Allow
      let attachFiles = PermValue.Allow
      let readMessageHistory = PermValue.Allow
      let mentionEveryone = PermValue.Allow
      let useExternalEmojis = PermValue.Allow
      let connect = PermValue.Allow
      let speak = PermValue.Allow
      let muteMembers = PermValue.Allow
      let deafenMembers = PermValue.Allow
      let moveMembers = PermValue.Allow
      let useVoiceActivation = PermValue.Allow
      let manageRoles = PermValue.Allow
      let manageWebhooks = PermValue.Allow
      let prioritySpeaker = PermValue.Allow
      let stream = PermValue.Allow

      OverwritePermissions(
        createInstantInvite,
        manageChannel,
        addReactions,
        viewChannel,
        sendMessages,
        sendTTSMessages,
        manageMessages,
        embedLinks,
        attachFiles,
        readMessageHistory,
        mentionEveryone,
        useExternalEmojis,
        connect,
        speak,
        muteMembers,
        deafenMembers,
        moveMembers,
        useVoiceActivation,
        manageRoles,
        manageWebhooks,
        prioritySpeaker,
        stream)

    let playerPerms =
      let createInstantInvite = PermValue.Deny
      let manageChannel = PermValue.Deny
      let addReactions = PermValue.Allow
      let viewChannel = PermValue.Allow
      let sendMessages = PermValue.Allow
      let sendTTSMessages = PermValue.Deny
      let manageMessages = PermValue.Deny
      let embedLinks = PermValue.Allow
      let attachFiles = PermValue.Allow
      let readMessageHistory = PermValue.Allow
      let mentionEveryone = PermValue.Deny
      let useExternalEmojis = PermValue.Allow
      let connect = PermValue.Allow
      let speak = PermValue.Allow
      let muteMembers = PermValue.Deny
      let deafenMembers = PermValue.Deny
      let moveMembers = PermValue.Deny
      let useVoiceActivation = PermValue.Allow
      let manageRoles = PermValue.Deny
      let manageWebhooks = PermValue.Deny
      let prioritySpeaker = PermValue.Deny
      let stream = PermValue.Allow

      OverwritePermissions(
        createInstantInvite,
        manageChannel,
        addReactions,
        viewChannel,
        sendMessages,
        sendTTSMessages,
        manageMessages,
        embedLinks,
        attachFiles,
        readMessageHistory,
        mentionEveryone,
        useExternalEmojis,
        connect,
        speak,
        muteMembers,
        deafenMembers,
        moveMembers,
        useVoiceActivation,
        manageRoles,
        manageWebhooks,
        prioritySpeaker,
        stream)

    let getPerms (guild: SocketGuild) (playerIDs: uint64 list): Generic.IEnumerable<Overwrite> =
      let rec loopIDs (perm) (target: PermissionTarget) (roles: uint64 list) acc =
        match roles with
        | [] -> acc
        | x::xs -> Overwrite(x, target, perm) |> prepend acc |> loopIDs perm target xs

      let settings = config.GetGuildSettings guild.Id
      let adminIDs = settings.AdminRoles
      let captainIDs = settings.CaptainRoles
      let everyone = [guild.EveryoneRole.Id]
      upcast(
        []
        |> loopIDs captainPerms PermissionTarget.Role adminIDs
        |> loopIDs captainPerms PermissionTarget.Role captainIDs
        |> loopIDs playerPerms PermissionTarget.User playerIDs
        |> loopIDs everyonePerms PermissionTarget.Role everyone
      )

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
    _helper.stripUID userPrefix suffix name

  let mentionUserID (id: uint64): string = userPrefix + id.ToString() + suffix

  let parseDiscoRole (guild: SocketGuild) (role: string): Result<SocketRole, string> =
    let inline tryFind (id): SocketRole option = guild.Roles |> Seq.tryFind (fun role -> role.Id = id)
    _helper.stripUID rolePrefix suffix role
    |> bind tryFind
    |> function
    | Some s -> Ok s
    | None -> Error role

  let mentionRoleID (sr: SocketRole): string = rolePrefix + sr.Id.ToString() + suffix

  let parseDiscoChannel (name: string): uint64 option =
    _helper.stripUID channelPrefix suffix name

  let mentionChannelID (chl: SocketChannel): string = channelPrefix + chl.Id.ToString() + suffix


  //let socketRoleToStrName (sr: SocketRole): string = sr.Name |> quoteEscape

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
    let NAME = name.ToUpper()
    guild.CategoryChannels |> Seq.tryFind (fun y -> y.Name.ToUpper() = NAME)

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

  let sendMsg (channel: ITextChannel) (msg: string) =
    channel.SendMessageAsync msg

  let updateMsg (msg: IUserMessage) (content: string): unit =
      let content' = content |> Optional<string>
      msg.ModifyAsync (fun msgP -> msgP.Content <- content') |> ignore

  let addReaction (msg: IUserMessage) (emoji: string) =
    msg.AddReactionAsync((Emoji emoji), config.restClientOptions)

  let reactDistrust (_: Server) (_: GuildOO) (parsedM: ParsedMsg): Server option =
    emojis.Distrust |> Emoji |> parsedM.Message.AddReactionAsync |> Async.AwaitTask |> ignore
    None

  let pingToString (p: PingType) =
    match p with
    | PingType.Everyone -> "@everyone"
    | PingType.Here -> "@here"
    | _ -> ""
