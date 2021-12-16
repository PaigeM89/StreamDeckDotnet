module App

open StreamDeckDotnet
open StreamDeckDotnet.Fable
open Example
open Example.Websockets

let mutable websocket : Websocket option = None

let messageHandler msg : Async<Result<StreamDeckDotnet.Context.EventContext, string>> =
    let routes : StreamDeckDotnet.Routing.EventRoute = EventHandling.eventPipeline
    socketMsgHandler routes msg


let connectStreamDeck
        (inPort : int)
        (inPropertyInspectorUUID : System.Guid)
        (inRegisterEvent : string)
        (inInfo: string)
        (inActionInfo : string) =

    printfn
        "Args are: inPort: %A\nInPI_UUID: %A\nregister Event: %s\ninfo: %s\n actionInfo: %s"
        inPort
        inPropertyInspectorUUID
        inRegisterEvent
        inInfo
        inActionInfo

    let ws = Websocket(inPort, inPropertyInspectorUUID, messageHandler)
    websocket <- Some ws
