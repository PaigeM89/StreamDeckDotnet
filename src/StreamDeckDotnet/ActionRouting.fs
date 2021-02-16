namespace StreamDeckDotnet

open StreamDeckDotnet.Logging
open StreamDeckDotnet.Logger
open StreamDeckDotnet.Logger.Operators

module ActionRouting = 
  open Core
  open Context
  open Types

  type EventRoute = EventFunc -> EventContext -> EventFuncResult

  /// Routing that is only available once we have determined the payload type
  module PayloadRouting = 
    let multiAction() = true

    //todo: state router
    let inline contextState (stateCheck : int -> 'a -> bool) =
      fun next ctx ->
        next ctx


  /// Accepts a function that takes the action context and validates if the action route is valid.
  let appState (stateCheck: EventContext -> bool) =
    fun next ctx ->
      next ctx

  let matcher matchFunc =
    let logErrorHandler err =
      Core.log $"Error handling event: {err}"
    fun next (ctx: EventContext) ->
      tryBindEvent logErrorHandler (matchFunc ctx) next ctx


  let eventMatch (eventName : string) : EventHandler =
    fun (next : EventFunc) (ctx : Context.EventContext) ->
      let validate = Core.validateAction eventName
      Core.validateEvent validate next ctx


  let tryBindToKeyPayload decodingErrorHandler bindingErrorHandler successHandler =
    fun next ctx -> 
      let validatePayload e =
        match e with
        | Received.KeyUp payload -> successHandler payload
        | Received.KeyDown payload -> successHandler payload
        | _ -> bindingErrorHandler e next ctx
      tryBindEvent decodingErrorHandler validatePayload next ctx

  let tryBindKeyDownEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.KeyPayload -> EventHandler) =
    fun next (ctx : EventContext) ->
      let filter (e : Received.EventReceived)  = 
        match e with
        | Received.EventReceived.KeyDown payload -> successHandler payload
        | _ -> errorHandler (Context.PipelineFailure.WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEvent errorHandler filter next ctx

  let tryBindKeyDownEventPipeline (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.KeyPayload -> EventHandler) =
    fun next (ctx : EventContext) ->
      let filter (e : Received.EventReceived)  = 
        match e with
        | Received.EventReceived.KeyDown payload -> successHandler payload
        | _ -> errorHandler Context.PipelineFailure.Placeholder
      tryBindEvent errorHandler filter next ctx

module Engine =
  //open MessageRoutingBuilder
  open FsToolkit.ErrorHandling
  open Context
  open Core

  let logger = LogProvider.getLoggerByName("StreamDeckDotnet.Engine")

  // type ActionDelegate = delegate of EventContext -> Async<unit>

  // type ActionMiddleware(next : ActionDelegate, handler: EventHandler) =
  //   let func = handler earlyReturn

  //   member this.Invoke (ctx : EventContext) =
  //     async {
  //       let! result = func ctx
  //       if result.IsNone then return! next.Invoke ctx
  //     }

  type RequestHandler = EventFunc -> EventContext -> EventFuncResult

  // let mapActions (routes : ActionRoute list) =
  //   routes
  //   |> List.iter(fun r ->
  //     match r with
  //     | SimpleRoute (r, n, a) -> MessageRoutingBuilder.mapSingleAction (r, n, a)
  //     | MultiRoutes (routes) -> mapMultiAction routes
  //   )

  let evaluateStep (handler : RequestHandler) next (ctx : EventContext) : EventFuncResult =
    async {
      match! next ctx with
      | Some ctx -> return! handler next ctx
      | None -> return! skipPipeline
    }
  
  let inspectroute handler next ctx = 
    let eval = next ctx |> Async.RunSynchronously
    match eval with
    | Some ctx ->
      handler next ctx
    | None -> skipPipeline


  // let buildActionRoutes (rootHandler : RequestHandler) (ctx : ActionContext) =
  //   let func : EventFunc = rootHandler earlyReturn
  //   let t = 
  //     async {
  //       let! result = func ctx
  //       if result.IsNone then return! next.Invoke(ctx)
  //     }
  //   //let buildActionRoute (func: EventFunc, ctx : ActionContext, funcResult : EventFuncResult) =
      
  //   ()

  // let matchActionContextToHandler (ctx : Context.ActionContext) (routes: ActionRoute list) = 
  //   ()

  let handleSocketRegister (args : Websockets.StreamDeckSocketArgs) () =
    let register = Types.Sent.RegisterPluginPayload.Create args.RegisterEvent args.PluginUUID
    !! "Creating registration event of {event}"
    >>!+ ("event", register)
    |> logger.info
    let sendEvent = Types.Sent.EventSent.RegisterPlugin register
    sendEvent.Encode None None //context or device not needed for this event.

  // handles the raw json message from the web socket
  let socketMsgHandlerR (msg : string) (routes: Core.EventHandler) = asyncResult {
    //first decode into an EventMetadata
    !! "Decoding event metadata from msg {msg}"
    >>!- ("msg", msg)
    |> logger.info
    let! eventMetadata = Types.decodeEventMetadata msg
    !! "Building context from metadata object {meta}"
    >>!+ ("meta", eventMetadata)
    |> logger.info
    //then build the context
    let ctx = EventContext(eventMetadata)
    
    //now match the context to the known routes
    let initHandler = fun ctx -> AsyncOption.retn ctx

    !! "Beginning Event Handling" |> logger.trace

    match! routes initHandler ctx with
    | Some ctx -> return ctx
    | None -> return ctx
  }

  let socketMsgHandler (routes: Core.EventHandler)  (msg : string) = async {
    let! r = socketMsgHandlerR msg routes
    match r with
    | Ok x -> return x
    | Error e -> return failwithf "%A" e
  }

  let handleMatch (matchFunc: EventContext -> Types.Received.EventReceived -> EventHandler) = 
    ActionRouting.matcher matchFunc

  type StreamDeckClient(args : Websockets.StreamDeckSocketArgs, handler : Core.EventHandler) =
    let msgHandler = socketMsgHandler handler
    let registerHandler = handleSocketRegister args

    let _socket = Websockets.StreamDeckConnection(args, msgHandler, registerHandler)

    member this.Run() = _socket.Run()

module TestCode =
  open Core
  open Context
  open FsToolkit.ErrorHandling
  open Websockets
  open ActionRouting

  let myAction (event : Types.Received.EventReceived) (next: EventFunc) (ctx : EventContext) = async {
    let ctx' = Core.addLog $"in My Action handler, with event {event}" ctx
    return! Core.flow next ctx'
  }

  let keyUpEvent (event : Types.Received.KeyPayload) (next : EventFunc) (ctx: EventContext) = async {
    let ctx' = Core.addLog $"In key up event handler, with event payload {event}" ctx
    return! Core.flow next ctx'
  }

  let settingsReceivedHandler (settings : Types.Received.SettingsPayload) (next : EventFunc) (ctx: EventContext) = async {
    let ctx' = Core.addLog $"In settings received handler, with event payload {settings}" ctx
    return! Core.flow next ctx'
  }

  let errorHandler (err : PipelineFailure) : EventHandler = Core.log ($"in error handler, error: {err}")
  let bindingErrorHandler (e) : EventHandler = Core.log($"In binding error handler, got event {e}")

  let settingsT = (Types.EventNames.DidReceiveSettings, typeof<Types.Received.SettingsPayload>)

  let keyPayloadStateCheck targetState (payload : Types.Received.KeyPayload) =
    payload.State = targetState

  let routes =
    let t2 =
      eventMatch Types.EventNames.KeyDown >=> Core.log "in KEY_DOWN handler" >=> tryBindKeyDownEvent errorHandler keyUpEvent

    let keydownroute =
      eventMatch Types.EventNames.KeyDown >=> Core.log "in KEY_DOWN handler" >=> tryBindEvent errorHandler myAction
    let wakeUpRoute =
      eventMatch Types.EventNames.SystemDidWakeUp >=> Core.log "in system wake up"
    
    choose [
      t2
      keydownroute
      wakeUpRoute
    ]

  //todo: try this, resolve types.
  let matchFuction (ctx : EventContext) event =
    match event with
    | Types.Received.EventReceived.KeyDown payload ->
      () |> Async.lift
    | Types.Received.EventReceived.DidReceiveSettings payload ->
      () |> Async.lift
    | Types.Received.EventReceived.SystemWakeUp ->
      () |> Async.lift
    | _ ->
      Core.addLog $"Unhandled event type:{event}" ctx |> ignore
      () |> Async.lift

  let run() =
    let args = {
      StreamDeckSocketArgs.Port = 0
      PluginUUID = System.Guid.Empty
      RegisterEvent = ""
      Info = ""
    }

    let client = Engine.StreamDeckClient(args, routes)
    client.Run()
    ()
