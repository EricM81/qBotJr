namespace qBotJr.T


[<Struct>]
type State =
    {
    mutable Servers : Map<uint64, Server>
    mutable cmdCreatorFilters : Command array
    mutable cmdStaticFilters : Command array
    mutable cmdTempFilters : MessageFilter list
    mutable reaServerFilters : ReactionFilter list
    mutable reaTempFilters : ReactionFilter list
    }
    static member create =
        {State.Servers = Map.empty; cmdCreatorFilters = Array.empty; cmdStaticFilters = Array.empty; cmdTempFilters = []; reaServerFilters = []; reaTempFilters = []}

type ScheduledTask = delegate of byref<State> -> unit

type MailboxMessage =
    | NewMessage of NewMessage
    | MessageReaction of MessageReaction
    | Task of ScheduledTask
    static member createMessage goo msg : MailboxMessage =
        NewMessage (NewMessage.create msg goo)
    static member createReaction  msg reaction isAdd : MailboxMessage=
        MessageReaction (MessageReaction.create msg reaction isAdd)
    static member createTask i : MailboxMessage =
        Task i
