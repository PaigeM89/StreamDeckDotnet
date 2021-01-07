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
    let ctx = Context.ActionContext(actionReceived)
    //Context.fromActionReceived actionReceived
    
    //now match the context to the known routes
    let t = fun ctx -> ctx |> Some |> Async.lift
    let thing = routes (t) ctx

    return! () |> Ok
  }

  let socketMsgHandler (msg : string) (routes: Core.ActionHandler) = async {
    let! r = socketMsgHandlerR msg routes
    match r with
    | Ok x -> x
    | Error e -> failwithf "%A" e
    return ()
  }

  let compileFunction str = () |> Async.lift

  type StreamDeckClient(args : Websockets.StreamDeckSocketArgs, handler : Core.ActionHandler) = 
    let _socket = Websockets.StreamDeckConnection(args, compileFunction)
    
    member this.Run() = _socket.Run()

module TestCode =
  open Core
  open Context
  open FsToolkit.ErrorHandling
  open Websockets
  //open MessageRoutingBuilder
  open ActionRouting

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

  let settingsT2 = (Events.EventNames.DidReceiveSettings, fun v -> v |> function | Events.EventReceived.DidReceiveSettings _ -> true | _ -> false)

  let routes =
    let t2 =
      action Events.EventNames.KeyDown >=> Core.log "in KEY_DOWN handler" >=> tryBindKeyDownEvent errorHandler keyUpEvent

    let keydownroute = 
      action Events.EventNames.KeyDown >=> Core.log "in KEY_DOWN handler" >=> tryBindEvent errorHandler myAction
    let keyUpRoute =
      action Events.EventNames.KeyUp >=> Core.log "in key up route" >=> tryBindEvent errorHandler keyUpEvent >=> Core.bindEventPayload
    let wakeUpRoute = 
      action Events.EventNames.SystemDidWakeUp >=> Core.log "in system wake up"
    let settingsReceivedRoute =
      actionWithBinding settingsT2 >=> Core.log "did receive settings" >=> tryBindEventStrict errorHandler settingsReceivedHandler
    choose [
      keydownroute
      wakeUpRoute
    ]

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
