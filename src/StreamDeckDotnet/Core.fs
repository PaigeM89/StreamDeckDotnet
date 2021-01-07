namespace StreamDeckDotnet


module Core =
  open Microsoft.Extensions.Logging
  open Context
  open Events
  open Thoth.Json.Net
  open FsToolkit.ErrorHandling

  // all actions can possibly result in an event being sent.
  type ActionFuncResult = Async<Context.ActionContext option>

  type ActionFunc = Context.ActionContext -> ActionFuncResult

  type ActionHandler = ActionFunc -> ActionFunc

  type ErrorHandler = exn -> ILogger -> ActionHandler

  let skipPipeline : ActionFuncResult = Async.lift None
  let earlyReturn : ActionFunc = Some >> Async.lift

  let compose (action1 : ActionHandler) (action2 : ActionHandler) : ActionHandler =
    fun final -> final |> action2 |> action1

  let (>=>) = compose

  let rec private chooseActionFunc (funcs : ActionFunc list) : ActionFunc =
    fun (ctx : Context.ActionContext) ->
      async {
        match funcs with
        | [] -> return None
        | func :: tail ->
          let! result = func ctx
          match result with
          | Some c -> return Some c
          | None -> return! chooseActionFunc tail ctx
      }

  let choose (handlers : ActionHandler list) : ActionHandler = 
    fun (next : ActionFunc) ->
      let funcs = handlers |> List.map (fun h -> h next)
      fun (ctx : Context.ActionContext) -> chooseActionFunc funcs ctx

  let tryDecode decoder targetType payload =
    result {
      let! payload = Decode.fromString decoder payload
      return targetType payload
    }

  let tryBindEvent (errorHandler : ActionFailure -> ActionHandler) (successHandler : EventReceived -> ActionHandler) : ActionHandler =
    fun (next : ActionFunc) (ctx: ActionContext) -> async {
      let! result = ctx.TryBindEventAsync
      match result with
      | Ok event -> return! successHandler event next ctx
      | Error err -> return! errorHandler err next ctx
    }

  let actionReceived (validate: string -> bool) : ActionHandler =
    fun (next : ActionFunc) (ctx : ActionContext) ->
      if validate ctx.ActionReceived.Event
      then next ctx
      else skipPipeline

  let validateAction (s : string) (t : string) = 
    s.ToLowerInvariant() = t.ToLowerInvariant()

  //these structures are in Giraffe but i don't know what they do
  let KEY_DOWN : ActionHandler = actionReceived (validateAction EventNames.KeyDown)
  let SYSTEM_WAKE_UP : ActionHandler = actionReceived (validateAction EventNames.SystemDidWakeUp)
  let KEY_UP : ActionHandler = actionReceived (validateAction EventNames.KeyDown)

  let addLog (msg : string) (ctx : ActionContext) =
    let log = Events.createLogEvent msg
    Context.addSendEvent log ctx

  let log (msg : string) : ActionHandler =
    fun (next : ActionFunc) (ctx : ActionContext) ->
      addLog msg ctx
      next ctx

  let flow (_ : ActionFunc) (ctx: ActionContext) = Context.flow ctx

module ActionRouting = 
  open Core
  open Context

  //todo: state router

  let action (eventName : string) : ActionHandler =
    fun (next : ActionFunc) (ctx : Context.ActionContext) ->
      let validate = Core.validateAction eventName
      Core.actionReceived validate next ctx
  
  let actionWithBinding (eventName : string, eventType : System.Type) : ActionHandler =
    fun (next : ActionFunc) (ctx : Context.ActionContext) ->
      let validate = Core.validateAction eventName
      ctx.SetEventType(eventType)
      Core.actionReceived validate next ctx
  
  let actionForType (eventName : string, eventType : System.Type) : ActionHandler =
    fun (next : ActionFunc) (ctx : Context.ActionContext) ->
      let validate = Core.validateAction eventName
      ctx.SetEventType(eventType)
      Core.actionReceived validate next ctx

  let tryBindToKeyPayload decodingErrorHandler bindingErrorHandler successHandler =
    fun next ctx -> 
      let validatePayload e =
        match e with
        | Events.KeyUp payload -> successHandler payload
        | Events.KeyDown payload -> successHandler payload
        | _ -> bindingErrorHandler e next ctx
      tryBindEvent decodingErrorHandler validatePayload next ctx

  let keyUpAction 
        (interimPipeline : (ActionFunc -> Context.ActionContext -> ActionFuncResult) option)
        decodingErrorHandler
        bindingErrorHandler
        successHandler : ActionHandler =
    match interimPipeline with
    | Some interim ->
        action Events.EventNames.KeyUp >=> interim >=> tryBindToKeyPayload decodingErrorHandler bindingErrorHandler successHandler
    | None -> 
        action Events.EventNames.KeyUp >=> tryBindToKeyPayload decodingErrorHandler bindingErrorHandler successHandler


  let tryBindKeyDownEvent (errorHandler : Context.ActionFailure -> ActionHandler) (successHandler : Types.Received.KeyPayload -> ActionHandler) =
    fun next (ctx : ActionContext) ->
      let filter (e : Events.EventReceived)  = 
        match e with
        | Events.EventReceived.KeyDown payload -> successHandler payload
        | _ -> errorHandler Context.ActionFailure.Placeholder
      tryBindEvent errorHandler filter next ctx
