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
    // fun (final : ActionFunc) ->
    //   let func = final |> action2 |> action1
    //   fun (ctx : ActionContext) ->
    //     match ctx.EventReceived.IsSome with
    //     | true -> final ctx
    //     | false -> func ctx
      //final |> action2 |> action1
    //action2 >> action1

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

  let tryBindEventPayload (errorHandler : string -> ActionHandler) (successHandler : EventReceived -> ActionHandler) : ActionHandler =
    fun (next : ActionFunc) (ctx: ActionContext) -> async {
      let! result = ctx.TryBindEventAsync
      match result with
      | Ok event -> return! successHandler event next ctx
      | Error msg -> return! errorHandler msg next ctx
    }

  let private actionReceived (validate: string -> bool) : ActionHandler =
    fun (next : ActionFunc) (ctx : ActionContext) ->
      if validate ctx.ActionReceived.Event
      then next ctx
      else skipPipeline

  let private validateAction (s : string) (t : string) = 
    s.ToLowerInvariant() = t.ToLowerInvariant()

  let KEY_DOWN : ActionHandler = actionReceived (validateAction EventNames.DidReceiveSettings)

  let addLog (msg : string) (ctx : ActionContext) =
    let log = Events.createLogEvent msg
    Context.addSendEvent log ctx

  let log (msg : string) : ActionHandler =
    fun (_ : ActionFunc) (ctx : ActionContext) ->
      addLog msg ctx
  
  let log2 (msg : string) : ActionHandler =
    let log = Events.createLogEvent msg
    fun (_ : ActionFunc) (ctx: ActionContext) ->
      Context.addSendEvent log ctx

  let flow (_ : ActionFunc) (ctx: ActionContext) = Context.flow ctx


module MessageRoutingBuilder =
  open Microsoft.Extensions.Logging
  open Events
  open Core

  type Receive =
  | KeyDown
  | KeyUp
  /// allow for externally raised events somehow
  | External
  | NotSpecified

  type ActionRoute = 
  | SimpleRoute of Receive * ActionHandler
  //| NestedRoutes (see giraffe??)
  | MultiRoutes of ActionRoute list

  let rec private applyReceiveToActionRoute (receive : Receive) (route : ActionRoute) : ActionRoute =
    match route with
    | SimpleRoute (_, handler) -> SimpleRoute (receive, handler)
    | MultiRoutes routes ->
      routes
      |> List.map(applyReceiveToActionRoute receive)
      |> MultiRoutes

  let rec private applyReceiveToActionRoutes (receive: Receive) (routes : ActionRoute list) : ActionRoute =
    routes
    |> List.map(fun route ->
      match route with
      | SimpleRoute (_, handler) -> SimpleRoute (receive, handler)
      | MultiRoutes routes ->
        applyReceiveToActionRoutes receive routes
    ) |> MultiRoutes

  let KEY_DOWN = applyReceiveToActionRoutes Receive.KeyDown
  let KEY_UP = applyReceiveToActionRoute Receive.KeyUp

  let action (handler : ActionHandler) =
    SimpleRoute (Receive.NotSpecified, handler)


module TestCode =
  open Core
  open Context
  open FsToolkit.ErrorHandling


  let myAction (event : Events.EventReceived) (next: ActionFunc) (ctx : ActionContext) = async {
    let! thing = Core.addLog $"in My Action handler, with event {event}" ctx
    match thing with
    | Some ctx -> return! Core.flow next ctx
    | None -> return! Core.flow next ctx
  }

  let errorHandler (msg : string) : ActionHandler = Core.log ($"in error handler, error: {msg}")

  let routes = choose [
    KEY_DOWN >=> Core.log "in KEY_DOWN handler" >=> tryBindEventPayload errorHandler myAction
  ]
