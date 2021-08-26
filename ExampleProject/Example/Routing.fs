namespace ExampleProject

open System

open StreamDeckDotnet
open StreamDeckDotnet.EventBinders
open StreamDeckDotnet.Types.Received

open StreamDeckDotnet.Logging
open StreamDeckDotnet.Logger
open StreamDeckDotnet.Logger.Operators

module Routing =

  let logger = LogProvider.getLoggerByName("StreamDeckDotnet.Routing")

  type Timer =
  | Started of startTime : System.DateTimeOffset
  | Completed of duration : System.TimeSpan

  /// A map of the timer history for each action instance
  type Timers = Map<Guid, Timer list> // int64 list>

  /// The history of all timer values for each action instance
  let mutable timerHistory : Timers = [] |> Map.ofList

  let replace (id, values) m = Map.add id values m

  let updateTimers (id, newValues) =
    //let existing = timerHistory.TryFind id |> Option.defaultValue []
    // let newTimers = newValue :: existing
    // let m = replace (id, newTimers) timerHistory
    let m = Map.add id newValues timerHistory
    timerHistory <- m

  let keyDownHandler (keyPayload : KeyPayload) next (ctx : EventContext) = async {
    !! "In key down handler of example plugin, attempting to extract settings value" |> logger.info
    match ctx.TryGetContextGuid() with
    | Some contextId ->
      match Map.tryFind contextId timerHistory with
      | Some ((Timer.Started startDto) :: xs) ->
        let now = DateTimeOffset.UtcNow
        let diff = now - startDto
        let newValues = Completed diff :: xs
        updateTimers (contextId, newValues)
      | Some (x :: xs) ->
        let start = DateTimeOffset.UtcNow
        let newValues = Started start :: (x :: xs)
        updateTimers (contextId, newValues)
      | None
      | Some [] ->
        let start = DateTimeOffset.UtcNow
        let newValues = [Started start]
        updateTimers (contextId, newValues)
    | None -> ()
    return! next ctx
  }

  let simpleKeyDownHandler (keyPayload : KeyPayload) next (ctx : EventContext) = async {
    match ctx.TryGetContextGuid() with
    | Some contextId ->
      let ctx = ctx |> addLogToContext $"In Key Down Handler for context %O{contextId}" |> addShowOk
      return! next ctx
    | None ->
      ctx.AddLog($"In key down handler, no context was found")
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
    KEY_UP >=> Core.log "In KEY_UP handler"
    // This will flood the logs
    Core.logWithContext "Unsupported event type" // >=> showAlert
  ]
