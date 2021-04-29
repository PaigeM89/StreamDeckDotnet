namespace StreamDeckDotnet

open StreamDeckDotnet.Logging
open StreamDeckDotnet.Logger
open StreamDeckDotnet.Logger.Operators

[<AutoOpen>]
module Routing = 
  open Core
  open Context
  open Types

  type EventRoute = EventFunc -> EventContext -> EventFuncResult

  /// Routing that is only available once we have determined the payload type
  module PayloadRouting = 
    let multiAction() = true

    //todo: state router
    let inline contextState (stateCheck : EventContext -> bool) errorHandler successHandler =
      fun next ctx ->
        if stateCheck ctx then
          successHandler next ctx
        else
          errorHandler next ctx

  /// Accepts a function that takes the action context and validates if the action route is valid.
  let appState (stateCheck: EventContext -> bool) =
    fun next ctx ->
      next ctx

  /// Tries to bind the event received in the context using the given match function.
  let matcher matchFunc =
    let logErrorHandler err =
      Core.log $"Error handling event: {err}"
    fun next (ctx: EventContext) ->
      tryBindEvent logErrorHandler (matchFunc ctx) next ctx

  /// Validates that the event received in the context matches the given event name.
  /// This validation is case insensitive.
  let eventMatch (eventName : string) : EventHandler =
    fun (next : EventFunc) (ctx : Context.EventContext) ->
      let validate = Core.validateAction eventName
      Core.validateEvent validate next ctx

  module EventBinders =

    let inline private tryBindEventWithMatcher errHandler matcher =
      fun next (ctx : EventContext) ->
        let filter (e : Received.EventReceived) =
          matcher e
        tryBindEvent errHandler filter next ctx

    /// Attempts to bind the event received in the context to a `KeyDown` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindKeyDownEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.KeyPayload -> EventHandler) =
      fun next (ctx : EventContext) ->
        let filter (e : Received.EventReceived) =
          match e with
          | Received.EventReceived.KeyDown payload -> successHandler (payload.Payload)
          | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
        tryBindEvent errorHandler filter next ctx
    
    /// Attempts to bind the event received in the context to a `KeyDown` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindKeyUpEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.KeyPayload -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.KeyUp payload -> successHandler (payload.Payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `DidReceiveSettings` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindDidReceiveSettingsEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.SettingsPayload -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.DidReceiveSettings payload -> successHandler (payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `DidReceiveGlobalSettings` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindDidReceiveGlobalSettingsEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.GlobalSettingsPayload -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.DidReceiveGlobalSettings payload -> successHandler (payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `WillAppear` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let trybindWillAppearEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.AppearPayload -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.WillAppear payload -> successHandler (payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `WillDisappear` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindWillDisappearEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.AppearPayload -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.WillDisappear payload -> successHandler (payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `TitleParametersDidChange` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindTitleParametersDidChangeEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.TitleParametersPayload -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.TitleParametersDidChange payload -> successHandler (payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `DeviceDidConnect` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindDeviceDidConnectEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.DeviceInfoPayload -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.DeviceDidConnect payload -> successHandler (payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `DeviceDidDisconnect` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindDeviceDidDisconnectEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : unit -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.DeviceDidDisconnect -> successHandler ()
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `ApplicationDidLaunch` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindApplicationDidLaunchEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.ApplicationPayload -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.ApplicationDidLaunch payload -> successHandler (payload.Payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `ApplicationDidTerminate` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindApplicationDidTerminateEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.ApplicationPayload -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.ApplicationDidTerminate payload -> successHandler (payload.Payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `SystemDidWakeUp` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindSystemDidWakeUpEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.AppearPayload -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.WillAppear payload -> successHandler (payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `PropertyInspectorDidAppear` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindPropertyInspectorDidAppearEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : unit -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.PropertyInspectorDidAppear -> successHandler ()
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `PropertyInspectorDidDisappear` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindPropertyInspectorDidDisappearEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : unit -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.PropertyInspectorDidDisappear -> successHandler ()
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `SendToPlugin` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindSendToPluginEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Newtonsoft.Json.Linq.JToken -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.SendToPlugin payload -> successHandler (payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

    /// Attempts to bind the event received in the context to a `SendToPropertyInspector` event, then runs the success handler.
    /// If the decoding or binding is not successful, the error handler is run.
    let tryBindSendToPropertyInspectorEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Newtonsoft.Json.Linq.JToken -> EventHandler) =
      let filter (e : Received.EventReceived) = 
        match e with
        | Received.EventReceived.SendToPropertyInspector payload -> successHandler (payload)
        | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyDown))
      tryBindEventWithMatcher errorHandler filter

module internal Engine =
  open FsToolkit.ErrorHandling
  open Context
  open Core

  let private logger = LogProvider.getLoggerByName("StreamDeckDotnet.Engine")

  // type RequestHandler = EventFunc -> EventContext -> EventFuncResult

  // let evaluateStep (handler : RequestHandler) next (ctx : EventContext) : EventFuncResult =
  //   async {
  //     match! next ctx with
  //     | Some ctx -> return! handler next ctx
  //     | None -> return! skipPipeline
  //   }

  // handles application registration & sends a `RegisterPlugin` event to streamdeck application.
  let handleSocketRegister (args : Websockets.StreamDeckSocketArgs) () =
    let register = Sent.RegisterPluginPayload.Create args.RegisterEvent args.PluginUUID
    !! "Creating registration event of {event}"
    >>!+ ("event", register)
    |> logger.info
    let sendEvent = Sent.EventSent.RegisterPlugin register
    sendEvent.Encode None None //context or device not needed for this event.

  // handles the raw json message from the web socket
  let socketMsgHandlerR (msg : string) (routes: Core.EventHandler) = asyncResult {
    //first decode into an EventMetadata
    !! "Decoding event metadata from msg '{msg}'"
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
    | Some ctx ->
      !! "Event {name} was successfully handled" >>!+ ("name", ctx.EventReceived) |> logger.trace
      return ctx
    | None ->
      !! "No route found to handle event {eventName}" >>!+ ("eventName", ctx.EventReceived) |> logger.warn
      return ctx
  }

  let socketMsgHandler (routes: Core.EventHandler) (msg : string) = async {
    let! r = socketMsgHandlerR msg routes
    match r with
    | Ok x -> return x
    | Error e -> return failwithf "%A" e
  }

[<AutoOpen>]
module Client =
  open Engine

  type StreamDeckClient(args : Websockets.StreamDeckSocketArgs, handler : Core.EventHandler) =
    let msgHandler = socketMsgHandler handler
    let registerHandler = handleSocketRegister args

    let _socket = Websockets.StreamDeckConnection(args, msgHandler, registerHandler)

    member _.Run() = _socket.Run()
