namespace StreamDeckDotnet

[<AutoOpen>]
module Context =
  open System
  open Types
  open Types.Sent
  open Types.Received
  open FsToolkit.ErrorHandling
  open StreamDeckDotnet
  open StreamDeckDotnet.Logging
  open StreamDeckDotnet.Logging.Operators
  #if FABLE_COMPILER
  open Thoth.Json
  #else
  open Thoth.Json.Net
  #endif
  open System.Collections.Concurrent

  let private logger = LogProvider.getLoggerByName("StreamDeckDotnet.Context")

  /// A failure in the event pipeline to handle an event.
  /// This is only returned if the Context is expected to evaluate but does not.
  type PipelineFailure =
  /// The input did not decode to an expected type.
  | DecodeFailure of input : string * errorMsg : string
  /// The event type is not known and cannot be decoded.
  | UnknownEventType of eventName : string
  /// Attempted to decode a payload for a type that does not have a payload, such as SystemDidWakeUp
  | NoPayloadForType of eventName : string
  /// Attempted to decode a type that requires a payload, but the payload was not found.
  | PayloadMissing
  /// A specific event handler (eg, `tryBindKeyDownEvent`) attempted to decode a payload, but
  /// the event received was a different event type.
  | WrongEvent of encounteredEvent : string * expectedEvent : string

  /// Checks a given input & result, and generates a `DecodeFailure` if the result is a failure.
  let inline mapDecodeError<'a> input (res : Result<_, string>) =
    match res with
    | Ok x -> Ok x
    | Error msg -> DecodeFailure(input, msg) |> Error

  /// Attempts to decode the given input with the given function.
  /// Returns Ok or a DecodeFailure
  let tryDecode<'a> input decodeFunc : Result<'a, PipelineFailure> = result {
    match decodeFunc input with
    | Ok x -> return x
    | Error msg -> return! DecodeFailure(input, msg) |> Error
  }

  /// A function that accepts a string and returns a result, or a string error message.
  [<Obsolete()>]
  type DecodeFunc<'a> = string -> Result<'a, string>

  /// A function that accepts a JsonValue and returns a result of the decoded value or a string error message.
  type JsonDecodeFunc<'a> = JsonValue -> Result<'a, string>

  /// Denotes an instance where a decode function may be needed
  type Decoding<'a> =
  /// A function to decode a specific payload, and that payload (if present)
  //| PayloadRequired of decodeFunc : DecodeFunc<'a> * payload : string option
  /// A function to decode a specific payload, and the payload to decode (if present)
  | PayloadRequired of decodeFunc : JsonDecodeFunc<'a> * payload : JsonValue option
  /// An `EventReceived` that does not require a payload.
  | NoPayloadRequired of EventReceived

  /// Decodes with the given function, checking to see if a payload is required and if it is present.
  let decode<'a> func =
    match func with
    | PayloadRequired (func, payload) ->
      match payload with
      | Some p ->
        !!! "Attempting to decode payload '{payload}'"
        >>!+ ("payload", (Json.anyOptionToString payload))
        |> logger.info
        p |> func |> mapDecodeError (string p)
      | None -> PayloadMissing |> Error
    | NoPayloadRequired e -> Ok e

  /// The metadata associated and generated when an event is sent from StreamDeck.
  /// Requires `EventMetadata`, which is sent with every event.
  type EventContext(eventMetadata : EventMetadata) =
    let mutable _eventReceived : EventReceived option = None
    let mutable _eventsToSend : EventSent list option = None

    /// The `EventMetadata` that was sent from StreamDeck.
    member _.EventMetadata = eventMetadata

    /// The more specific `EventReceived` that was sent from StreamDeck.
    /// This is only populated when the event handler pipeline attempts to parse the event metadata event
    /// type and payload.
    member _.EventReceived = _eventReceived

    /// The name of the event currently being processed.
    member this.EventName = this.EventMetadata.Event

    /// Attempts to bind the `Event` and `Payload` (if applicable) in the `EventMetadata` to
    /// an `EventReceived`. This will automatically match the `Event` to the appropriate type.
    member _.TryBindEventAsync() = asyncResult {
      printfn "Attempting to bind event"
      let keyPayloadFunc mapper = tryDecodePayload KeyPayloadDU.Decoder mapper
      let applicationPayloadFunc mapper = tryDecodePayload ApplicationPayloadDU.Decoder mapper
      let decoder =
        let event = eventMetadata.Event.ToLowerInvariant()
        printfn "Matching event %s" event
        match event with
        | InvariantEqual EventNames.KeyDown ->
          let mapper = KeyPayloadDU.KeyDown >> EventReceived.KeyDown
          fun p -> decode (PayloadRequired (keyPayloadFunc mapper, p))
        | InvariantEqual EventNames.KeyUp ->
          let mapper = KeyPayloadDU.KeyUp >> EventReceived.KeyUp
          fun p -> decode (PayloadRequired (keyPayloadFunc mapper, p))
        | InvariantEqual EventNames.DidReceiveSettings ->
          fun p ->
            (tryDecodePayload SettingsPayload.Decoder DidReceiveSettings, p)
            |> PayloadRequired |> decode
        | InvariantEqual EventNames.DidReceiveGlobalSettings ->
          fun p ->
              (tryDecodePayload GlobalSettingsPayload.Decoder DidReceiveGlobalSettings, p)
              |> PayloadRequired |> decode
        | InvariantEqual EventNames.WillAppear ->
          fun p ->
            (tryDecodePayload AppearPayload.Decoder WillAppear, p)
            |> PayloadRequired |> decode
        | InvariantEqual EventNames.WillDisappear ->
          fun p ->
            (tryDecodePayload AppearPayload.Decoder WillDisappear, p)
            |> PayloadRequired |> decode
        | InvariantEqual EventNames.TitleParametersDidChange ->
          fun p ->
            (tryDecodePayload TitleParametersPayload.Decoder TitleParametersDidChange, p)
            |> PayloadRequired |> decode
        | InvariantEqual EventNames.DeviceDidConnect ->
          fun p ->
            (tryDecodePayload DeviceInfoPayload.Decoder DeviceDidConnect, p)
            |> PayloadRequired |> decode
        | InvariantEqual EventNames.DeviceDidDisconnect ->
          fun _ -> decode (NoPayloadRequired DeviceDidDisconnect)
        | InvariantEqual EventNames.ApplicationDidLaunch ->
          fun p ->
            let mapper = (ApplicationPayloadDU.Launch >> ApplicationDidLaunch)
            (applicationPayloadFunc mapper, p)
            |> PayloadRequired |> decode
        | InvariantEqual EventNames.ApplicationDidTerminate ->
          fun p ->
            let mapper = (ApplicationPayloadDU.Terminate >> ApplicationDidTerminate)
            (applicationPayloadFunc mapper, p)
            |> PayloadRequired |> decode
        | InvariantEqual EventNames.SystemDidWakeUp ->
          fun _ -> decode (NoPayloadRequired SystemDidWakeUp)
        | InvariantEqual EventNames.PropertyInspectorDidAppear ->
          fun _ ->
            PropertyInspectorDidAppear
            |> NoPayloadRequired |> decode
        | InvariantEqual EventNames.PropertyInspectorDidDisappear ->
          fun _ ->
            PropertyInspectorDidDisappear
            |> NoPayloadRequired |> decode
        | InvariantEqual EventNames.SendToPlugin ->
          fun p ->
            match p with
            | Some payload ->
              payload |> SendToPlugin |> Ok
            | None ->
              #if FABLE_COMPILER
              null |> SendToPlugin |> Ok
              #else
              JsonValue.Parse("{}") |> SendToPlugin |> Ok
              #endif
        | InvariantEqual EventNames.SendToPropertyInspector ->
          fun p ->
            match p with
            | Some payload ->
              payload |> SendToPropertyInspector |> Ok
            | None ->
              #if FABLE_COMPILER
              // fable can handle nulls
              null |> SendToPropertyInspector |> Ok
              #else
              // but regular F# parsing wants an empty object
              JsonValue.Parse("{}") |> SendToPropertyInspector |> Ok
              #endif
        | _ ->
          fun _ -> UnknownEventType event |> Error
      (StreamDeckDotnet.Json.anyOptionToString eventMetadata.Payload) |> printfn "Attempting to decode payload: %A"
      return! decoder eventMetadata.Payload
    }

    /// Add the given `EventSent` to the event queue to send back to StreamDeck.
    member _.AddSendEvent e =
      match _eventsToSend with
      | None ->
        _eventsToSend <- Some [e]
      | Some es ->
        _eventsToSend <- Some (e :: es)

    /// <summary>
    /// Adds the given log message to the event queue.
    /// </summary>
    /// <param name="msg">The string message to add as a log.</param>
    /// <returns>unit</returns>
    member this.AddLog msg =
      let log = createLogEvent msg
      this.AddSendEvent log

    /// Adds a "Show Ok" event to the event queue.
    member this.AddOk() =
      let ok = createOkEvent()
      this.AddSendEvent ok

    /// Adds a "Show Alert" event to the event queue.
    member this.AddAlert() =
      let ohno = createAlertEvent()
      this.AddSendEvent ohno

    /// Returns a list of the events that will be sent to StreamDeck, in the order they were added.
    /// This is called at the end of event processing, when getting the list of events to send to the stream deck.
    member _.GetEventsToSend() =
      match _eventsToSend with
      | Some x -> x
      | None -> []


    member this.GetEncodedEventsToSend() =
      this.GetEventsToSend()
      |> List.map (fun payload ->
        let payload = payload.Encode this.EventMetadata.Context this.EventMetadata.Device
        // !! "Created event sent payload of {payload}"
        // >>!- ("payload", payload)
        // |> logger.debug
        payload
      )

    /// Removes all queued events that match the given predicate.
    member _.PurgeEventsMatching predicate =
      match _eventsToSend with
      | Some x -> _eventsToSend <- List.filter predicate x |> Some
      | None -> ()

    /// Returns the Guid of the Context, or None if it was not bound.
    member this.TryGetContextGuid() =
      match this.EventMetadata.Context with
      | Some x ->
        match System.Guid.TryParse x with
        | true, v -> Some v
        | false, _ -> None
      | None -> None

  /// Adds the given `EventSent` to the `EventContext`
  let addSendEvent e (ctx : EventContext) =
    ctx.AddSendEvent e
    ctx

  /// Lift an instance of an `EventContext` to an async result.
  let lift (ctx : EventContext) = Some ctx |> Async.singleton
