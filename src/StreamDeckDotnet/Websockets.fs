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
  open FSharp.Control.Websockets
  open FSharp.Control.Websockets.Stream
  open FSharp.Control.Websockets.ThreadSafeWebSocket

  type StreamDeckSocketArgs = {
    Port : int
    PluginUUID : Guid
    RegisterEvent : string
    Info : string
  }

  type IWebSocket =
    abstract member SendAsync : string -> Async<unit>
    abstract member ReceiveHandler : string -> Async<unit>

  let logger = LogProvider.getLoggerByName("StreamDeckDotnet.Websockets")

  let BufferSize = 1024 * 1024

  // StreamDeck launches the plugin with these details
  // -port [number] -pluginUUID [GUID] -registerEvent [string?] -info [json]
  type StreamDeckConnection(args : StreamDeckSocketArgs, 
                            receiveHandler : string -> Async<Context.EventContext>,
                            registrationHandler : unit -> string) =

      
      // https://github.com/TheAngryByrd/FSharp.Control.WebSockets/blob/master/src/FSharp.Control.Websockets/FSharp.Control.Websockets.fs
      let mutable _tsWebsocket : ThreadSafeWebSocket = 
        ThreadSafeWebSocket.createFromWebSocket (new ClientWebSocket())

      let _isOpen() = WebSocket.isWebsocketOpen _tsWebsocket.websocket
      let _isClosed() = WebSocket.isWebsocketOpen _tsWebsocket.websocket |> not

      let _cancelSource = new CancellationTokenSource()
      
      // https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=net-5.0
      let _semaphore = new SemaphoreSlim(1)
      let port = args.Port

      let lockAsync() = 
        !! "Locking socket" |> logger.info
        _semaphore.WaitAsync() |> Async.AwaitTask
      let release() = 
        !! "Unlocking socket" |> logger.info
        _semaphore.Release() |> ignore

      let connectWebsocket (port : int) = task {
        if _isClosed() then
          let uri = $"ws://localhost:{port}"
          !! "Connecting web socket to port {port} with uri {uri}"
          >>!- ("port", port)
          >>!- ("uri", uri)
          |> logger.info
          let ws = new ClientWebSocket()
          do! ws.ConnectAsync(Uri(uri), _cancelSource.Token)
          !! "Creating thread safe web socket from web socket" |> logger.trace
          _tsWebsocket <- ThreadSafeWebSocket.createFromWebSocket ws
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

      let awaitAsyncReceive () =
        !! "calling ts websocket receive message as utf8" |> logger.trace
        ThreadSafeWebSocket.receiveMessageAsUTF8 _tsWebsocket

      //splitting this into a separate function allows for better error logging
      let closeSocket = 
        !! "In close socket function" |> logger.trace
        async {
          let! result = ThreadSafeWebSocket.close _tsWebsocket WebSocketCloseStatus.NormalClosure "Closing web socket"
          match result with
          | Ok () ->
            !! "Socket closed successfully" |> logger.trace
            ()
          | Error e ->
            !! "Error closing thread safe web socket: {err}"
            >>!+ ("err", e)
            |> logger.error
          
          return ()
        }

      member this.DisconnectAsync() = async {
          if _isOpen() then
            !! "attempting disconnect socket, currently has state {state}"
            >>!+ ("state", _tsWebsocket.State)
            |> logger.debug
            !! "Closing socket" |> logger.info
            do! closeSocket
            !! "Disposing socket" |> logger.debug
            _tsWebsocket.websocket.Dispose()
            !! "Initializing new websocket" |> logger.trace
            do! connectWebsocket(port) |> Async.AwaitTask
          else
            !! "Websocket is attempting to disconnect, but is not open and is in state {state}"
            >>!+ ("state", _tsWebsocket.State)
            |> logger.warn
        }

      member this.SendToSocketAsync(text : string) = async {
        try
          try
            if _isOpen() then
              !! "Sending {text} to socket" >>!- ("text", text) |> logger.info
              let! sendResult =
                ThreadSafeWebSocket.sendMessageAsUTF8
                  _tsWebsocket
                  text

              match sendResult with
              | Ok _ -> ()
              | Error e ->
                !! "Error sending to thread safe websocket: {err}"
                >>!+("err", e)
                |> logger.error
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
        async {
          !! "Entering while loop to recieve & send messages" |> logger.trace
          while _isOpen() do
            try
              !! "Calling await async receive in core loop" |> logger.trace
              let! msg = awaitAsyncReceive()
              !! "After await async receive in core loop" |> logger.trace
              match msg with
              | Ok (WebSocket.ReceiveUTF8Result.String msgText) ->
                !! "Received message of {msg} from web socket" >>!- ("msg", msg) |> logger.info
                let! ctx = receiveHandler (msgText)
                do! ctx |> eventsEncoded |> this.SendAllToSocketAsync
              | Ok (WebSocket.ReceiveUTF8Result.Closed (status, description)) ->
                !! "Received closed receive message from web socket. Status: {status}. Description: {desc}"
                >>!+ ("status", status)
                >>!- ("desc", description)
                |> logger.error
              | Error e ->
                !! "Received error {err} when awaiting async recieve for web socket"
                >>!+ ("err", e)
                |> logger.error
            with
            | ex ->
              !! "Exiting receive loop due to error {msg}:\n{st}"
              >>!+ ("msg", ex.Message)
              >>!+ ("st", ex.StackTrace)
              |> logger.error

          !! "Exiting receive loop due to socket closure" |> logger.trace
        }

      member this.RunAsync() = 
        let fin = async {
            !! "Finishing async work, disposing socket" |> logger.trace
            do! this.DisconnectAsync()
          }
        let work = async {
            !! "Starting web socket, connecting... " |> logger.trace
            do! (connectWebsocket port) |> Async.AwaitTask
            if not (_isOpen()) then
              !! "Websocket is not open after starting" |> logger.trace
              ()
            else
              !! "Web socket successfully connected in state {state}, sending registration response." 
              >>!+ ("state", _tsWebsocket.State)
              |> logger.trace
              let response = registrationHandler()
              !! "Response being sent to web socket is:\n  \'{response}\'"
              >>!- ("response", response)
              |> logger.info
              do! this.SendToSocketAsync(response)
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