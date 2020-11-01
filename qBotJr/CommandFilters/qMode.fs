namespace qBotJr
open Discord
open System
open qBotJr.T
open FSharp.Control
open System.Collections.Generic

module qMode = 

//type cmdGuildFunc = (SocketMessage) -> (SocketGuildChannel) -> (SocketGuildUser) -> unit 

    let str = "QMODE"
  
    let Run  (pm : ParsedMsg) (goo : GuildOO) : unit =
        ()
    
    let noPerms  (pm : ParsedMsg) (goo : GuildOO) : unit =
        ()
        
    let collectMsgs (acc) (KeyValue(k,v) : KeyValuePair<uint64, Server>) = //: Async<(RestUserMessage * Player list)> list=
        match v.HereMsg with
        | Some msg ->
            let x = 
                async
                    {
                        let tmp : System.Collections.Generic.IAsyncEnumerable<IEnumerable<IUser>> =
                            downcast msg.GetReactionUsersAsync(Emoji (Emojis.RaiseHands), 1000)
                        let! reactions =
                            AsyncEnumerableExtensions.FlattenAsync tmp
                            |> Async.AwaitTask
                        return (reactions, v)
                    }
            x::acc
        | _ -> acc
        
       
    let processPlayerReactions currentServers =
    
        //get server's qhere's
        let values = 
            currentServers 
            |> Seq.fold collectMsgs []
            |> FSharpx.Control.Async.ParallelWithThrottle 4
            |> Async.RunSynchronously
        for value in values do
            let (reaction, server) = value
            server.Players
            |> List.iter (fun p ->
                p.isHere <-
                    reaction
                    |> Seq.exists (fun x ->
                        if x.Id = p.UID then true else false)
                )
            
//' Discord.AsyncEnumerableExtensions
//''' <summary> Flattens the specified pages into one <see cref="T:System.Collections.Generic.IEnumerable`1" /> asynchronously. </summary>
//' Token: 0x060003A5 RID: 933 RVA: 0x000055E8 File Offset: 0x000037E8
//<System.Runtime.CompilerServices.ExtensionAttribute()>
//Public Shared Function FlattenAsync(Of T)(source As IAsyncEnumerable(Of IEnumerable(Of T))) As Task(Of IEnumerable(Of T))
//	Dim <FlattenAsync>d__ As AsyncEnumerableExtensions.<FlattenAsync>d__0(Of T)
//	<FlattenAsync>d__.source = source
//	<FlattenAsync>d__.<>t__builder = AsyncTaskMethodBuilder(Of IEnumerable(Of T)).Create()
//	<FlattenAsync>d__.<>1__state = -1
//	Dim <>t__builder As AsyncTaskMethodBuilder(Of IEnumerable(Of T)) = <FlattenAsync>d__.<>t__builder
//	<>t__builder.Start(Of AsyncEnumerableExtensions.<FlattenAsync>d__0(Of T))(<FlattenAsync>d__)
//	Return <FlattenAsync>d__.<>t__builder.Task
//End Function

//' Discord.AsyncEnumerableExtensions
//''' <summary> Flattens the specified pages into one <see cref="T:System.Collections.Generic.IAsyncEnumerable`1" />. </summary>
//' Token: 0x060003A6 RID: 934 RVA: 0x0000562D File Offset: 0x0000382D
//<System.Runtime.CompilerServices.ExtensionAttribute()>
//Public Shared Function Flatten(Of T)(source As IAsyncEnumerable(Of IEnumerable(Of T))) As IAsyncEnumerable(Of T)
//	Dim <>9__1_ As Func(Of IEnumerable(Of T), IAsyncEnumerable(Of T)) = AsyncEnumerableExtensions.<>c__1(Of T).<>9__1_0
//	Dim func As Func(Of IEnumerable(Of T), IAsyncEnumerable(Of T)) = <>9__1_
//	If <>9__1_ Is Nothing Then
//		Dim func2 As Func(Of IEnumerable(Of T), IAsyncEnumerable(Of T)) = Function(enumerable As IEnumerable(Of T)) AsyncEnumerable.ToAsyncEnumerable(Of T)(enumerable)
//		func = func2
//		AsyncEnumerableExtensions.<>c__1(Of T).<>9__1_0 = func2
//	End If
//	Return AsyncEnumerable.SelectMany(Of IEnumerable(Of T), T)(source, func)
//End Function
