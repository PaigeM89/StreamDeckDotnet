module Example.EventHandling

open StreamDeckDotnet
open StreamDeckDotnet.EventBinders
open StreamDeckDotnet.Types.Received
open Thoth.Json

let sendToPIHandler (payload : JsonValue) next ctx = async {
    let msg = sprintf "In PI sendToPIHandler, JTOken is %A" payload
    let ctx' = Core.addLogToContext (msg + ", 2nd log line") ctx
    return! next ctx'
}

let keyDownHandler (payload : KeyPayload) next (ctx : EventContext) = async {
    let ctx = Core.addLogToContext (sprintf "In PI Key Down, KeyPayload is %A" payload) ctx
    return! next ctx
}

let errorHandler (err: PipelineFailure) : EventHandler =
    Core.log ($"In PI error handler, err is : {err}")

let eventPipeline : EventRoute = choose [
    SEND_TO_PROPERTY_INSPECTOR >=> log "in PI handler" >=> tryBindSendToPropertyInspectorEvent errorHandler sendToPIHandler
    KEY_DOWN >=> log "key down in client event handler" >=> tryBindKeyDownEvent errorHandler keyDownHandler
    Core.logWithContext "Unsupported event type" // >=> showAlert
]

