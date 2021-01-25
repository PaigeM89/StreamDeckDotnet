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

  let inline private runTask (t : Task) = t |> Async.AwaitTask

  let inline private runTaskT (t : Task<_>) = t |> Async.AwaitTask

  let BufferSize = 1024 * 1024

  // let awaitSocketAndFillBuffer (socketFunc : byref<ArraySegment<byte>> -> CancellationToken -> Task<WebSocketReceiveResult>) ct =
  //   let buf = Array.zeroCreate (BufferSize)
  //   let mutable arrayBuf = ArraySegment<_>(buf)
  //   let textBuf = StringBuilder(BufferSize)
  //   task {
  //     let! socketResult = socketFunc &arrayBuf ct
  //     while not (socketResult.EndOfMessage) do
  //       if socketResult.MessageType = WebSocketMessageType.Text then
  //         textBuf.Append(Encoding.UTF8.GetString(buf, 0, socketResult.Count)) |> ignore
        
  //     let msg = string textBuf
  //     textBuf.Clear() |> ignore
  //     return msg
  //   }

  // StreamDeck launches the plugin with these details
  // -port [number] -pluginUUID [GUID] -registerEvent [string?] -info [json]
  type StreamDeckConnection(args : StreamDeckSocketArgs, 
                            receiveHandler : string -> Async<Context.EventContext>,
                            registrationHandler : unit -> string) =

      // https://docs.microsoft.com/en-us/dotnet/api/system.net.websockets.clientwebsocket?view=net-5.0
      //let mutable _websocket : ClientWebSocket = new ClientWebSocket()
      
      // https://github.com/TheAngryByrd/FSharp.Control.WebSockets/blob/master/src/FSharp.Control.Websockets/FSharp.Control.Websockets.fs
      let mutable _tsWebsocket : ThreadSafeWebSocket = 
        ThreadSafeWebSocket.createFromWebSocket (new ClientWebSocket())

      let _isOpen() = WebSocket.isWebsocketOpen _tsWebsocket.websocket

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
        //if _websocket.State <> WebSocketState.Open then
        //if not (WebSocket.isWebsocketOpen _tsWebsocket.websocket) then
        if _isOpen() then
          let uri = $"ws://localhost:{port}"
          !! "Connecting web socket to port {port} with uri {uri}"
          >>!- ("port", port)
          >>!- ("uri", uri)
          |> logger.info
          let ws = new ClientWebSocket()
          do! ws.ConnectAsync(Uri(uri), _cancelSource.Token)
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

      let awaitAsyncReceive(buffer : ArraySegment<byte>) = ThreadSafeWebSocket.receiveMessageAsUTF8 _tsWebsocket
        //async {
          // let ct = _cancelSource.Token
          // let! result = WebSocket.asyncReceive _tsWebsocket.websocket buffer
          // return result
        //}


      // let awaitMessage() : Task<string> = 
      //   let buf = Array.zeroCreate (BufferSize)
      //   let arrayBuf = ArraySegment<_>(buf)
      //   let textBuf = StringBuilder(BufferSize)
      //   task {
      //     try
      //       !! "Awaiting socket receive" |> logger.trace
      //       do! lockAsync()
      //       //let! webSocketResult = _websocket.ReceiveAsync(arrayBuf, _cancelSource.Token)

      //       while not (webSocketResult.EndOfMessage) do
      //         if webSocketResult.MessageType = WebSocketMessageType.Text then
      //           !! "Received text message from web socket" |> logger.trace
      //           textBuf.Append(Encoding.UTF8.GetString(buf, 0, webSocketResult.Count)) |> ignore
      //           ()
      //         else
      //           !! "Web socket recieved non-text message. Type: {type}"
      //           >>!+ ("type", webSocketResult.MessageType)
      //           |> logger.trace
      //           ()

      //       let msg = string textBuf
      //       !! "Exiting loop, returning message: \"{msg}\""
      //       >>!+ ("msg", msg)
      //       |> logger.trace
      //       textBuf.Clear() |> ignore
      //       release()
      //       return msg
      //     with
      //     | ex ->
      //       release()
      //       !! "Error when awaiting message: {msg}\n{st}"
      //       >>!- ("msg", ex.Message)
      //       >>!+ ("st", ex)
      //       >>!! ex
      //       |> logger.error
      //       return raise ex
      //   }

      // let awaitMessage f = 
      //   let buf = Array.zeroCreate (BufferSize)
      //   let arrayBuf = ArraySegment<_>(buf)
      //   let textBuf = StringBuilder(BufferSize)
      //   task {
      //     !! "Awaiting socket receive" |> logger.trace
      //     let! webSocketResult = _websocket.ReceiveAsync(arrayBuf, _cancelSource.Token)
      //     !! "received from web socket, processing..." |> logger.trace
      //     if webSocketResult.MessageType = WebSocketMessageType.Text then
      //       !! "Received text message from web socket" |> logger.trace
      //       textBuf.Append(Encoding.UTF8.GetString(buf, 0, webSocketResult.Count)) |> ignore

      //       if webSocketResult.EndOfMessage then
      //         let msg = string textBuf
      //         !! "Received message of {msg} from web socket" >>!- ("msg", msg) |> logger.info
      //         let! ctx = receiveHandler (msg)
      //         do! f// ctx |> eventsEncoded |> this.SendAllToSocketAsync
      //         textBuf.Clear() |> ignore

      //       ()
      //     else
      //       !! "Web socket recieved non-text message. Type: {type}"
      //       >>!+ ("type", webSocketResult.MessageType)
      //       |> logger.trace
      //       ()
      //   }

      //splitting this into a separate function allows for better error logging
      let closeSocket = 
        async {
          //if isNull _websocket then !! "how did we get here" |> logger.error
          //_websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", token)
          //WebSocket.asyncClose _tsWebsocket.websocket WebSocketCloseStatus.NormalClosure "Closing web socket"
          let! result = ThreadSafeWebSocket.close _tsWebsocket WebSocketCloseStatus.NormalClosure "Closing web socket"
          match result with
          | Ok () -> ()
          | Error e ->
            !! "Error closing thread safe web socket: {err}"
            >>!+ ("err", e)
            |> logger.error
          
          return ()
        }

      member this.DisconnectAsync() = async {
          try
            //if not (isNull _websocket) && _websocket.State <> WebSocketState.Closed then
            //if WebSocket.isWebsocketOpen _tsWebsocket.websocket then
            if _isOpen() then
              !! "attempting disconnect socket, currently has state {state}"
              >>!+ ("state", _tsWebsocket.State)
              |> logger.debug
              do! lockAsync()
              !! "Closing socket" |> logger.info
              let token = _cancelSource.Token
              do! closeSocket
              !! "Disposing socket" |> logger.debug
              _tsWebsocket.websocket.Dispose()
              !! "Initializing new websocket" |> logger.trace
              do! connectWebsocket(port) |> Async.AwaitTask
            else
              !! "Websocket is attempting to disconnect, but is closed already."
              |> logger.warn
          finally
            do release |> ignore
        }

      member this.SendToSocketAsync(text : string) = async {
        try
          try
            //if WebSocket.isWebsocketOpen _tsWebsocket.websocket then
            if _isOpen() then
              !! "Sending {text} to socket" >>!- ("text", text) |> logger.info
              //let buf = Encoding.UTF8.GetBytes(text)
              let stream = System.IO.MemoryStream.UTF8toMemoryStream text
              let! sendResult =
                ThreadSafeWebSocket.sendMessage 
                  _tsWebsocket
                  BufferSize
                  WebSocketMessageType.Binary
                  stream

              match sendResult with
              | Ok _ -> ()
              | Error e ->
                !! "Error sending to thread safe websocket: {err}"
                >>!+("err", e)
                |> logger.error

            // if _websocket.State = WebSocketState.Open then
            //   do! _semaphore.WaitAsync() |> Async.AwaitTask
            //   !! "Sending {text} to socket" >>!- ("text", text) |> logger.info
            //   let buf = Encoding.UTF8.GetBytes(text)
            //   do! _websocket.SendAsync(ArraySegment<_>(buf), WebSocketMessageType.Text, true, _cancelSource.Token) |> Async.AwaitTask
            //   return ()
            // else
            //   return ()
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

      member this.Receive() = //awaitMessage (ctx |> eventsEncoded |> this.SendAllToSocketAsync) |> Async.awaitTask
        async {
          while not _cancelSource.IsCancellationRequested do
            //let! msg = awaitMessage() |> Async.AwaitTask
            let buf = Array.zeroCreate (BufferSize)
            let arrseg = ArraySegment<byte>(buf)
            let! msg = awaitAsyncReceive arrseg
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
          !! "Exiting receive loop due to cancellation" |> logger.trace
        }


      // member this.Receive() = //awaitMessage (ctx |> eventsEncoded |> this.SendAllToSocketAsync) |> Async.awaitTask
      //   let buf = Array.zeroCreate (BufferSize)
      //   let arrayBuf = ArraySegment<_>(buf)
      //   let textBuf = StringBuilder(BufferSize)
      //   async {
      //     while not _cancelSource.IsCancellationRequested do
      //       !! "Awaiting socket receive" |> logger.trace
      //       let! websocketResult = _websocket.ReceiveAsync(arrayBuf, _cancelSource.Token) |> Async.AwaitTask
      //       if websocketResult.MessageType = WebSocketMessageType.Text then
      //         !! "Received text message from web socket" |> logger.trace
      //         textBuf.Append(Encoding.UTF8.GetString(buf, 0, websocketResult.Count)) |> ignore

      //         if websocketResult.EndOfMessage then
      //           let msg = string textBuf
      //           !! "Received message of {msg} from web socket" >>!- ("msg", msg) |> logger.info
      //           let! ctx = receiveHandler (msg)
      //           do! ctx |> eventsEncoded |> this.SendAllToSocketAsync
      //           textBuf.Clear() |> ignore
      //         ()
      //       else
      //         !! "Web socket recieved non-text message. Type: {type}"
      //         >>!+ ("type", websocketResult.MessageType)
      //         |> logger.trace
      //         ()

      //     !! "Exiting receive loop due to cancellation" |> logger.trace
      //   }

      member this.RunAsync() = 
        let fin = async {
            do! this.DisconnectAsync()
          }
        let work = async {
            !! "Starting web socket, connecting... " |> logger.trace
            do! (connectWebsocket port) |> Async.AwaitTask
            //if _websocket.State <> WebSocketState.Open then
            if _isOpen() then
              !! "Websocket is not open after starting" |> logger.trace
              ()
            else
              !! "Web socket successfully connected, sending response" |> logger.trace
              let response = registrationHandler()
              !! "Response being sent to web socket is \"{response}\""
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