namespace StreamDeckDotnet

module Types =
  //open System.Text.Json
  open Newtonsoft.Json
  open Newtonsoft.Json.Linq
  open Thoth.Json.Net
  open FsToolkit.ErrorHandling

  let tryDecodePayload decoder targetType payload =
    result {
      let! payload = Decode.fromString decoder payload
      return targetType payload
    }

  type ActionReceived = {
    /// The URI of the Action. Eg, "com.elgato.example.action"
    Action : string option
    
    /// A string describing the action, eg "didReceiveSettings"
    Event : string

    /// A unique, opaque, non-controlled ID for the instance's action.
    /// This identifies the specific button being pressed for a given action,
    /// which is relevant for actions that allow multiple instances.
    Context : string option
    
    /// A unique, opaque, non-controlled ID for the device that is sending or receiving the action.
    Device : string option

    /// The raw JSON describing the payload for this event.
    Payload : string option
  } with
      static member Decoder : Decoder<ActionReceived> = 
        Decode.object (fun get -> {
          Action = get.Optional.Field "action" Decode.string
          Event = get.Required.Field "event" Decode.string
          Context = get.Optional.Field "context" Decode.string
          Device = get.Optional.Field "device" Decode.string
          Payload = get.Optional.Field "payload" Decode.string
        })
  
  let decodeActionReceived (str : string) =
    Decode.fromString ActionReceived.Decoder str

  module Received = 

    let buildJObject (s : string) =
      JObject(s)

    type Coordinates = {
      Column: int
      Row: int
    } with
      static member Decoder : Decoder<Coordinates> =
        Decode.object (fun get -> {
            Column = get.Required.Field "column" Decode.int
            Row = get.Required.Field "row" Decode.int
          }
        )

    type KeyPayload = {
      Settings: JObject
      Coordinates: Coordinates
      State: uint
      UserDesiredState: uint
      IsInMultiAction: bool
    } with
      static member Decoder : Decoder<KeyPayload> =
        Decode.object (fun get -> {
          Settings = get.Required.Field "settings" Decode.string |> JObject
          Coordinates = get.Required.Field "coordinates" Coordinates.Decoder
          State = get.Required.Field "state" Decode.uint32
          UserDesiredState = get.Required.Field "userDesiredState" Decode.uint32
          IsInMultiAction = get.Required.Field "isInMultiAction" Decode.bool
        })

    type SettingsPayload = {
      Settings : JObject
      Coordinates : Coordinates
      IsInMultiAction : bool
    } with
      static member Decoder : Decoder<SettingsPayload> =
        Decode.object (fun get -> {
          Settings = get.Required.Field "settings" Decode.string |> JObject
          Coordinates = get.Required.Field "coordinates" Coordinates.Decoder
          IsInMultiAction = get.Required.Field "isInMultiAction" Decode.bool
        })

  module Sent =
    open Newtonsoft.Json.Linq

    type LogMessagePayload = {
      Message : JObject
    } with
        member this.Encode =
          Encode.object [
            "message", Encode.string (string this.Message)
          ]

        static member Create (msg : string) =
          { Message = JObject(msg) }

module Events =
  open Newtonsoft.Json.Linq
  open Types.Received
  open Types.Sent

  /// Events sent from the stream deck application to the plugin.
  type EventReceived =
  /// Recieved when a Stream Deck key is pressed.
  | KeyDown of payload: KeyPayload
  /// Received when a Stream Deck key is released after being pressed.
  | KeyUp of payload: KeyPayload
  | DidReceiveSettings of payload : SettingsPayload
  /// Received when the computer wakes up from sleep.
  /// This event could appear multiple times. There is no guarantee the device is available.
  | SystemWakeUp // no payload on this event
  
  with
    member this.GetName() =
      match this with
      | KeyDown _ -> "KeyDown"
      | KeyUp _ -> "KeyUp"
      | DidReceiveSettings _ -> "DidReceiveSettings"
      | SystemWakeUp -> "SystemWakeUp"

  type EventSent =
  | LogMessage of LogMessagePayload

  module EventNames =
    [<Literal>]
    let DidReceiveSettings = "didReceiveSettings"
    [<Literal>]
    let KeyDown = "keyDown"
    [<Literal>]
    let KeyUp = "keyUp"
    [<Literal>]
    let SystemDidWakeUp = "systemDidWakeUp"

  let createLogEvent (msg : string) = 
    let payload = { Types.Sent.LogMessagePayload.Message = JObject(msg) }
    LogMessage payload

module Context =
  open Types
  open FsToolkit.ErrorHandling
  open Thoth.Json.Net

  type ActionFailure =
  | DecodeFailure of input : string * errorMsg : string
  | UnknownEventType of eventName : string
  /// Returned when attempting to decode a payload for a type that does not have one, such as SystemWakeUp
  | NoPayloadForType of eventName : string
  | PayloadMissing
  | Placeholder 

  let inline mapDecodeError<'a> input (res : Result<_, string>) = 
    match res with
    | Ok x -> Ok x
    | Error msg -> DecodeFailure(input, msg) |> Error

  let tryDecode<'a> input decodeFunc : Result<'a, ActionFailure> = result {
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

  type ActionContext(actionReceived : ActionReceived) =
    let mutable _eventReceived : Events.EventReceived option = None
    let mutable _eventsToSend : Events.EventSent list option = None
    let mutable _eventType : System.Type option = None
    let mutable _eventTypeValidation: (Events.EventReceived -> bool )option = None

    member this.ActionReceived = actionReceived
    member this.EventReceived = _eventReceived

    member this.TryBindEventAsync = asyncResult {
      let decoder =
        let event = actionReceived.Event.ToLowerInvariant()
        match event with
        | Events.EventNames.KeyDown ->
          let func = Types.tryDecodePayload Types.Received.KeyPayload.Decoder Events.EventReceived.KeyDown
          fun p -> decode (PayloadRequired (func, p))
        | Events.EventNames.SystemDidWakeUp ->
          fun _ -> decode (NoPayloadRequired Events.SystemWakeUp)
        | _ ->
          fun _ -> UnknownEventType event |> Error
      return! decoder actionReceived.Payload
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

  let addSendEvent e (ctx : ActionContext) = 
    ctx.AddSendEvent e

  let setEventType t (ctx : ActionContext) =
    ctx.SetEventType t

  let flow (ctx : ActionContext) = Some ctx |> Async.lift 


