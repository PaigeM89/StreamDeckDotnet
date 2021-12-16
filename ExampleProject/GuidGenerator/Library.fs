module GuidGenerator.Program

open System
open StreamDeckDotnet
open StreamDeckDotnet.Types.Sent
open StreamDeckDotnet.EventBinders
open Thoth.Json.Net
open TextCopy

(*
  Todo: store the last guid in something so that it appears when the user _selects_ the plugin,
        not just has the plugin already open in the PI.
*)

/// Add a Property Inspector event so we can update the Property Inspector with the newly
/// generated GUID.
let addPIEvent (g : Guid) (ctx : EventContext) =
  let piSettings = GuidGenerator.SharedTypes.PropertyInspectorSettings.Create g
  let event = piSettings.Encode() |> SendToPropertyInspector
  ctx.AddSendEvent event

/// Handle the KEY_DOWN action. Generates a random GUID and adds it to the clip board.
let keyDownHandler keyPayload next (ctx : EventContext) =  async {
  let guid = Guid.NewGuid()
  ClipboardService.SetText(string guid)
  addPIEvent guid ctx
  return! next ctx
}

/// Generic error handler that sends logs to the Stream Deck Application
let errorHandler (err : PipelineFailure) : EventHandler =
  Core.log($"Error handling event: %A{err}")

/// The routes we'll handle - we only care about one event.
let routes : EventRoute = choose [
  KEY_DOWN >=> tryBindKeyDownEvent errorHandler keyDownHandler >=> showOk
]

[<EntryPoint>]
let main argv =
  // parse out streamdeck args
  let args = ArgsParsing.parseArgs argv

  // create the client using the args we parsed & the routes we created
  let client = StreamDeckClient(args, routes)
  try
    // start the client
    client.Run()
  with
  | e ->
    printfn "Error: %A" e
  0
