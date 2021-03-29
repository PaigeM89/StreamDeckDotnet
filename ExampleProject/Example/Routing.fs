namespace ExampleProject

open System

open StreamDeckDotnet
open StreamDeckDotnet.Types.Received

open StreamDeckDotnet.Logging
open StreamDeckDotnet.Logger
open StreamDeckDotnet.Logger.Operators

module Routing =

  let logger = LogProvider.getLoggerByName("StreamDeckDotnet.Routing")

  /// A map of the timer history for each action instance
  type Timers = Map<Guid, int64 list> 

  /// The history of all timer values for each action instance
  let mutable timerHistory : Timers = [] |> Map.ofList

  let replace (id, values) m =
    Map.remove id m
    |> Map.add id values

  let updateTimers (id, newValue) =
    let existing = timerHistory.TryFind id |> Option.defaultValue []
    let newTimers = newValue :: existing
    let m = replace (id, newTimers) timerHistory
    timerHistory <- m

  let keyDownHandler (keyPayload : KeyPayload) next (ctx : EventContext) = async {
    //todo: add type, deserialize Settings to that type
    let duration : obj = keyPayload.Settings.Value("duration")
    match ctx.TryGetContextGuid()with
    | Some contextId ->
      match duration with
      | :? int64 as dur ->
        ctx.AddLog $"Received timer duration of %A{dur} for context %A{contextId}"
        updateTimers (contextId, dur)
        ctx.AddOk()
        return! next ctx
      | _ ->
        ctx.AddLog $"Received timer duration of %A{duration} for context %A{contextId} but was unable to cast it to an int64."
        ctx.AddAlert()
        return! next ctx
    | None ->
      ctx.AddLog $"Received timer duration of %A{duration} but with unparsable or missing context id. Metadata: %A{ctx.EventMetadata}"
      ctx.AddAlert()
      return! next ctx
  }

  let myAction (event : EventReceived) (next: EventFunc) (ctx : EventContext) = async {
    !! "in My Action handler, with event {event}" >>!+ ("event", event) |> logger.info
    let ctx' = Core.addLogToContext $"in My Action handler, with event {event}" ctx
    return! next ctx'
  }

  let appearHandler (event : EventReceived) next ctx = async {
    !! "In Will Appear handler, with event {event}" >>!+ ("event", event) |> logger.info
    let ctx' = Core.addLogToContext $"In appear handler, with event {event}" ctx
    return! next ctx'
  }

  let errorHandler (err : PipelineFailure) : EventHandler =
    !! "in error handler, with pipeline failure {err}" >>!+ ("err", err) |> logger.info
    Core.log ($"in error handler, error: {err}")

  let routes : EventRoute = choose [
    KEY_DOWN >=> Core.log "in KEY_DOWN handler" >=> tryBindKeyDownEvent errorHandler keyDownHandler
    WILL_APPEAR >=> Core.log "in WILL_APPEAR handler" >=> tryBindEvent errorHandler appearHandler
    // This will flood the logs
    Core.logWithContext "Unsupported event type" >=> showAlert
  ]