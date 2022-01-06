namespace StreamDeckDotnet


[<AutoOpen>]
module EventBinders =
  #if FABLE_COMPILER
  open Thoth.Json
  #else
  open Thoth.Json.Net
  #endif

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
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.KeyUp))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `DidReceiveSettings` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindDidReceiveSettingsEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.SettingsPayload -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.DidReceiveSettings payload -> successHandler (payload)
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.DidReceiveSettings))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `DidReceiveGlobalSettings` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindDidReceiveGlobalSettingsEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.GlobalSettingsPayload -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.DidReceiveGlobalSettings payload -> successHandler (payload)
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.DidReceiveGlobalSettings))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `WillAppear` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindWillAppearEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.AppearPayload -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.WillAppear payload -> successHandler (payload)
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.WillAppear))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `WillDisappear` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindWillDisappearEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.AppearPayload -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.WillDisappear payload -> successHandler (payload)
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.WillDisappear))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `TitleParametersDidChange` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindTitleParametersDidChangeEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.TitleParametersPayload -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.TitleParametersDidChange payload -> successHandler (payload)
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.TitleParametersDidChange))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `DeviceDidConnect` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindDeviceDidConnectEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.DeviceInfoPayload -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.DeviceDidConnect payload -> successHandler (payload)
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.DeviceDidConnect))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `DeviceDidDisconnect` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindDeviceDidDisconnectEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : unit -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.DeviceDidDisconnect -> successHandler ()
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.DeviceDidDisconnect))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `ApplicationDidLaunch` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindApplicationDidLaunchEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.ApplicationPayload -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.ApplicationDidLaunch payload -> successHandler (payload.Payload)
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.ApplicationDidTerminate))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `ApplicationDidTerminate` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindApplicationDidTerminateEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.ApplicationPayload -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.ApplicationDidTerminate payload -> successHandler (payload.Payload)
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.ApplicationDidTerminate))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `SystemDidWakeUp` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindSystemDidWakeUpEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : Received.AppearPayload -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.WillAppear payload -> successHandler (payload)
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.SystemDidWakeUp))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `PropertyInspectorDidAppear` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindPropertyInspectorDidAppearEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : unit -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.PropertyInspectorDidAppear -> successHandler ()
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.PropertyInspectorDidAppear))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `PropertyInspectorDidDisappear` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindPropertyInspectorDidDisappearEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : unit -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.PropertyInspectorDidDisappear -> successHandler ()
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.PropertyInspectorDidDisappear))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `SendToPlugin` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindSendToPluginEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : JsonValue -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.SendToPlugin payload -> successHandler (payload)
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.SendToPlugin))
    tryBindEventWithMatcher errorHandler filter

  /// Attempts to bind the event received in the context to a `SendToPropertyInspector` event, then runs the success handler.
  /// If the decoding or binding is not successful, the error handler is run.
  let tryBindSendToPropertyInspectorEvent (errorHandler : Context.PipelineFailure -> EventHandler) (successHandler : JsonValue -> EventHandler) =
    let filter (e : Received.EventReceived) =
      match e with
      | Received.EventReceived.SendToPropertyInspector payload -> successHandler (payload)
      | _ -> errorHandler (WrongEvent ((e.GetName()), EventNames.SendToPropertyInspector))
    tryBindEventWithMatcher errorHandler filter


[<AutoOpen>]
module Routing =
  open StreamDeckDotnet

  type EventRoute = EventFunc -> EventContext -> EventFuncResult

  /// Accepts a function that takes the action context and validates if the action route is valid.
  let private appState (stateCheck: EventContext -> bool) =
    fun next ctx ->
      if stateCheck ctx then next ctx else skipPipeline

  /// Checks the `stateCheck` function before running the success handler.
  /// Will run the error handler on failure.
  let inline contextStateWithError (stateCheck : EventContext -> bool) errorHandler successHandler =
    fun next ctx ->
      if stateCheck ctx then
        successHandler next ctx
      else
        errorHandler next ctx

  /// Checks the `stateCheck` function before running the success handler.
  /// Will skip the pipeline on a failure.
  let inline contextState (stateCheck : EventContext -> bool) successHandler =
    fun next ctx ->
      if stateCheck ctx then
        successHandler next ctx
      else
        skipPipeline

  /// Tries to bind the event received in the context using the given match function.
  let matcher matchFunc =
    let logErrorHandler err = Core.log (sprintf "Error handling event: %A" err)
    fun next (ctx: EventContext) ->
      tryBindEvent logErrorHandler (matchFunc ctx) next ctx

  /// Validates that the event received in the context matches the given event name.
  /// This validation is case insensitive.
  let eventMatch (eventName : string) : EventHandler =
    fun (next : EventFunc) (ctx : Context.EventContext) ->
      let validate = Core.validateAction eventName
      Core.validateEvent validate next ctx
