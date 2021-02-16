namespace StreamDeckDotnet


module Core =
  open Microsoft.Extensions.Logging
  open Context
  open Types
  open Thoth.Json.Net
  open FsToolkit.ErrorHandling

  /// The result of an EventContext being processed through the event pipeline.
  /// If the result is Some, then the handler will finish processing the event and exit.
  /// If it is None, the handler will continue to find the next pipeline
  /// to handle the event.
  type EventFuncResult = Async<Context.EventContext option>

  /// A function that accepts a <see cref="Context.EventContext" /> and returns an <see cref="EventFuncResult" />
  /// This function may modify the EventContext. If the result is Some, then the handler will consider the event
  /// processed and exit. If it is None, the handler will continue to find the next pipeline to handle the event.
  type EventFunc = Context.EventContext -> EventFuncResult

  /// The core building block of event handling, the EventHandler will either invoke the next
  /// <see cref="EventFunc" /> or exit the pipeline early.
  type EventHandler = EventFunc -> EventFunc

  /// Takes a <see cref="System.Exception" /> object and an <see cref="Microsoft.Extensions.Logging.ILogger"/> instance
  /// to handle any uncaught errors. Returns an <see cref="EventHandler" />.
  type ErrorHandler = exn -> ILogger -> EventHandler

  /// Short circuit the pipeline and return None, exiting that pipeline's processing.
  let skipPipeline : EventFuncResult = Async.lift None

  /// Stop evaluating the rest of the pipeline and return Some, causing the event to be processed.
  let earlyReturn : EventFunc = Some >> Async.lift

  /// Combines two <see cref="EventHandler" /> functions into a single function.
  /// Consider using the `>=>` operator as an easier alternative to this function.
  let compose (action1 : EventHandler) (action2 : EventHandler) : EventHandler =
    fun final -> final |> action2 |> action1

  /// Combines two <see cref="EventHandler" /> functions into a single function.
  let (>=>) = compose

  let rec private chooseEventFunc (funcs : EventFunc list) : EventFunc =
    fun (ctx : Context.EventContext) ->
      async {
        match funcs with
        | [] -> return None
        | func :: tail ->
          let! result = func ctx
          match result with
          | Some c -> return Some c
          | None -> return! chooseEventFunc tail ctx
      }

  /// Iterates through a list of <see cref="EventHandler" /> functions and returns the first <see cref="EventFuncResult" />
  /// of which the option is Some.
  let choose (handlers : EventHandler list) : EventHandler = 
    fun (next : EventFunc) ->
      let funcs = handlers |> List.map (fun h -> h next)
      fun (ctx : Context.EventContext) -> chooseEventFunc funcs ctx

  let tryDecode decoder targetType payload =
    result {
      let! payload = Decode.fromString decoder payload
      return targetType payload
    }

  /// Attempts to bind the payload in an <see cref="EventContext" /> to the <see cref="Received.EventReceived" /> type given,
  /// and then process that event to return a <see cref="EventHandler" />. If the event fails to bind, then the
  /// `errorHandler` will process the error and return the <see cref="EventHandler" />.
  let tryBindEvent (errorHandler : PipelineFailure -> EventHandler) (successHandler : Received.EventReceived -> EventHandler) : EventHandler =
    fun (next : EventFunc) (ctx: EventContext) -> async {
      let! result = ctx.TryBindEventAsync
      match result with
      | Ok event -> return! successHandler event next ctx
      | Error err -> return! errorHandler err next ctx
    }

  /// Validates that the Event name in the <see cref="EventContext" /> passes the given `validate` function.
  /// If successful, the pipeline evaluation will continue.
  /// If not successful, the pipeline is skipped via `skipPipeline`.
  let validateEvent (validate: string -> bool) : EventHandler =
    fun (next : EventFunc) (ctx : EventContext) ->
      let x = ctx.EventMetadata.Event
      if validate x
      then next ctx
      else skipPipeline

  /// Validates the two given strings (assumed to be event names) to be equal, ignoring case.
  let validateAction (s : string) (t : string) =
    s.ToLowerInvariant() = t.ToLowerInvariant()

  /// Validates that the action in the EventContext is a "keyDown" action.
  let KEY_DOWN : EventHandler = validateEvent (validateAction EventNames.KeyDown)
  /// Validates that the action in the EventContext is a "systemDidWakeUp" action.
  let SYSTEM_WAKE_UP : EventHandler = validateEvent (validateAction EventNames.SystemDidWakeUp)
  /// Validates that the action in the EventContext is a "keyDown" action.
  let KEY_UP : EventHandler = validateEvent (validateAction EventNames.KeyDown)

  /// Adds the passed message to the Logs in the Context and returns the Context.
  /// Note that the context is the same object and is only returned for chaining.
  let addLog (msg : string) (ctx : EventContext) =
    let log = createLogEvent msg
    Context.addSendEvent log ctx

  let log (msg : string) : EventHandler =
    fun (next : EventFunc) (ctx : EventContext) ->
      addLog msg ctx
      |> next

  /// Flow the event handler to the next function, ignoring the passed function
  let flow (_ : EventFunc) (ctx: EventContext) = Context.flow ctx
