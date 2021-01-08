namespace StreamDeckDotnet.Routing

open StreamDeckDotnet

[<AutoOpenAttribute>]
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

