namespace qBotJr

open System
open System.Collections.Generic

module emojis =

  let dict =
    let tmp = Dictionary<string, string> ()
    tmp.Add ("Distrust", "🤨")
    tmp.Add ("Sleep", "💤")
    tmp.Add ("Ok", "👌")
    tmp.Add ("Cancel", "❌")
    tmp.Add ("ThumbsDown", "👎")
    tmp.Add ("Back", "👈")
    tmp.Add ("Forward", "👉")
    tmp.Add ("Up", "👆")
    tmp.Add ("Down", "👇")
    tmp.Add ("FU", "🖕")
    tmp.Add ("RaiseHands", "🙌")
    tmp.Add ("ThumbsUp", "👍")
    tmp.Add ("Ear", "👂")
    tmp.Add ("MnK", "🐒")
    tmp.Add ("Controller", "🎮")
    tmp.Add ("Keyboard", "⌨")
    tmp.Add ("Bullseye", "🎯")
    tmp.Add ("GameMode0Soccer", "⚽")
    tmp.Add ("GameMode1Baseball", "⚾")
    tmp.Add ("GameMode2Basketball", "🏀")
    tmp.Add ("GameMode3Football", "🏈")
    tmp.Add ("GameMode4Frisbee", "🥏")
    tmp.Add ("GameMode5Bowling", "🎳")
    tmp.Add ("GameMode6Hockey", "🏒")
    tmp.Add ("GameMode7PingPong", "🏓")
    tmp.Add ("GameMode8Boxing", "🥊")
    tmp.Add ("GameMode9Karate", "🥋")
    tmp.Add ("Hashtag", "#️⃣")
    tmp.Add ("Star", "*️⃣")
    tmp.Add ("Zero", "0️⃣")
    tmp.Add ("One", "1️⃣")
    tmp.Add ("Two", "2️⃣")
    tmp.Add ("Three", "3️⃣")
    tmp.Add ("Four", "4️⃣")
    tmp.Add ("Five", "5️⃣")
    tmp.Add ("Six", "6️⃣")
    tmp.Add ("Seven", "7️⃣")
    tmp.Add ("Eight", "8️⃣")
    tmp.Add ("Nine", "9️⃣")
    tmp.Add ("playerA", "🇦")
    tmp.Add ("playerB", "🇧")
    tmp.Add ("playerC", "🇨")
    tmp.Add ("playerD", "🇩")
    tmp.Add ("playerE", "🇪")
    tmp.Add ("playerF", "🇫")
    tmp.Add ("playerG", "🇬")
    tmp.Add ("playerH", "🇭")
    //tmp.Add("playerI", "🇮")
    tmp.Add ("playerJ", "🇯")
    tmp.Add ("playerK", "🇰")
    //tmp.Add("playerL", "🇱")
    tmp.Add ("playerM", "🇲")
    tmp.Add ("playerN", "🇳")
    //tmp.Add("playerO", "🇴")
    //tmp.Add("playerP", "🇵")
    //tmp.Add("playerQ", "🇶")
    tmp.Add ("playerR", "🇷")
    tmp.Add ("playerS", "🇸")
    tmp.Add ("playerT", "🇹")
    tmp.Add ("playerU", "🇺")
    //tmp.Add("playerV", "🇻")
    tmp.Add ("playerW", "🇼")
    tmp.Add ("playerX", "🇽")
    tmp.Add ("playerY", "🇾")
    tmp.Add ("playerZ", "🇿")
    tmp

  let Distrust = "🤨"
  let Sleep = "💤"
  let Ok = "👌"
  let Cancel = "❌"
  let Back = "👈"
  let Forward = "👉"
  let Up = "👆"
  let Down = "👇"
  let FU = "🖕"
  let RaiseHands = "🙌"
  let ThumbsUp = "👍"
  let ThumbsDown = "👎"
  let Ear = "👂"
  let MnK = "🐒"
  let Controller = "🎮"
  let Keyboard = "⌨"
  let Bullseye = "🎯"
  let GameMode0Soccer = "⚽"
  let GameMode1Baseball = "⚾"
  let GameMode2Basketball = "🏀"
  let GameMode3Football = "🏈"
  let GameMode4Frisbee = "🥏"
  let GameMode5Bowling = "🎳"
  let GameMode6Hockey = "🏒"
  let GameMode7PingPong = "🏓"
  let GameMode8Boxing = "🥊"
  let GameMode9Karate = "🥋"
  let Hashtag = "#️⃣"
  let Star = "*️⃣"
  let Zero = "0️⃣"
  let One = "1️⃣"
  let Two = "2️⃣"
  let Three = "3️⃣"
  let Four = "4️⃣"
  let Five = "5️⃣"
  let Six = "6️⃣"
  let Seven = "7️⃣"
  let Eight = "8️⃣"
  let Nine = "9️⃣"
  let playerA = "🇦"
  let playerB = "🇧"
  let playerC = "🇨"
  let playerD = "🇩"
  let playerE = "🇪"
  let playerF = "🇫"
  let playerG = "🇬"
  let playerH = "🇭"
  let playerI = "🇮"
  let playerJ = "🇯"
  let playerK = "🇰"
  let playerL = "🇱"
  let playerM = "🇲"
  let playerN = "🇳"
  let playerO = "🇴"
  let playerP = "🇵"
  let playerQ = "🇶"
  let playerR = "🇷"
  let playerS = "🇸"
  let playerT = "🇹"
  let playerU = "🇺"
  let playerV = "🇻"
  let playerW = "🇼"
  let playerX = "🇽"
  let playerY = "🇾"
  let playerZ = "🇿"

  let letterItems =
    [
      playerA
      playerB
      playerC
      playerD
      playerE
      playerF
      playerG
      playerH
      playerI
      playerJ
      playerK
      playerL
      playerM
      playerN
      playerO
      playerP
      playerQ
      playerR
      playerS
      playerT
      playerU
      playerV
      playerW
      playerX
      playerY
      playerZ
    ]

  let sports =
    [
      GameMode0Soccer
      GameMode1Baseball
      GameMode2Basketball
      GameMode3Football
      GameMode4Frisbee
      GameMode5Bowling
      GameMode6Hockey
      GameMode7PingPong
      GameMode8Boxing
      GameMode9Karate
    ]
  let private rng = Random ()

  let GetRandomLetter () =
    let index = rng.Next (letterItems.Length)
    if index < letterItems.Length then List.item index letterItems else playerA
