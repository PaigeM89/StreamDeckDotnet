namespace StreamDeckDotnet

[<AutoOpen>]
module Context =
  open Types
  open Types.Sent
  open Types.Received
  open FsToolkit.ErrorHandling
  open System.Collections.Concurrent

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
  type DecodeFunc<'a> = string -> Result<'a, string>

  /// Denotes an instance where a decode function may be needed
  type Decoding<'a> =
  /// A function to decode a specific payload, and that payload (if present)
  | PayloadRequired of decodeFunc : DecodeFunc<'a> * payload : string option
  /// An `EventReceived` that does not require a payload.
  | NoPayloadRequired of EventReceived

  /// Decodes with the given function, checking to see if a payload is required and if it is present.
  let inline decode<'a>  func =
    match func with
    | PayloadRequired (func, payload) ->
      match payload with
      | Some p -> func p |> mapDecodeError p
      | None -> PayloadMissing |> Error
    | NoPayloadRequired e -> Ok e

  /// The metadata associated and generated when an event is sent from StreamDeck.
  /// Requires `EventMetadata`, which is sent with every event.
  type EventContext(eventMetadata : EventMetadata) =
    let mutable _eventReceived : EventReceived option = None
    let mutable _eventsToSend : EventSent list option = None
    let mutable _sendEventQueue : ConcurrentQueue<EventSent > = new ConcurrentQueue<EventSent>()

    /// The `EventMetadata` that was sent from StreamDeck.
    member _.EventMetadata = eventMetadata

    /// The more specific `EventReceived` that was sent from StreamDeck.
    /// This is only populated when the event handler pipeline attempts to parse the event metadata event 
    /// type and payload.
    member _.EventReceived = _eventReceived

    /// Attempts to bind the `Event` and `Payload` (if applicable) in the `EventMetadata` to 
    /// an `EventReceived`. This will automatically match the `Event` to the appropriate type.
    member _.TryBindEventAsync = asyncResult {
      let keyPayloadFunc mapper = Types.tryDecodePayload Received.KeyPayloadDU.Decoder mapper
      let applicationPayloadFunc mapper = tryDecodePayload ApplicationPayloadDU.Decoder mapper
      let decoder =
        let event = eventMetadata.Event.ToLowerInvariant()
        match event with
        | InvariantEqual EventNames.KeyDown ->
          let mapper = (fun v -> KeyPayloadDU.KeyDown v |> EventReceived.KeyDown)
          fun p -> decode (PayloadRequired (keyPayloadFunc mapper, p))
        | InvariantEqual EventNames.KeyUp ->
          let mapper = (fun v -> KeyPayloadDU.KeyUp v |> EventReceived.KeyUp)
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
            let mapper = (fun v -> ApplicationPayloadDU.Launch v |> ApplicationDidLaunch)
            (applicationPayloadFunc mapper, p)
            |> PayloadRequired |> decode
        | InvariantEqual EventNames.ApplicationDidTerminate ->
          fun p ->
            let mapper = (fun v -> ApplicationPayloadDU.Terminate v |> ApplicationDidTerminate)
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
              Newtonsoft.Json.Linq.JToken.Parse(payload) |> SendToPlugin |> Ok
            | None ->
              Newtonsoft.Json.Linq.JToken.Parse("{}") |> SendToPlugin |> Ok
        | InvariantEqual EventNames.SendToPropertyInspector ->
          fun p -> 
            match p with
            | Some payload ->
              Newtonsoft.Json.Linq.JToken.Parse(payload) |> SendToPropertyInspector |> Ok
            | None ->
              Newtonsoft.Json.Linq.JToken.Parse("{}") |> SendToPropertyInspector |> Ok
        | _ ->
          fun _ -> UnknownEventType event |> Error
      return! decoder eventMetadata.Payload
    }

    /// Add the given `EventSent` to the event queue to send back to StreamDeck.
    member _.AddSendEvent e =
      _sendEventQueue.Enqueue(e)
      match _eventsToSend with
      | None ->
        _eventsToSend <- Some [e]
      | Some es ->
        _eventsToSend <- Some (e :: es)

    /// Adds the given log message to the event queue.
    member _.AddLog msg =
      let log = createLogEvent msg
      _sendEventQueue.Enqueue(log)

    /// Adds a "Show Ok" event to the event queue.
    member _.AddOk() =
      let ok = createOkEvent()
      _sendEventQueue.Enqueue(ok)

    /// Adds a "Show Alert" event to the event queue.
    member _.AddAlert() =
      let ohno = createAlertEvent()
      _sendEventQueue.Enqueue(ohno)

    /// Returns a list of the events that will be sent to StreamDeck.
    [<System.Obsolete("Use the event queue")>]
    member _.GetEventsToSendFromList() = 
      match _eventsToSend with
      | Some x -> x
      | None -> []

    /// Returns a list of the events that will be sent to StreamDeck, in the order they were added.
    /// This is called at the end of event processing, when getting the list of events to send to the stream deck.
    member _.GetEventsToSend() =
      _sendEventQueue.ToArray() |> List.ofArray

    /// Removes all queued events that match the given predicate.
    member _.PurgeEventsMatching f =
      /// there has to be a better way to do this.
      let filteredEvents =
        _sendEventQueue.ToArray()
        |> Array.filter (fun x -> f x |> not)
      // clear the queue by making a new one
      _sendEventQueue <- new ConcurrentQueue<_>()
      filteredEvents |> Array.iter (fun x -> _sendEventQueue.Enqueue x)

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
  let lift (ctx : EventContext) = Some ctx |> Async.lift
