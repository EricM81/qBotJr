namespace qBotJr.T

open System

type MessageUpdate = {GuildID: uint64; MessageID: uint64; Action: Action}

[<Struct>]
type State =
  {
    mutable Servers: Map<uint64, Server>
    mutable cmdCreatorFilters: Command array
    mutable cmdStaticFilters: Command array
    mutable cmdTempFilters: MessageFilter list
    mutable rtServerFilters: ReactionFilter list
    mutable rtTempFilters: ReactionFilter list
  }

  static member create () =
    {
      State.Servers = Map.empty
      cmdCreatorFilters = Array.empty
      cmdStaticFilters = Array.empty
      cmdTempFilters = []
      rtServerFilters = []
      rtTempFilters = []
    }

type AsyncTask = delegate of byref<State> -> unit

type MailboxMessage =
  | NewMessage of NewMessage
  | MessageReaction of MessageReaction
  | UpdateState of AsyncTask

  static member createMessage goo msg: MailboxMessage = NewMessage (NewMessage.create msg goo)

  static member createReaction msg reaction isAdd goo: MailboxMessage =
    MessageReaction (MessageReaction.create msg reaction isAdd goo)

  static member createTask i: MailboxMessage = UpdateState i
