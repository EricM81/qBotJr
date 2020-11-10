namespace qBotJr

open System
open qBotJr.T

//test cases:
//        let s1 = @"commandWord -a ""multi word string1"" word1 word2 ""multi string2"" -b ""multi string3"" -c word3"
//        let s2 = @"commandWord 9 -a ""multi word string1"" word1 w ""multi string2"" -b ""multi string3"" -c ""3"""
//        let s3 = @"commandWord ""T"" -a ""multi word string1"" word1 word2 ""multi string2"" -b ""multi string3"" -c word3"
//        let s4 = @"commandWord ""Another Test"" -a ""multi word string1"" word1 word2 ""multi string2"" -b ""multi string3"" -c word3"
//        let s5 = @"-a"
//        let s6 = @"-a word1"
//        let s7 = @"-a ""word1 word2"""



module parser =

    let rec private findEnd (input : string) (pos : int) (len : int) (termChar : char) : int =
        let x = if (pos < len) then input.[pos] else '\000'
        match x with
        | '\000' -> pos - 1
        | x when x = termChar -> pos - 1
        | _ -> findEnd input (pos + 1) len termChar

    let rec private findWord (input : string) (pos : int) (len : int) : int * int =
        let x = if (pos < len) then input.[pos] else '\000'
        match x with
        | '\000' -> 0, -1
        | ' ' -> findWord input (pos + 1) len
        | '\"' ->
            let a = pos + 1
            a, (findEnd input a len '\"')
        | _ -> pos, (findEnd input (pos + 1) len ' ')

    let rec private parseValues (input : string) (pos : int) (len : int) (acc : string list) : int * string list =
        let x = if (pos < len) then input.[pos] else '\000'
        match x with
        | '\000'
        | '-' -> pos, List.rev acc
        | ' ' -> parseValues input (pos + 1) len acc
        | _ ->
            let (word, word') = findWord input pos len
            if word <= word' then
                parseValues input (word' + 2) len ((input.Substring(word, (word' + 1 - word))) :: acc)
            else
                parseValues input (word' + 2) len acc

    let private takeSwitchToUpper (input : string) (pos : int) (len : int) : char option =
        if (pos < len) then input.[pos] |> Char.ToUpper |> Some else None

    let rec private parseArgs
        (input : string)
        (pos : int)
        (len : int)
        (acc : CommandLineArgs list)
        : int * CommandLineArgs list
        =
        let x = if (pos < len) then input.[pos] else '\000'
        match x with
        | '\000' -> pos, List.rev acc
        | '-' ->
            let switch = takeSwitchToUpper input (pos + 1) len
            let (pos', values) = parseValues input (pos + 2) len []
            parseArgs input pos' len ((CommandLineArgs.create switch values) :: acc)
        | ' ' -> parseArgs input (pos + 1) len acc
        | _ ->
            let (pos', values) = parseValues input pos len []
            parseArgs input pos' len ((CommandLineArgs.create None values) :: acc)

    let rec parseInput (cmd : string) (input : string) : CommandLineArgs list =
        let (_, args) = parseArgs input cmd.Length input.Length []
        args
