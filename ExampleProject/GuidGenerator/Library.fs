module GuidGenerator.Program

open System
open StreamDeckDotnet
open StreamDeckDotnet.Types.Sent
open StreamDeckDotnet.EventBinders
open Thoth.Json.Net
open TextCopy

let addPIEvent (g : Guid) (ctx : EventContext) =
  let piSettings = GuidGenerator.SharedTypes.PropertyInspectorSettings.Create g
  let event = piSettings.Encode() |> SendToPropertyInspector
  ctx.AddSendEvent event

let keyDownHandler keyPayload next (ctx : EventContext) =  async {
  let guid = Guid.NewGuid()
  ClipboardService.SetText(string guid)
  addPIEvent guid ctx
  return! next ctx
}

let errorHandler (err : PipelineFailure) : EventHandler =
  Core.log($"Error handling event: %A{err}")

let routes : EventRoute = choose [
  KEY_DOWN >=> tryBindKeyDownEvent errorHandler keyDownHandler >=> showOk
]

[<EntryPoint>]
let main argv =
  let args = ArgsParsing.parseArgs argv

  let client = StreamDeckClient(args, routes)
  try
    client.Run()
  with
  | e ->
    printfn "Error: %A" e
  0
