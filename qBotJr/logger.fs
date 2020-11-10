namespace qBotJr

open System.IO
open System

//private singleton instance so we have a finalizer to close IO connections
type private _logger() =

    let mutable disposed = false

    let f =
        File.Open
            ((sprintf "%s%s-DiscoLog.txt" config.BotSettings.LogFileRoot (DateTime.Today.ToString("yyyyMMdd"))),
             FileMode.OpenOrCreate,
             FileAccess.Write,
             FileShare.Read)

    let sw = new StreamWriter(f)
    do
        f.Seek(0L, SeekOrigin.End) |> ignore
        ()

    let cleanup (disposing : bool) =
        if disposing = false then
            sw.Flush()
            sw.Close()
        else
            disposed <- true
        ()

    member this.WriteLine(str : string) =
        sw.WriteLine(str)
        sw.Flush()


    interface IDisposable with
        member this.Dispose() =
            cleanup (true)
            GC.SuppressFinalize(this)

    override this.Finalize() = cleanup (false)

//static singleton wrapper for file logger
type logger() =

    static let log = new _logger()

    static let agent =
        MailboxProcessor<string>
            .Start(fun inbox ->
                  let rec messageLoop () =
                      async {
                          let! msg = inbox.Receive()
                          log.WriteLine msg
                          return! messageLoop ()
                      }

                  messageLoop ())

    static member WriteLine(str : string) = agent.Post str
