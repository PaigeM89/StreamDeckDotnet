module App

open StreamDeckDotnet
open StreamDeckDotnet.Routing
open StreamDeckDotnet.EventBinders
open StreamDeckDotnet.Fable
open GuidGenerator.Websockets
open Thoth.Json


let sendToPIHandler (payload : JsonValue) next ctx = async {
    let msg = sprintf "In PI sendToPIHandler, JTOken is %A" payload
    printfn "msg in send to pI handler is %A" msg
    let ctx' = Core.addLogToContext (msg + ", 2nd log line") ctx
    return! next ctx'
}


let errorHandler (err: PipelineFailure) : EventHandler =
    Core.log (sprintf "In PI error handler, err is : %A" err)

let eventPipeline : EventRoute = choose [
    SEND_TO_PROPERTY_INSPECTOR >=> log "in PI handler" >=> tryBindSendToPropertyInspectorEvent errorHandler sendToPIHandler
]

let mutable websocket : Websocket option = None

let messageHandler msg : Async<Result<StreamDeckDotnet.Context.EventContext, string>> =
    socketMsgHandler eventPipeline msg

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
