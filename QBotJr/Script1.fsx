#load @".paket\load\netcoreapp3.1\main.group.fsx"

open Newtonsoft.Json
open System
open System.IO
open System.Text



let configRoot = @"C:\Users\EricM\Documents\Dev\QBotJr\QBotJr\"

[<Struct>]
type DiscordConnectionSettings  = {
    DiscordToken: string;
    SomethingElse: string
    }

let tmp = {DiscordToken = @"NzYwNjQ0ODA1OTY5MzEzODIz.X3PDrQ.YU12w__n_cm6WVZdTxhOsEFvZ8E"; SomethingElse = "stuff"}

let f = File.Open(configRoot + "settings.json", FileMode.OpenOrCreate, FileAccess.Write)
let sw = new StreamWriter(f)
let str = JsonConvert.SerializeObject(tmp)
sw.Write str
sw.Close ()
f.Close ()


let f2 = File.Open(configRoot + "settings.json", FileMode.Open, FileAccess.Read)
let sr = new StreamReader(f2)

let tmp2 = JsonConvert.DeserializeObject<DiscordConnectionSettings>(sr.ReadToEnd())
sr.Close()

f2.Close()

tmp = tmp2 

let addU (i : uint32) (x : uint32) : uint32 = 
    x + i

let toU (tmp : byte[]) : uint32 = 
    BitConverter.ToUInt32(tmp, 0)

let rev arr = 
    Array.Reverse arr
    arr

let uniMap (abits : byte[]) (i : uint) = 
    abits |> toU |> addU i |> BitConverter.GetBytes |> rev |> Encoding.UTF8.GetString

let i = 0x1F1E6
let c = char(i)
let c2 = "🇦"
c2.Length
let c3 = c2.Chars(0)
let c4 = c2.Chars(1)
let d = "\x1F1E6"
let e = "🇦"
Encoding.BigEndianUnicode.GetBytes(e)
Encoding.UTF8.GetBytes(e)
let x = Encoding.UTF8.GetBytes(e) |> rev |> toU |> addU |> BitConverter.GetBytes |> rev
let z = "🇧"
Encoding.UTF8.GetBytes(z)
BitConverter.IsLittleEndian
Encoding.BigEndianUnicode.GetBytes(z)
let a = "🇦"
let b = "🇧"
Encoding.UTF8.GetBytes(a)
Encoding.UTF8.GetBytes(b)
let abits = Encoding.UTF8.GetBytes "🇦" |> rev

let ret = [0u..25u]
let names = ['A'..'Z']
ret |> List.map (fun i -> uniMap abits i) |> List.map2 (fun n s -> printfn "let player%c = \"%s\"" n s ) names

//a |> Encoding.UTF8.GetBytes |> rev |> toU |> addU |> BitConverter.GetBytes |> rev |> Encoding.UTF8.GetString

let rec col (acc' : string) (i : int) (emotes' : string list) : string =
    let str = 
        match emotes' with 
        | head :: tail when i > 9 -> (head.ToString())
        | head :: tail -> (col (acc' + (head.ToString())) (i + 1) tail)
        | [] -> ""
    str
