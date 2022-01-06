module GuidGenerator.Program

open System
open StreamDeckDotnet
open StreamDeckDotnet.Types.Sent
open StreamDeckDotnet.EventBinders
open Thoth.Json.Net
open TextCopy

open Serilog
open Serilog.Context
open Serilog.Sinks.File

/// Store the last guid we generated so we can fetch it when the PI appears
let mutable lastGuid = Guid.Empty

/// Add a Property Inspector event so we can update the Property Inspector with the newly
/// generated GUID. This will only be handled by the PI while it's currently open in the Stream Deck Application.
let addPIEvent (g : Guid) (ctx : EventContext) =
  let piSettings = GuidGenerator.SharedTypes.PropertyInspectorSettings.Create g
  let event = piSettings.Encode() |> SendToPropertyInspector
  ctx.AddSendEvent event

/// Handle the KEY_DOWN action. Generates a random GUID and adds it to the clip board.
let keyDownHandler keyPayload next (ctx : EventContext) =  async {
  lastGuid <- Guid.NewGuid()
  ClipboardService.SetText(string lastGuid)
  addPIEvent lastGuid ctx
  return! next ctx
}

/// When the property inspector appears, we need to send the last generated guid
let propertyInspectorDidAppearHandler () next (ctx : EventContext) = async {
  addPIEvent lastGuid ctx
  return! next ctx
}

/// Generic error handler that sends logs to the Stream Deck Application
let errorHandler (err : PipelineFailure) : EventHandler =
  Core.log($"Error handling event: %A{err}")

/// The routes we'll handle
let routes : EventRoute = choose [
  // When the user hits the stream deck button, generate a guid on the clip board.
  KEY_DOWN >=> tryBindKeyDownEvent errorHandler keyDownHandler >=> showOk
  // When the user opens up the property inspector, we want to display the last generated guid.
  PROPERTY_INSPECTOR_DID_APPEAR >=> tryBindPropertyInspectorDidAppearEvent errorHandler propertyInspectorDidAppearHandler
]

[<EntryPoint>]
let main argv =
  let log =
    LoggerConfiguration()
      .MinimumLevel.Verbose()
      .WriteTo.File("log.txt", rollingInterval = RollingInterval.Day)
      .Enrich.FromLogContext()
      .CreateLogger()
  Log.Logger <- log

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
