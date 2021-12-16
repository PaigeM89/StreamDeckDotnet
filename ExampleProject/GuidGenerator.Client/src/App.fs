module App

open System
open Fable.Core
open Browser.Types
open Browser.Dom
open StreamDeckDotnet
open StreamDeckDotnet.Fable
open GuidGenerator.SharedTypes
open GuidGenerator.Websockets
open Thoth.Json

let dprintfn fmt msg =
#if DEBUG
  printfn fmt msg
#else
  ()
#endif

let updateLastGeneratedGuid (g : Guid) =
  let ele = document.getElementById "last-generated-guid-output" :?> HTMLInputElement
  dprintfn "Setting element %A to value %A" ele g
  ele.value <- (string g)


let decipherPayload (payload : JsonValue) =
  dprintfn "raw payload in string form is %s" (string payload)
  let piSettingsResult : Result<PropertyInspectorSettings, string> =
    payload
    |> Decode.fromValue "" GuidGenerator.SharedTypes.PropertyInspectorSettings.Decoder
  match piSettingsResult with
  | Ok settings ->
    dprintfn "parsed payload result is %A" (string settings)
    updateLastGeneratedGuid settings.LastGeneratedGuid
  | Error e ->
    dprintfn "Error decoding: %A" e
    ()

let sendToPIHandler (payload : JsonValue) next ctx = async {
    let msg = sprintf "In PI sendToPIHandler, string payload is %s" (string payload)
    dprintfn "msg in send to pI handler is %A" msg
    decipherPayload payload
    return! next ctx
}

let genericEventHandler (event : Received.EventReceived) next ctx = async {
  string event |> dprintfn "received unhandled event: %A"
  let ctx = Core.addLogToContext ("Did not handle event in Guid Generator.Client but did make it to the generic handler.") ctx
  return! next ctx
}

let errorHandler (err: PipelineFailure) : EventHandler =
    Core.log (sprintf "In PI error handler, err is : %A" err)

let eventPipeline : EventRoute = choose [
    tryBindSendToPropertyInspectorEvent errorHandler sendToPIHandler
    tryBindEvent errorHandler genericEventHandler
    Core.log "Did not handle event in Guid Generator.Client"
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

    //dprintfn
    //     "Args are: inPort: %A\nInPI_UUID: %A\nregister Event: %s\ninfo: %s\n actionInfo: %s"
    //     inPort
    //     inPropertyInspectorUUID
    //     inRegisterEvent
    //     inInfo
    //     inActionInfo

    let ws = Websocket(inPort, inPropertyInspectorUUID, messageHandler)
    websocket <- Some ws
