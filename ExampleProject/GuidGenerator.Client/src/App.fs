module App

open System
open Fable.Core
open Browser.Types
open Browser.Dom
open StreamDeckDotnet
// open StreamDeckDotnet.Routing
// open StreamDeckDotnet.EventBinders
open StreamDeckDotnet.Fable
open GuidGenerator.SharedTypes
open GuidGenerator.Websockets
open Thoth.Json

(*
  todo: the updated application doesn't seem to be getting bundled to the stream deck package
*)

let updateLastGeneratedGuid (g : Guid) =
  let ele = document.getElementById "last-generated-guid-output" :?> HTMLInputElement
  printfn "Setting element %A to value %A" ele g
  ele.value <- (string g)


let decipherPayload (payload : JsonValue) =
  printfn "raw payload in string form is %s" (string payload)
  let piSettingsResult : Result<PropertyInspectorSettings, string> =
    payload
    |> Decode.fromValue "" GuidGenerator.SharedTypes.PropertyInspectorSettings.Decoder
  match piSettingsResult with
  | Ok settings ->
    printfn "parsed payload result is %A" (string settings)
    updateLastGeneratedGuid settings.LastGeneratedGuid
  | Error e ->
    printfn "Error decoding: %A" e
    ()

let sendToPIHandler (payload : JsonValue) next ctx = async {
    let msg = sprintf "In PI sendToPIHandler, string payload is %s" (string payload)
    printfn "msg in send to pI handler is %A" msg
    decipherPayload payload
    return! next ctx
}

let genericEventHandler (event : Received.EventReceived) next ctx = async {
  string event |> printfn "received unhandled event: %A"
  let ctx = Core.addLogToContext ("Did not handle event in Guid Generator.Client but did make it to the generic handler.") ctx
  return! next ctx
}

let errorHandler (err: PipelineFailure) : EventHandler =
    printfn "Error handling event: %A" (string err)
    Core.log (sprintf "In PI error handler, err is : %A" err)

let eventPipeline : EventRoute = choose [
    //SEND_TO_PROPERTY_INSPECTOR >=>
    tryBindSendToPropertyInspectorEvent errorHandler sendToPIHandler
    tryBindEvent errorHandler genericEventHandler
    Core.log "Did not handle event in Guid Generator.Client"
]

let mutable websocket : Websocket option = None

let messageHandler msg : Async<Result<StreamDeckDotnet.Context.EventContext, string>> =
    printfn "In web socket message handler for msg %s" msg
    socketMsgHandler eventPipeline msg

let connectStreamDeck
        (inPort : int)
        (inPropertyInspectorUUID : System.Guid)
        (inRegisterEvent : string)
        (inInfo: string)
        (inActionInfo : string) =

    printfn "Initializing..."

    printfn
        "Args are: inPort: %A\nInPI_UUID: %A\nregister Event: %s\ninfo: %s\n actionInfo: %s"
        inPort
        inPropertyInspectorUUID
        inRegisterEvent
        inInfo
        inActionInfo

    printfn "Creating web socket..."

    let ws = Websocket(inPort, inPropertyInspectorUUID, messageHandler)
    websocket <- Some ws
