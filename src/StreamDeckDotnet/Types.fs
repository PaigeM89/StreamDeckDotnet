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
  /// Received when the computer wakes up from sleep.
  /// This event could appear multiple times. There is no guarantee the device is available.
  | SystemWakeUp // no payload on this event

  type EventSent =
  | LogMessage of LogMessagePayload

  module EventNames =
    [<Literal>]
    let DidReceiveSettings = "didReceiveSettings"
    [<Literal>]
    let KeyDown = "keyDown"
    [<Literal>]
    let SystemDidWakeUp = "systemDidWakeUp"

  let createLogEvent (msg : string) = 
    let payload = { Types.Sent.LogMessagePayload.Message = JObject(msg) }
    LogMessage payload

module Context =
  open FsToolkit.ErrorHandling
  open Thoth.Json.Net

  type ActionFailure =
  | DecodeFailure of input : string * errorMsg : string
  | UnknownEventType of string
  | PayloadMissing

  let mapDecodeError input (res : Result<_, string>) = 
    match res with
    | Ok x -> Ok x
    | Error msg -> DecodeFailure(input, msg) |> Error

  type DecodeFunc<'a> = string -> Result<'a, string>

  type Decoding<'a> =
  | PayloadRequired of decodeFunc : DecodeFunc<'a> * payloadO : string option
  | NoPayloadRequired of Events.EventReceived
  // decodeFunc : DecodeFunc<'a>

  let inline decode<'a>  func =
    match func with
    | PayloadRequired (func, payload) ->
      match payload with
      | Some p -> func p |> mapDecodeError p
      | None -> PayloadMissing |> Error
    | NoPayloadRequired e -> Ok e

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

  type ActionContext = {
    ActionReceived : ActionReceived

    /// The event received from the stream deck, if any.
    EventReceived : Events.EventReceived option

    /// The event to send to the stream deck, if any.
    EventsToSend : Events.EventSent list option
  } with
    member this.TryBindEventAsync =
      asyncResult {
        let decoder = 
          let event = this.ActionReceived.Event.ToLowerInvariant()
          match event with
          | Events.EventNames.KeyDown ->
            let func = Types.tryDecodePayload Types.Received.KeyPayload.Decoder Events.EventReceived.KeyDown
            fun p -> decode (PayloadRequired (func, p))
          | Events.EventNames.SystemDidWakeUp ->
            fun _ -> decode (NoPayloadRequired Events.SystemWakeUp)
          | _ ->
            fun _ -> UnknownEventType event |> Error
        return! decoder this.ActionReceived.Payload
      }

  let fromActionReceived ar =
    {
      ActionReceived = ar
      EventReceived = None
      EventsToSend = None
    }

  let addSendEvent e ctx = async {
    match ctx.EventsToSend with
    | None ->
      return { ctx with EventsToSend = Some [e] } |> Some
    | Some x ->
      return { ctx with EventsToSend = Some (e :: x) } |> Some
  }

  let flow (ctx : ActionContext) = Some ctx |> Async.lift 

