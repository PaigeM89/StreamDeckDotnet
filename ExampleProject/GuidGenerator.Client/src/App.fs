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

let updateLastGeneratedGuid (g : Guid) =
  let ele = document.getElementById "last-generated-guid-output" :?> HTMLInputElement
  ele.value <- (string g)

let decipherPayload (payload : JsonValue) =
  let piSettingsResult : Result<PropertyInspectorSettings, string> =
    payload
    |> Decode.fromValue "$" GuidGenerator.SharedTypes.PropertyInspectorSettings.Decoder
  match piSettingsResult with
  | Ok settings ->
    printfn "Got custom property inspector settings: %A" settings
    updateLastGeneratedGuid settings.LastGeneratedGuid
  | Error e ->
    printfn "Error decoding: %A" e
    ()

/// When the PI is open, and the button is pressed, this will handle the "send to PI"
/// event and update the last generated guid field.
let sendToPIHandler (payload : JsonValue) next ctx = async {
  printfn "in send to PI handler"
  decipherPayload payload
  return! next ctx
}

let errorHandler (err: PipelineFailure) : EventHandler =
    printfn "In PI error handler, err is %A" err
    // i think this line will cause an error in the stream deck
    Core.log (sprintf "In PI error handler, err is : %A" err)

/// We use `compose` here because there is currently a bug when using the `>=>` operator that causes
/// the next function in the pipeline to not execute.
let eventPipeline : EventRoute = choose [
    compose SEND_TO_PROPERTY_INSPECTOR (tryBindSendToPropertyInspectorEvent errorHandler sendToPIHandler)
    // as mentioned above, the >=> operator is bugged in Fable
    // SEND_TO_PROPERTY_INSPECTOR >=> tryBindSendToPropertyInspectorEvent errorHandler sendToPIHandler
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

    //printfn
    //     "Args are: inPort: %A\nInPI_UUID: %A\nregister Event: %s\ninfo: %s\n actionInfo: %s"
    //     inPort
    //     inPropertyInspectorUUID
    //     inRegisterEvent
    //     inInfo
    //     inActionInfo

    let ws = Websocket(inPort, inPropertyInspectorUUID, messageHandler)
    websocket <- Some ws
