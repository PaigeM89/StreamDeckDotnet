namespace Example.Client

open StreamDeckDotnet
open StreamDeckDotnet.Received
open Newtonsoft.Json
open Newtonsoft.Json.Linq

module EventHandling = 

    let decodeString (s : string) = "not working yet"

    let sendToPIHandler (payload : JToken) next ctx = async {
        let msg = sprintf "In PI sendToPIHandler, JTOken is %A" payload
        Core.log (msg)
        let ctx' = Core.addLogToContext (msg + ", 2nd log line")
        return! next ctx'
    }

    let keyDownHandler (payload : KeyPayload) next ctx = async {
        Core.log (sprintf "In PI Key Down, KeyPayload is %A" keyPayload)
        return! next ctx
    }

    let errorHandler (err: PipelineFailure) : EventHandler =
        Core.log ($"In PI error handler, err is : {err}")

    let eventPipeline = choose [
        SEND_TO_PROPERTY_INSPECTOR >=> log "in PI handler" >=> tryBindSendToPropertyInspectorEvent errorHandler sendToPIHandler
        KEY_DOWN >=> log "key down in client event handler" >=> tryBindKeyDownEvent errorHandler
        Core.logWithContext "Unsupported event type" >=> showAlert
    ]
