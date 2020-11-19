namespace qBotJr
open System
open Discord.WebSocket
open System.Text
open discord
open qBotJr.T
open helper

module qHelp =

    let private printMan : string =

        let sb = StringBuilder()
        let a format = bprintfn sb format

        a ">>> **Here's the available commands.**"
        a "(run any command without parameters for help)"
        a ""
        a "Admin Stuff"
        a "```"
        a "qBot - Configure your servers bot permissions"
        a "qHere - Announce games; users react and are \'here\'"
        a "qMode - Let weirdos opt in for a meme match"
        a "qNew - Round up the next group from the queue"
        a "qSet - Manually adjust a user's games played count"
        a "qBan - Ban a player from playing again tonight"
        a "qCustoms - Set a time for next game night"
        a "```"
        a "Captain Stuff"
        a "(captains herd cats while you play)"
        a "```"
        a "qAFK - Marks a user AFK; user re-reacts to be \'here\' agane"
        a "qKick - Removes user from this match; 1st in line next match"
        a "qAdd - Gets next in line after an AFK or Kick"
        a "qClose - Close the Lobby Channel after the match"
        a "```"
        a "Sub Stuff"
        a "```"
        a "qNext - Spits out the Days:Hours:Minutes to next game night"
        a "```"

        sb.ToString()

    let Run (_ : Server) (goo : GuildOO) (_ : ParsedMsg) : Server option =
        printMan
        |> sendMsg goo.Channel
        |> ignore
        None

    let Command = Command.create "QHELP" UserPermission.None Run reactDistrust
