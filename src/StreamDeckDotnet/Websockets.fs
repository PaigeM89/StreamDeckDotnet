namespace StreamDeckDotnet

open System
open System.Text
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks
open FSharp.Control.Tasks.Affine

module Websockets =

  let inline private runTask (t : Task) = t |> Async.AwaitTask |> Async.RunSynchronously

  let inline private runTaskT (t : Task<_>) =
    t |> Async.AwaitTask |> Async.RunSynchronously

  type StreamDeckConnection(port : int) =
      let _websocket : ClientWebSocket = new ClientWebSocket()
      let _cancelSource = new CancellationTokenSource()

      // do
      //   _websocket.ConnectAsync(Uri($"ws://localhost:{port}"), _cancelSource.Token) |> runTask

      let connectWebsocket port = task {
        if _websocket.State <> WebSocketState.Open then
          do! _websocket.ConnectAsync(Uri($"ws://localhost:{port}"), _cancelSource.Token)
        else ()
      }

      member this.Send(text : string) =
        let buf = Encoding.UTF8.GetBytes(text)
        _websocket.SendAsync(ArraySegment<_>(buf), WebSocketMessageType.Text, true, _cancelSource.Token)
        |> Async.AwaitTask
        |> Async.RunSynchronously

      member this.Receive() =
        let buf = Array.zeroCreate (1024*1024)
        let arrayBuf = ArraySegment<_>(buf)
        let textBuf = StringBuilder(1024 * 1024)

        while not _cancelSource.IsCancellationRequested do
          let websocketResult = _websocket.ReceiveAsync(arrayBuf, _cancelSource.Token) |> runTaskT
          if websocketResult.MessageType = WebSocketMessageType.Text then
            textBuf.Append(Encoding.UTF8.GetString(buf, 0, websocketResult.Count)) |> ignore
            printfn "Web socket recieved: %s" (string textBuf)
            textBuf.Clear() |> ignore
            ()
          else
            ()

      member this.RunTask() = 
        connectWebsocket port
        |> Task.map (fun () ->
          if _websocket.State <> WebSocketState.Open then
            ()
          else
            this.Send("response from web socket!")
        )
        |> Task.map(fun () ->
          this.Receive()
        )
      
      member this.Run() =
        this.RunTask() |> Async.AwaitTask