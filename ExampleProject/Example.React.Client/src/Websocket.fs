namespace Example.React.Client

module Websockets =
  open System
  open Browser
  open Browser.Types
  open Fable.Core
  open Fable.SimpleJson
  // open Fable.Browser.Dom
  // open Fable.Browser.WebSocket

  let getWebsocketServerUrl (port: int) =
    sprintf "ws://localhost:%i" port

  //https://github.com/fable-compiler/fable-browser/blob/master/src/WebSocket/Browser.WebSocket.fs
  
  // messageHandler : string -> unit
  type Websocket(port : int, uuid: System.Guid, messageHandler : string -> unit) =
    let mutable msgQueue : string list = []
    let wsref : WebSocket option ref = ref None
    
    let createWebsocket() =
      let rec connect timeout server =
        printfn "attempting to connect web socket to %s with timeout %i..." server timeout
        match !wsref with
        | Some _ -> ()
        | None ->
          let socket = WebSocket.Create server
          wsref := Some socket
          // we send our registration event after opening the socket without using the OnOpen event
          socket.onerror <- fun _ ->
            printfn "Socket had error!"
          socket.onopen <- fun e ->
            printfn "Socket was opened, on open being called! %A" e
            printfn "MsgQueue is %A" msgQueue
            msgQueue |> List.rev |> List.iter socket.send
          socket.onclose <- fun _ ->
            printfn "Socket was closed!"
            Dom.window.setTimeout
                ((fun () -> connect timeout server), timeout, ()) |> ignore
          socket.onmessage <- fun e ->
            printfn "socket.onmessage was called"
            Json.tryParseNativeAs(string e.data)
            |> function
              | Ok msg ->
                printfn "websocket msg is %A" msg
                messageHandler msg
              | _ ->
                printfn "could not parse message %A" e
      connect (60000) (getWebsocketServerUrl port)
      printfn "Websocket finished connect(), returning out of constructor"
      match !wsref with
      | Some ws -> 
          printfn "Socket state is %A" ws.readyState
      | None -> printfn "socket is none"

    do createWebsocket()

    member this.IsOpen() =
      printfn "in IsOpen func"
      match !wsref with
      | Some ws -> ws.readyState = WebSocketState.OPEN
      | None -> false

    member this.SendToSocket(payload: string) = 
      if this.IsOpen() then
        match !wsref with
        | Some ws ->
          let payload = Json.stringify payload
          printfn "websocket sending \"%s\"" payload
          ws.send payload
        | None -> ()
      else
        let payload = Json.stringify payload
        msgQueue <- payload :: msgQueue