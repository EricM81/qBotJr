open System.Xml.Schema

#load @".paket\load\netcoreapp3.1\main.group.fsx"
#r @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Collections.Specialized.dll"

open Newtonsoft.Json
open System
open System.IO
open System.Text
open Newtonsoft.Json

type CommandLineArg =
    {
    Switch : char option
    Values : string list
    }
    static member create prefix values =
        {Switch = prefix; Values = values}
type Command =
    {
    Name : string
    Arguments : CommandLineArg list
    }
    static member create name args =
        {Name = name; Arguments = args}
   
let rec findEnd (input : string) (pos : int) (len : int) (termChar : char) : int =
    let x = if (pos < len) then input.[pos] else '\000'
    match x with
    | '\000' -> pos - 1
    | x when x = termChar -> pos - 1
    | _ -> findEnd input (pos + 1) len termChar
 
let rec findWord (input : string) (pos : int) (len : int) : int * int =
    let x = if (pos < len) then input.[pos] else '\000'
    match x with
    | '\000' -> //end of input, force return
                0, -1
    | ' ' -> //skip spaces
                findWord input (pos + 1) len
    | '\"' -> //go till next quote
            let a = pos + 1
            a, (findEnd input a len '\"')
    | _ -> //go till next space
            pos, (findEnd input (pos + 1) len ' ')

let rec parseValues (input : string) (pos : int) (len : int) (acc : string list) : int * string list =
    let x = if (pos < len) then input.[pos] else '\000'
    match x with
    | '\000' | '-' -> //end of string or done with this switch's values, return acc
            pos, List.rev acc
    | ' ' -> //skip blank spaces
            parseValues input (pos + 1) len acc
    | _ -> //parse value
            let (word, word') = findWord input pos len
            if word <= word' then
                parseValues input (word' + 2) len ((input.Substring(word, (word' + 1 - word)))::acc)
            else
                parseValues input (word' + 2) len acc
            
    
    
let takeOne (input : string) (pos : int) (len : int) : char option =
    if (pos < len) then Some input.[pos] else None
    
let rec parseArgs (input : string) (pos : int) (len : int) (acc : CommandLineArg list) : int * CommandLineArg list = 
    let x = if (pos < len) then input.[pos] else '\000'
    match x with
    | '\000' -> //end of string
            pos, List.rev acc 
    | '-' -> //switch followed by list of values
            let switch = takeOne input (pos + 1) len
            let (pos', values) = parseValues input (pos + 2) len []
            parseArgs input pos' len ((CommandLineArg.create switch values)::acc)
    | ' ' -> //skip blank spaces
            parseArgs input (pos + 1) len acc 
    | x -> //default values not preceded by a -switch
            let (pos', values) = parseValues input pos len []
            parseArgs input pos' len ((CommandLineArg.create None values)::acc)
    
    

                
let rec parseInput (cmd : string) (input : string) : CommandLineArg list =
    let (x, args) = parseArgs input cmd.Length input.Length []
    args
    

let s1 = @"commandWord -a ""multi word string1"" word1 word2 ""multi string2"" -b ""multi string3"" -c word3"
let s2 = @"commandWord 9 -a ""multi word string1"" word1 w ""multi string2"" -b ""multi string3"" -c ""3"""
let s3 = @"commandWord ""T"" -a ""multi word string1"" word1 word2 ""multi string2"" -b ""multi string3"" -c word3"
let s4 = @"commandWord ""Another Test"" -a ""multi word string1"" word1 word2 ""multi string2"" -b ""multi string3"" -c word3"
let s5 = @"-a"
let s6 = @"-a word1"
let s7 = @"-a ""word1 word2"""

#time

let s1' = parseInput "commandWord" s1
let s2' = parseInput "commandWord" s2
let s3' = parseInput "commandWord" s3
let s4' = parseInput "commandWord" s4
let s5' = parseInput "" s5
let s6' = parseInput "" s6
let s7' = parseInput "" s7
()    
#time

#time
let test iter =
    for i = 1 to iter do
            
        let s1' = parseInput "commandWord" s1
        let s2' = parseInput "commandWord" s2
        let s3' = parseInput "commandWord" s3
        let s4' = parseInput "commandWord" s4
        let s5' = parseInput "" s5
        let s6' = parseInput "" s6
        let s7' = parseInput "" s7
        ()
    ()
    
#time

    
    
    
    


    //qNew <@!442438729207119892>		qNew @QButtsSr
    //qnew <@!679018301543677959>		qnew @Queue Bot
    //qnew <@!760644805969313823>		qnew @QButtsJr
    //qnew <@!255387600246800395> 	qnew @hoooooowdyhow 
    //qnew -a <#544636678954811392>	qnew -a #sub_chat_announcements
    //qnew 👌 <:kamiUHH:715006017506639962>	qnew :ok_hand: :kamiUHH:
    //qnew <@!643435344355917854>		qnew @paniniham
    //qnew <@!257947314625314816>		qnew @Indulgence82
    //https://www.google.com/search?q=current+time+zone
//
     
//
//
//
//let run (s : string) : commandArg list =
//    let x = String.
        //|> Seq.
    //let rec outer  args arg =  

//let guild = {GuildID = 132359939363569664UL; AdminRoles = [178555702430793728UL; 222429919655886849UL]; CaptainRoles = [751273804155715604UL]; PlayersPerGame = 9; AnnounceChannel = Some {ID = 544636678954811392UL; Name = "#sub_chat_announcements"}}
//saveGuild guild

    

//   
//
//
//let test func =
//    for i = 0 to 100000000 do
//        func listA
//        |> ignore
//        ()
//    ()

//    
//#time
//printfn "Arr"
//test charsToStringArr
//#time
//
//#time
//printfn "Concat"
//test charsToStringConcat
//#time


//
//let charsToStringRev (list : char list) : string =
//    let arr = list |> List.toArray
//    Array.Reverse arr
//    String arr
//    
//let listAppend tail head =
//    head :: tail
//    
//    

    
//    
//let rec parseWord (input : string) (i : int) (len : int) (acc : char list) : (int * char list) =
//    let x = if (i < len) then input.[i] else ' '
//    match x with 
//    | ' ' -> (i, acc)
//    | x -> parseWord input (i + 1) len (x::acc)
//    
//let rec parsePhrase (input : string) (i : int) (len : int) (acc : char list) : (int * char list) =
//    let x = if (i < len) then input.[i] else '\"'
//    match x with 
//    | '\"' -> (i, acc)
//    | x -> parsePhrase input (i + 1) len (x::acc)
//
////get args for command switch
//let rec parseValue (input : string) (i : int) (len : int) (switch : char option) (acc : string list) : (int * string list) =
//    let x = if (i < len) then input.[i] else '\000'
//    match x with
//    | '\000' | '-' -> (i, (List.rev acc))
//    | ' ' -> parseValue input (i + 1) len switch acc
//    | '\"' ->
//            let (i', value) = parsePhrase input (i + 1) len []
//            value
//            |> charsToStringRev
//            |> listAppend acc 
//            |> parseValue input (i' + 1) len switch
//    | x ->
//            let (i', value) = parseWord input i len []
//            value
//            |> charsToStringRev
//            |> listAppend acc 
//            |> parseValue input (i' + 1) len switch
//    
//let rec parseSwitch (input : string) (i : int) (len : int) (acc : CommandLineArg list) : (int * CommandLineArg list) =
//    let x = if (i < len) then input.[i] else '\000'
//    match x with
//    | '\000' -> (i, (List.rev acc))
//    | ' ' | '-' -> parseSwitch input (i + 1) len (List.rev acc) 
//    | x ->
//            let (i', value) = parseValue input (i + 1) len (Some x) []
//            parseSwitch input (i' + 1) len ((CommandLineArg.create (Some x) value)::acc)
//
//let rec parseValueOrSwitch (input : string) (i : int) (len : int) : CommandLineArg list =
//    let x = if (i < len) then input.[i] else '\000'
//    match x with
//    | '\000' -> []
//    | ' ' -> parseValueOrSwitch input (i + 1) len 
//    | '-' ->
//            let (i', value) = parseSwitch input (i + 1) len []
//            value
//    | x ->
//            let (i', value) = parseValue input i len None []
//            let (tmp , ret) = parseSwitch input (i' + 1) len [(CommandLineArg.create None value)]
//            ret
//            
//    
//let rec parseInput (cmd : string) (input : string) : CommandLineArg list =
//    //cmd = "qBot"
//    //can be followed by value or a switch " -x" for options or nothing for interactive options
//    parseValueOrSwitch input cmd.Length input.Length 
//    