module GuidGenerator.Websockets

open Fable.Core
open System
open Browser
open Browser.Types
open Fable.SimpleJson

let getWebsocketServerUrl (port: int) =
    sprintf "ws://127.0.0.1:%i" port

let getRegisterWebsocket (uuid : Guid) =
  let json = {|
    event = "registerPropertyInspector"
    uuid = uuid
  |}
  Json.stringify(json)

type Websocket(port : int, uuid: System.Guid, messageHandler : string -> Async<Result<StreamDeckDotnet.Context.EventContext, string>>) =
    let mutable msgQueue : string list = []
    let wsref : WebSocket option ref = ref None

    let createWebsocket () =
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
                    printfn "Socket was opened, on open being called! Event Target: %A" e.currentTarget
                    let registerPayload = getRegisterWebsocket uuid
                    msgQueue <- registerPayload :: msgQueue
                    printfn "MsgQueue is %A" msgQueue

                    msgQueue |> List.rev |> List.iter socket.send
                socket.onclose <- fun _ ->
                    printfn "Socket was closed!"
                    Dom.window.setTimeout
                        ((fun () -> connect timeout server), timeout, ()) |> ignore
                socket.onmessage <- fun e ->
                    printfn "socket.onmessage was called. raw data is %A" (string e.data)
                    let msg = string e.data
                    printfn "websocket msg is %A" msg
                    messageHandler msg
                    |> Async.StartAsPromise
                    |> Promise.map (fun ctx ->
                        match ctx with
                        | Ok ctx ->
                            let events = ctx.GetEncodedEventsToSend()
                            printfn "Got context back, sending %i events" (List.length events)
                            events |> List.iter socket.send
                        | Error e ->
                            printfn "Error handling message: %A" e
                    )
                    |> Promise.start
        connect (60000) (getWebsocketServerUrl port)
        printfn "Websocket finished connect(), returning out of constructor"
        match !wsref with
        | Some ws ->
            printfn "Socket state is %A" ws.readyState
        | None -> printfn "socket is none"

    do createWebsocket()

    member this.IsOpen () =
        printfn "in IsOpen func"
        match !wsref with
        | Some ws -> ws.readyState = WebSocketState.OPEN
        | None -> false

    member this.SendToSocket (payload: string) =
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
