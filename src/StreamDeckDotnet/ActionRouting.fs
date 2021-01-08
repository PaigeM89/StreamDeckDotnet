namespace StreamDeckDotnet


module ActionRouting = 
  open Core
  open Context

  /// Routing that is only available once we have determined the payload type
  module PayloadRouting = 
    let multiAction() = true

    //todo: state router
    let inline contextState (stateCheck : int -> 'a -> bool) =
      fun next ctx ->
        next ctx


  /// Accepts a function that takes the action context and validates if the action route is valid.
  let appState (stateCheck: ActionContext -> bool) =
    fun next ctx ->
      next ctx

  let matcher matchFunc =
    let logErrorHandler err =
      Core.log $"Error handling event: {err}"
    fun next (ctx: ActionContext) ->
      tryBindEvent logErrorHandler (matchFunc ctx) next ctx

  let action (eventName : string) : ActionHandler =
    fun (next : ActionFunc) (ctx : Context.ActionContext) ->
      let validate = Core.validateAction eventName
      Core.actionReceived validate next ctx


  let tryBindToKeyPayload decodingErrorHandler bindingErrorHandler successHandler =
    fun next ctx -> 
      let validatePayload e =
        match e with
        | Events.KeyUp payload -> successHandler payload
        | Events.KeyDown payload -> successHandler payload
        | _ -> bindingErrorHandler e next ctx
      tryBindEvent decodingErrorHandler validatePayload next ctx

  let tryBindKeyDownEvent (errorHandler : Context.ActionFailure -> ActionHandler) (successHandler : Types.Received.KeyPayload -> ActionHandler) =
    fun next (ctx : ActionContext) ->
      let filter (e : Events.EventReceived)  = 
        match e with
        | Events.EventReceived.KeyDown payload -> successHandler payload
        | _ -> errorHandler (Context.ActionFailure.WrongEvent ((e.GetName()), Events.EventNames.KeyDown))
      tryBindEvent errorHandler filter next ctx

  let tryBindKeyDownEventPipeline (errorHandler : Context.ActionFailure -> ActionHandler) (successHandler : Types.Received.KeyPayload -> ActionHandler) =
    fun next (ctx : ActionContext) ->
      let filter (e : Events.EventReceived)  = 
        match e with
        | Events.EventReceived.KeyDown payload -> successHandler payload
        | _ -> errorHandler Context.ActionFailure.Placeholder
      tryBindEvent errorHandler filter next ctx

module Engine =
  //open MessageRoutingBuilder
  open FsToolkit.ErrorHandling
  open Context
  open Core

  type ActionDelegate = delegate of ActionContext -> Async<unit>

  type ActionMiddleware(next : ActionDelegate, handler: ActionHandler) =
    let func = handler earlyReturn

    member this.Invoke (ctx : ActionContext) =
      async {
        let! result = func ctx
        if result.IsNone then return! next.Invoke ctx
      }

  type RequestHandler = ActionFunc -> ActionContext -> ActionFuncResult

  // let mapActions (routes : ActionRoute list) =
  //   routes
  //   |> List.iter(fun r ->
  //     match r with
  //     | SimpleRoute (r, n, a) -> MessageRoutingBuilder.mapSingleAction (r, n, a)
  //     | MultiRoutes (routes) -> mapMultiAction routes
  //   )

  let inspectRoute (handler : RequestHandler) next (ctx : ActionContext) =
    let thing = next ctx |> Async.RunSynchronously
    match thing with
    | Some ctx -> 
      handler next ctx
    | None -> skipPipeline


  // let buildActionRoutes (rootHandler : RequestHandler) (ctx : ActionContext) =
  //   let func : ActionFunc = rootHandler earlyReturn
  //   let t = 
  //     async {
  //       let! result = func ctx
  //       if result.IsNone then return! next.Invoke(ctx)
  //     }
  //   //let buildActionRoute (func: ActionFunc, ctx : ActionContext, funcResult : ActionFuncResult) =
      
  //   ()

  // let matchActionContextToHandler (ctx : Context.ActionContext) (routes: ActionRoute list) = 
  //   ()

  // handles the raw json message from the web socket
  let socketMsgHandlerR (msg : string) (routes: Core.ActionHandler) = asyncResult {
    //first decode into an ActionReceived
    let! actionReceived = Types.decodeActionReceived msg
    //then build the context
    let ctx = ActionContext(actionReceived)
    
    //now match the context to the known routes
    let t = fun ctx -> AsyncOption.retn ctx

    match! routes t ctx with
    | Some ctx -> return ctx
    | None -> return ctx
  }

  let socketMsgHandler (routes: Core.ActionHandler)  (msg : string) = async {
    let! r = socketMsgHandlerR msg routes
    match r with
    | Ok x -> return x
    | Error e -> return failwithf "%A" e
  }

  let handleMatch (matchFunc: ActionContext -> Events.EventReceived -> ActionHandler) = 
    ActionRouting.matcher matchFunc

  type StreamDeckClient(args : Websockets.StreamDeckSocketArgs, handler : Core.ActionHandler) =
    let msgHandler = socketMsgHandler handler
    let _socket = Websockets.StreamDeckConnection(args, msgHandler)
    
    member this.Run() = _socket.Run()

module TestCode =
  open Core
  open Context
  open FsToolkit.ErrorHandling
  open Websockets
  //open MessageRoutingBuilder
  open ActionRouting
  open ActionRouting.PayloadRouting
  

  let myAction (event : Events.EventReceived) (next: ActionFunc) (ctx : ActionContext) = async {
    Core.addLog $"in My Action handler, with event {event}" ctx
    return! Core.flow next ctx
  }

  let keyUpEvent (event : Types.Received.KeyPayload) (next : ActionFunc) (ctx: ActionContext) = async {
    Core.addLog $"In key up event handler, with event payload {event}" ctx
    return! Core.flow next ctx
  }

  let settingsReceivedHandler (settings : Types.Received.SettingsPayload) (next : ActionFunc) (ctx: ActionContext) = async {
    Core.addLog $"In settings received handler, with event payload {settings}" ctx
    return! Core.flow next ctx
  }

  let errorHandler (err : ActionFailure) : ActionHandler = Core.log ($"in error handler, error: {err}")
  let bindingErrorHandler (e) : ActionHandler = Core.log($"In binding error handler, got event {e}")

  let settingsT = (Events.EventNames.DidReceiveSettings, typeof<Types.Received.SettingsPayload>)

  let keyPayloadStateCheck targetState (payload : Types.Received.KeyPayload) =
    payload.State = targetState

  // let keyUpRoutes = choose [
  //   state (fun s -> s = 0)
  // ]

  let routes =
    let t2 =
      action Events.EventNames.KeyDown >=> Core.log "in KEY_DOWN handler" >=> tryBindKeyDownEvent errorHandler keyUpEvent

    // let t3 =
    //   action Events.EventNames.KeyDown >=> Core.log "in KEY_DOWN handler" >=> tryBindKeyDownEventPipeline errorHandler >=> Core.log "more logging" >=> keyUpEvent

    let keydownroute =
      action Events.EventNames.KeyDown >=> Core.log "in KEY_DOWN handler" >=> tryBindEvent errorHandler myAction
    let wakeUpRoute =
      action Events.EventNames.SystemDidWakeUp >=> Core.log "in system wake up"
    
    choose [
      t2
      keydownroute
      wakeUpRoute
    ]

  //todo: try this, resolve types.
  let matchFuction (ctx : ActionContext) event =
    match event with
    | Events.EventReceived.KeyDown payload ->
      () |> Async.lift
    | Events.EventReceived.DidReceiveSettings payload ->
      () |> Async.lift
    | Events.EventReceived.SystemWakeUp ->
      () |> Async.lift
    | _ ->
      Core.addLog $"Unhandled event type:{event}" ctx
      () |> Async.lift

  let run() =
    let args = {
      StreamDeckSocketArgs.Port = 0
      Id = System.Guid.Empty
      RegisterEvent = ""
      Info = ""
    }

    let client = Engine.StreamDeckClient(args, routes)
    client.Run()
    ()
