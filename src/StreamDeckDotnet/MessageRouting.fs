namespace StreamDeckDotnet


module MessageRoutingBuilder =
  open Microsoft.Extensions.Logging
  open Events
  open Core
  open FsToolkit.ErrorHandling

  type Receive =
  | KeyDown
  | KeyUp
  /// allow for externally raised events somehow
  | External
  | NotSpecified

  type ActionRoute = 
  | SimpleRoute of Receive * actionName : string * ActionHandler
  //| NestedRoutes (see giraffe??)
  | MultiRoutes of ActionRoute list

  let rec private applyReceiveToActionRoute (receive : Receive) (route : ActionRoute) : ActionRoute =
    match route with
    | SimpleRoute (_, eventName, handler) -> SimpleRoute (receive, eventName, handler)
    | MultiRoutes routes ->
      routes
      |> List.map(applyReceiveToActionRoute receive)
      |> MultiRoutes

  let rec private applyReceiveToActionRoutes (receive: Receive) (routes : ActionRoute list) : ActionRoute =
    routes
    |> List.map(fun route ->
      match route with
      | SimpleRoute (_, eventName, handler) -> SimpleRoute (receive, eventName, handler)
      | MultiRoutes routes ->
        applyReceiveToActionRoutes receive routes
    ) |> MultiRoutes

  let KEY_DOWN = applyReceiveToActionRoutes Receive.KeyDown
  let KEY_UP = applyReceiveToActionRoute Receive.KeyUp

  let action (eventName : string) (handler : ActionHandler) =
    SimpleRoute (Receive.NotSpecified, eventName, handler)

  let action2 (actionDiscriminator : ActionHandler) (handler : ActionHandler) = 
    SimpleRoute(Receive.NotSpecified, "", handler)

  let mapSingleAction(actionEndpoint : Receive * string * ActionHandler) =
    ()

  let mapMultiAction(multiAction: ActionRoute list) = ()


module Engine =
  open MessageRoutingBuilder
  open FsToolkit.ErrorHandling
  open Context
  open Core

  let mapActions (routes : ActionRoute list) =
    routes
    |> List.iter(fun r ->
      match r with
      | SimpleRoute (r, n, a) -> MessageRoutingBuilder.mapSingleAction (r, n, a)
      | MultiRoutes (routes) -> mapMultiAction routes
    )

  let matchActionContextToHandler (ctx : Context.ActionContext) (routes: ActionRoute list) = 
    ()

  // handles the raw json message from the web socket
  let socketMsgHandlerR (msg : string) (routes: Core.ActionHandler) = asyncResult {
    //first decode into an ActionReceived
    let! actionReceived = Types.decodeActionReceived msg
    //then build the context
    let ctx = ActionContext(actionReceived)
    
    //now match the context to the known routes
    let t = fun ctx -> AsyncOption.retn ctx
    //let thing = routes (t) ctx

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
      keydownroute
      wakeUpRoute
    ]

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
