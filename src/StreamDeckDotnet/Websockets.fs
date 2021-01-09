namespace StreamDeckDotnet

open System
open System.Text
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks

open StreamDeckDotnet.Logging
open StreamDeckDotnet.Logger
open StreamDeckDotnet.Logger.Operators
open FSharp.Control.Tasks.Affine

module Websockets =

  type StreamDeckSocketArgs = {
    Port : int
    Id : Guid
    RegisterEvent : string
    Info : string
  }

  type IWebSocket =
    abstract member SendAsync : string -> Async<unit>
    abstract member ReceiveHandler : string -> Async<unit>

  let logger = LogProvider.getLoggerByName("StreamDeckDotnet.Websockets")

  let inline private runTask (t : Task) = t |> Async.AwaitTask

  let inline private runTaskT (t : Task<_>) = t |> Async.AwaitTask

  let BufferSize = 1024 * 1024

  // StreamDeck launches the plugin with these details
  // -port [number] -pluginUUID [GUID] -registerEvent [string?] -info [json]
  type StreamDeckConnection(args : StreamDeckSocketArgs, receiveHandler : string -> Async<Context.EventContext>) =
      let mutable _websocket : ClientWebSocket = new ClientWebSocket()
      let _cancelSource = new CancellationTokenSource()
      let _semaphore = new SemaphoreSlim(1)
      let port = args.Port

      let lockAsync() = _semaphore.WaitAsync() |> Async.AwaitTask
      let release() = _semaphore.Release()

      let connectWebsocket port = task {
        if _websocket.State <> WebSocketState.Open then
          do!  _websocket.ConnectAsync(Uri($"ws://localhost:{port}"), _cancelSource.Token)
        else
          ()
      }

      let eventsEncoded (ctx : Context.EventContext) =
        ctx.GetEventsToSend()
        |> List.map (fun payloads -> 
          payloads.Encode ctx.EventMetadata.Context ctx.EventMetadata.Device
        )

      member this.DisconnectAsync() = async {
          try
            do! lockAsync()
            do! _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", _cancelSource.Token) |> Async.AwaitTask
            _websocket.Dispose()
            _websocket <- new ClientWebSocket()
          finally
            do release |> ignore
        }

      member this.SendToSocketAsync(text : string) = async {
        try
          if _websocket.State = WebSocketState.Open then
            do! _semaphore.WaitAsync() |> Async.AwaitTask
            let buf = Encoding.UTF8.GetBytes(text)
            do! _websocket.SendAsync(ArraySegment<_>(buf), WebSocketMessageType.Text, true, _cancelSource.Token) |> Async.AwaitTask
            return ()
          else
            return ()
        finally
          _semaphore.Release() |> ignore
      }

      member this.SendAllToSocketAsync(payloads : string list) = async {
        let allAsyncs = payloads |> List.map this.SendToSocketAsync
        do! Async.Sequential allAsyncs |> Async.Ignore
        return ()
      }

      member this.Receive() =
        let buf = Array.zeroCreate (BufferSize)
        let arrayBuf = ArraySegment<_>(buf)
        let textBuf = StringBuilder(BufferSize)
        async {
          while not _cancelSource.IsCancellationRequested do
            let! websocketResult = _websocket.ReceiveAsync(arrayBuf, _cancelSource.Token) |> Async.AwaitTask
            if websocketResult.MessageType = WebSocketMessageType.Text then
              textBuf.Append(Encoding.UTF8.GetString(buf, 0, websocketResult.Count)) |> ignore
              printfn "Web socket recieved: %s" (string textBuf)

              if websocketResult.EndOfMessage then 
                let! ctx = receiveHandler (string textBuf)
                do! ctx |> eventsEncoded |> this.SendAllToSocketAsync
                textBuf.Clear() |> ignore
              ()
            else
              ()
        }

      member this.RunAsync() = 
        let fin = async {
            do! this.DisconnectAsync()
          }
        let work = async {
            do! (connectWebsocket port) |> Async.AwaitTask
            if _websocket.State <> WebSocketState.Open then
              ()
            else
              do! this.SendToSocketAsync("response from web socket!")
            do! this.Receive()
            return ()
          }
        Async.tryFinally fin work

      member this.Run() =
        this.RunAsync() |> Async.RunSynchronously

      member this.Stop() =
        _cancelSource.Cancel()

      interface IWebSocket with
        member this.SendAsync (str : string) = this.SendToSocketAsync str
        member this.ReceiveHandler (str : string) = () |> Async.lift