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

      let lockAsync() = 
        !! "Locking socket" |> logger.info
        _semaphore.WaitAsync() |> Async.AwaitTask
      let release() = 
        !! "Unlocking socket" |> logger.info
        _semaphore.Release()

      let connectWebsocket (port : int) = task {
        if _websocket.State <> WebSocketState.Open then
          let uri = $"ws://localhost:{port}"
          !! "Connecting web socket to port {port} with uri {uri}"
          >>!- ("port", port)
          >>!- ("uri", uri)
          |> logger.info
          do!  _websocket.ConnectAsync(Uri(uri), _cancelSource.Token)
          !! "Finished connecting web socket" |> logger.trace
        else
          ()
      }

      let eventsEncoded (ctx : Context.EventContext) =
        ctx.GetEventsToSend()
        |> List.map (fun payloads -> 
          let payload = payloads.Encode ctx.EventMetadata.Context ctx.EventMetadata.Device
          !! "Created event sent paylod of {payload}"
          >>!- ("payload", payload)
          |> logger.debug
          payload
        )

      //splitting this into a separate function allows for better error logging
      let closeSocket token = 
        try
          if isNull _websocket then !! "how did we get here" |> logger.error
          _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", token)
        with
        | e -> 
          !! "Error closing web socket: {msg}"
          >>!+ ("msg", e.Message)
          >>!! e
          |> logger.error
          Task.FromException e

      member this.DisconnectAsync() = async {
          try
            if not (isNull _websocket) && _websocket.State <> WebSocketState.Closed then
              !! "attempting disconnect socket, currently has state {state}"
              >>!+ ("state", _websocket.State)
              |> logger.debug
              do! lockAsync()
              !! "Closing socket" |> logger.info
              let token = _cancelSource.Token
              do! closeSocket token |> Async.AwaitTask
              !! "Disposing socket" |> logger.debug
              _websocket.Dispose()
              !! "Initializing new websocket" |> logger.trace
              _websocket <- new ClientWebSocket()
            else
              !! "Websocket is attempting to disconnect, but is closed already."
              |> logger.warn
          finally
            do release |> ignore
        }

      member this.SendToSocketAsync(text : string) = async {
        try
          try
            if _websocket.State = WebSocketState.Open then
              do! _semaphore.WaitAsync() |> Async.AwaitTask
              !! "Sending {text} to socket" >>!- ("text", text) |> logger.info
              let buf = Encoding.UTF8.GetBytes(text)
              do! _websocket.SendAsync(ArraySegment<_>(buf), WebSocketMessageType.Text, true, _cancelSource.Token) |> Async.AwaitTask
              return ()
            else
              return ()
          with
          | e ->
            !! "Received error {e} when sending to socket" >>!- ("e", e.Message) >>!! e |> logger.error
            ()
        finally
          _semaphore.Release() |> ignore
      }

      member this.SendAllToSocketAsync(payloads : string list) = async {
        !! "Sending multiple payloads to socket" |> logger.trace
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
            !! "Awaiting socket receive" |> logger.trace
            let! websocketResult = _websocket.ReceiveAsync(arrayBuf, _cancelSource.Token) |> Async.AwaitTask
            if websocketResult.MessageType = WebSocketMessageType.Text then
              !! "Received text message from web socket" |> logger.trace
              textBuf.Append(Encoding.UTF8.GetString(buf, 0, websocketResult.Count)) |> ignore
              printfn "Web socket recieved: %s" (string textBuf)

              if websocketResult.EndOfMessage then
                let msg = string textBuf
                !! "Received message of {msg} from web socket" >>!- ("msg", msg) |> logger.info
                let! ctx = receiveHandler (msg)
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
            !! "Starting web socket, connecting... " |> logger.trace
            do! (connectWebsocket port) |> Async.AwaitTask
            if _websocket.State <> WebSocketState.Open then
              !! "Websocket is not open after starting" |> logger.trace
              ()
            else
              !! "Web socket successfully connected, sending response" |> logger.trace
              do! this.SendToSocketAsync("response from web socket!")
            !! "this.Receive() call in runAsync" |> logger.trace
            do! this.Receive()
            !! "Receive() received, returning in work func" |> logger.trace
            return ()
          }
        Async.tryFinally fin work

      member this.Run() = this.RunAsync() |> Async.RunSynchronously

      member this.Stop() =
        _cancelSource.Cancel()

      interface IWebSocket with
        member this.SendAsync (str : string) = this.SendToSocketAsync str
        member this.ReceiveHandler (str : string) = () |> Async.lift