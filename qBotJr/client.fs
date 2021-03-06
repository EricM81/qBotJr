﻿namespace qBotJr

open System
open Discord.WebSocket
open qBotJr.T
open qBotJr.parser
open qBotJr.helper


//Thread-safe MailboxProcessor to filter commands and update state
module client =

  /// examples for maintaining state in F# are lacking, but I needs it.
  ///
  /// the MailboxProcessor makes incoming messages single threaded, which gives
  /// thread safety but also creates a bottleneck.
  ///
  /// some fields on records held in State are mutable, but they were chosen
  /// judiciously - like TTL's or isDirty flags (akin to marking for GC or needing
  /// to redraw the UI). most are immutable and need to safely update the entire record
  /// held in state.
  ///
  /// functions that do something with user input have 3 options (ActionResult) :
  /// |> function
  /// | Done () -> ()              //For quick updates mutable fields.
  /// | Async a -> Async.Start a   //Run in thread pool; register update with mailbox (AsyncTask -> byref<State> -> ()).
  /// | Server server' -> state.Servers <- state.Servers |> Map.add server'.GuildID server'
  ///                              //Fast enough to not justify thread switch, but require a full add/update on record.
  ///
  /// to keep byref<State> from ever being handled by any other thread,
  /// ** state is a mutable value type **
  let mutable private state = State.create ()

  type private FoundMessage = Command * MessageFilter option
  type private FoundReaction = ReactionAction * ReactionFilter option

  /// these are you normal discord commands.  this bot uses 'q' as a prefix, but also allows for a
  /// temporary filter on any input.  for example, if someone runs qHere but forgets to specify a
  /// parameter, I can reply back that they need to specify -e to ping @everyone or -h for @here to
  /// keep them from having to retype an entire command
  module private command =

    let inline parseMsg (cmd: Command) (msg: SocketMessage) = parseInput cmd.PrefixUpper msg.Content |> ParsedMsg.create msg

    let inline matchPrefix (cmd: Command) (nm: NewMessage): bool =
      let str = nm.Message.Content
      let i = cmd.PrefixLength
      (str.Length >= i && str.Substring(0, i).ToUpper() = cmd.PrefixUpper)

    let matchArray (nm: NewMessage) (items: Command array): ContinueOption<NewMessage, FoundMessage> =
      items
      |> Array.tryFind (fun cmd -> matchPrefix cmd nm)
      |> function
      | Some cmd -> Found (cmd, None)
      | None -> Continue nm

    let searchStatic (nm: NewMessage): ContinueOption<NewMessage, FoundMessage> =
      //all static bot commands start with a "q"
      //no Q, no need to check
      let q = nm.Message.Content.[0]
      if (q = 'Q' || q = 'q') then matchArray nm state.cmdStaticFilters else Continue nm

    //cmds I can run for testing....or memeing
    let searchCreator (nm: NewMessage): ContinueOption<NewMessage, FoundMessage> =
      if isCreator nm.Goo.User = UserPermission.Creator then
        matchArray nm state.cmdCreatorFilters
      else
        Continue nm

    let matchList (nm: NewMessage) (items: Command list): Command option =
      items |> List.tryFind (fun cmd -> matchPrefix cmd nm)

    let matchFilter (msgGuild: uint64) (nm: NewMessage) (filter: MessageFilter): FoundMessage option =
      match filter.GuildID, filter.User with
      | id, None when id = msgGuild -> matchList nm filter.Items
      | id, Some u when id = msgGuild && u = nm.Goo.User.Id -> matchList nm filter.Items
      | _ -> None //user doesn't match
      |> function
      | Some cmd -> Some (cmd, Some filter)
      | None -> None

    let searchTemp (nm: NewMessage): ContinueOption<NewMessage, FoundMessage> =
      let now = DateTimeOffset.Now
      let msgGuild = nm.Goo.GuildID
      state.cmdTempFilters
      |> List.tryPick (fun filter -> if filter.TTL > now then matchFilter msgGuild nm filter else None)
      |> function
      | Some fm -> Found fm
      | None -> Continue nm


  /// admins will learn and run commands, but I didn't want to force every user to have to do the same
  /// user actions, like signaling they want to play, are handled through reactions to announcement messages
  /// I also wanted the ability to let a command with insufficient info to accept a reaction for a missing param
  /// which uses temporary filters (just like temp command filter, i.e. react ⚽ for -e, 🏈 for -h)
  module private reaction =

    let inline matchReaction (emoji: string) (item: ReAction): ReactionAction option =
      if item.Emoji = emoji then Some item.Action else None

    let inline matchFilterActions (emoji: string) (items: ReAction list): ReactionAction option =
      items |> List.tryPick (fun item -> matchReaction emoji item)

    let inline matchFilter (msgID: uint64) (userID: uint64) (emoji: string) (filter: ReactionFilter): FoundReaction option =
      match filter.MessageID, filter.UserID with
      | fMsg, None when fMsg = msgID -> matchFilterActions emoji filter.Items
      | fMsg, Some fUser when fMsg = msgID && fUser = userID -> matchFilterActions emoji filter.Items
      | _ -> None
      |> function
      | Some react -> Some (react, Some filter)
      | _ -> None

    let searchList (mr: MessageReaction) (items: ReactionFilter list): FoundReaction option =
      let msgID = mr.Message.Id
      let userID = mr.Reaction.UserId
      let emoji = mr.Reaction.Emote.Name
      let now = DateTimeOffset.Now
      items |> List.tryPick (fun item -> if item.TTL > now then matchFilter msgID userID emoji item else None)


    let searchServer (mr: MessageReaction): ContinueOption<MessageReaction, FoundReaction> =
      searchList mr state.rtServerFilters
      |> function
      | Some (action, _) -> Found (action, None) //remove the filter so TTL only expires when the server expires
      | _ -> Continue mr

    let searchTemp (mr: MessageReaction): ContinueOption<MessageReaction, FoundReaction> =
      searchList mr state.rtTempFilters
      |> function
      | Some fr -> Found fr
      | _ -> Continue mr

  let inline private add1ServerTTL (server: Server) = server.TTL <- DateTimeOffset.Now.AddHours (1.0)
  let inline private expireMsgFilterTTL (filter: MessageFilter) = filter.TTL <- DateTimeOffset.MinValue
  let inline private expireRtFilterTTL (filter: ReactionFilter) = filter.TTL <- DateTimeOffset.MinValue


  let inline private execCmd (cmd: Command) server (nm: NewMessage) =
    if cmd.MinPermission = UserPermission.Admin then
      add1ServerTTL server
    let pm = command.parseMsg cmd nm.Message
    let goo = nm.Goo
    if (getPerm goo.User) >= cmd.MinPermission then cmd.PermSuccess server goo pm else cmd.PermFailure server goo pm

  let inline private execRt action server mr = action server mr

  let inline private updateServer (server: Server option) =
    match server with
    | Some s -> state.Servers <- state.Servers |> Map.add s.GuildID s
    | None -> ()

  let inline private run expireFun execFun args guild (filterFun, filter) =
    match filter with
    | None -> ()
    | Some filter' -> expireFun filter'
    let server = getServer state.Servers guild
    execFun filterFun server args |> updateServer

  //99.99999% of incoming traffic should be ignored
  //letting async tasks on the thread pool do a check
  //before placing something into the Mailbox.
  let TestMsgFilters (msg: NewMessage) =
    command.searchStatic msg
    |> bindCont command.searchCreator
    |> bindCont command.searchTemp
    |> function
    | Found _ -> true
    | Continue _ -> false

  let TestRtFilters (mr: MessageReaction) =
    reaction.searchServer mr
    |> bindCont reaction.searchTemp
    |> function
    | Found _ -> true
    | Continue _ -> false

  //todo 99.9999% of NewMessages and MessageReactions are not a match.
  //Add a non-thread-safe method just for matching to reduce the entries placed in the mailbox.
  //They can be matched a second time using the mailbox to ensure it's still valid
  //(in case the state changed during the context switch)
  let private matchMailbox (mm: MailboxMessage) =
    match mm with
    | NewMessage nm ->
        command.searchStatic nm
        |> bindCont command.searchCreator
        |> bindCont command.searchTemp
        |> runCont run expireMsgFilterTTL execCmd nm nm.Goo.Guild
    | MessageReaction mr ->
        reaction.searchServer mr |> bindCont reaction.searchTemp |> runCont run expireRtFilterTTL execRt mr mr.Goo.Guild
    | UpdateState t -> t.Invoke &state

  let private processMail (inbox: MailboxProcessor<MailboxMessage>) =
    let rec msgLoop () =
      async {
        let! mm = inbox.Receive ()
        matchMailbox mm
        return! msgLoop ()
      }

    msgLoop ()

  let private agent = MailboxProcessor.Start (processMail)

  let initFilters creatorFilters staticFilters =
    state.cmdCreatorFilters <- creatorFilters
    state.cmdStaticFilters <- staticFilters

  //once a message is handled, it returns
  let Receive (mm: MailboxMessage) = agent.Post mm

  //Can add to F# linked lists without worrying about thread safety
  let AddMessageFilter (filter: MessageFilter): unit = state.cmdTempFilters <- filter :: state.cmdTempFilters

  let AddReactionFilter (filter: ReactionFilter): unit = state.rtTempFilters <- filter :: state.rtTempFilters

  let AddServerReactionFilter (filter: ReactionFilter): unit = state.rtServerFilters <- filter :: state.rtServerFilters
