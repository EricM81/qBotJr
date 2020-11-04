namespace qBotJr


module helper =
    let inline prepend xs x = x::xs

    [<StructuralEquality; StructuralComparison>]
        [<Struct>]
    type CmdOption<'T> = 
        | Continue of Continue : 'T 
        | Completed 

    let inline bind2 f g x = 
        match x with 
        | Continue y -> f g y
        | Completed -> Completed

    let inline bind f x =
        match x with
        | Continue x -> f x
        | Completed -> Completed
