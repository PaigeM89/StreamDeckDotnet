namespace StreamDeckDotnet

module Context =
  open Types
  open FsToolkit.ErrorHandling
  open Thoth.Json.Net

  type PipelineFailure =
  | DecodeFailure of input : string * errorMsg : string
  | UnknownEventType of eventName : string
  /// Returned when attempting to decode a payload for a type that does not have one, such as SystemWakeUp
  | NoPayloadForType of eventName : string
  | PayloadMissing
  | WrongEvent of encounteredEvent : string * expectedEvent : string
  | Placeholder 

  let inline mapDecodeError<'a> input (res : Result<_, string>) = 
    match res with
    | Ok x -> Ok x
    | Error msg -> DecodeFailure(input, msg) |> Error

  let tryDecode<'a> input decodeFunc : Result<'a, PipelineFailure> = result {
    match decodeFunc input with
    | Ok x -> return x
    | Error msg -> return! DecodeFailure(input, msg) |> Error
  }

  type DecodeFunc<'a> = string -> Result<'a, string>

  type Decoding<'a> =
  | PayloadRequired of decodeFunc : DecodeFunc<'a> * payloadO : string option
  | NoPayloadRequired of Events.EventReceived

  let inline decode<'a>  func =
    match func with
    | PayloadRequired (func, payload) ->
      match payload with
      | Some p -> func p |> mapDecodeError p
      | None -> PayloadMissing |> Error
    | NoPayloadRequired e -> Ok e

  type EventContext(eventMetadata : EventMetadata) =
    let mutable _eventReceived : Events.EventReceived option = None
    let mutable _eventsToSend : Events.EventSent list option = None
    let mutable _eventType : System.Type option = None
    let mutable _eventTypeValidation: (Events.EventReceived -> bool )option = None

    member this.EventMetadata = eventMetadata
    member this.EventReceived = _eventReceived

    member this.TryBindEventAsync = asyncResult {
      let decoder =
        let event = eventMetadata.Event.ToLowerInvariant()
        match event with
        | Events.EventNames.KeyDown ->
          let func = Types.tryDecodePayload Types.Received.KeyPayload.Decoder Events.EventReceived.KeyDown
          fun p -> decode (PayloadRequired (func, p))
        | Events.EventNames.SystemDidWakeUp ->
          fun _ -> decode (NoPayloadRequired Events.SystemWakeUp)
        | _ ->
          fun _ -> UnknownEventType event |> Error
      return! decoder eventMetadata.Payload
    }

    member this.ValidateEventType e = 
      if _eventTypeValidation.IsSome then
        (Option.get _eventTypeValidation) e
      else false

    member this.SetEventType(t : System.Type) = _eventType <- Some t

    member this.AddSendEvent e =
      match _eventsToSend with
      | None ->
        _eventsToSend <- Some [e]
      | Some es ->
        _eventsToSend <- Some (e :: es)

    member this.GetEventsToSend() = 
      match _eventsToSend with
      | Some x -> x
      | None -> []

  let addSendEvent e (ctx : EventContext) = 
    ctx.AddSendEvent e

  let setEventType t (ctx : EventContext) =
    ctx.SetEventType t

  let flow (ctx : EventContext) = Some ctx |> Async.lift

