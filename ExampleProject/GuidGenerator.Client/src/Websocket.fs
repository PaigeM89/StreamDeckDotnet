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
  Json.serialize(json)

type MessageHandler = string -> Async<Result<StreamDeckDotnet.Context.EventContext, string>>

let handleMessage (messageHandler : MessageHandler) responseHandler input = async {
  let! ctxResponse = messageHandler input
  match ctxResponse with
  | Ok ctx ->
    let events = ctx.GetEncodedEventsToSend()
    events |> List.iter responseHandler
  | Error e ->
    return ()
}

type Websocket(port : int, uuid: System.Guid, messageHandler : MessageHandler) =
    let mutable msgQueue : string list = []
    let wsref : WebSocket option ref = ref None

    let createWebsocket () =
        let rec connect timeout server =
            match wsref.Value with
            | Some _ -> ()
            | None ->
                let socket = WebSocket.Create server
                wsref.Value <- Some socket
                socket.onerror <- fun e ->
                    printfn "Socket had error: %A" e
                socket.onopen <- fun e ->
                    printfn "Socket was opened, on open being called! Event Target: %A" (string e.currentTarget)
                    // as soon as the socket is opened, send the register event
                    let registerPayload = getRegisterWebsocket uuid
                    msgQueue <- registerPayload :: msgQueue
                    printfn "Sending registration message"
                    msgQueue |> List.rev |> List.iter socket.send
                socket.onclose <- fun _ ->
                    printfn "Socket was closed!"
                    Dom.window.setTimeout
                        ((fun () -> connect timeout server), timeout, ()) |> ignore
                socket.onmessage <- fun e ->
                    let msg = string e.data
                    printfn "raw message from socket is %s" msg
                    handleMessage messageHandler socket.send msg
                    |> Async.StartAsPromise
                    |> Promise.start // not really needed but whatever

        connect (60000) (getWebsocketServerUrl port)

    do createWebsocket()

    member this.IsOpen () =
        match wsref.Value with
        | Some ws -> ws.readyState = WebSocketState.OPEN
        | None -> false

    member this.SendToSocket (payload: string) =
        if this.IsOpen() then
            match wsref.Value with
            | Some ws ->
                let payload = Json.serialize payload
                printfn "websocket sending \"%s\"" payload
                ws.send payload
            | None -> ()
        else
            let payload = Json.serialize payload
            msgQueue <- payload :: msgQueue
