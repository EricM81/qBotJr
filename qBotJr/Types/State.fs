namespace qBotJr.T


[<Struct>]
type State =
    {
    mutable Guilds : Map<uint64, Server>
    mutable CreatorFilters : Command array
    mutable StaticFilters : Command array
    mutable DynamicFilters : MessageFilter list
    mutable ReactionFilters : ReactionFilter list
    }
    static member create  =
        {State.Guilds = Map.empty; CreatorFilters = Array.empty; StaticFilters = Array.empty; DynamicFilters = []; ReactionFilters = []}



type ScheduledTask = delegate of byref<State> -> unit



type MailboxMessage =
    | NewMessage of NewMessage //: NewMessage
    | MessageReaction  of MessageReaction //: MessageReaction
    | Task of ScheduledTask //: ScheduledTask
    static member createMessage goo msg : MailboxMessage =
        NewMessage (NewMessage.create msg goo)
    static member createReaction  msg reaction isAdd goo : MailboxMessage=
        MessageReaction (MessageReaction.create goo msg reaction isAdd)
    static member createTask i : MailboxMessage =
        Task i
