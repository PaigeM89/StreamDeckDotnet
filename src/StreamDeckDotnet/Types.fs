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

    let buildJObject (s : string) = JObject(s)

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

    type Wrapper<'a> = {
      Event : string
      Context : string option
      Device : string option
      Payload : 'a option
    } with
      static member Create(event : string) = {
        Event = event
        Context = None
        Device = None
        Payload = None
      }

    let encodeWithWrapper (context: string option) (device : string option) event payload =
      Encode.object [
        if context.IsSome then "context", Option.get context |> Encode.string
        if device.IsSome then "device", Option.get device |> Encode.string
        "event", Encode.string event
        "payload", Encode.object payload
      ]

    type LogMessagePayload = {
      Message : JValue
    } with
        member this.Encode context device =
          let payload = [
            "message", Encode.string (string this.Message)
          ]
          encodeWithWrapper context device "logMessage" payload

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
  with
    member this.Encode context device =
      match this with
      | LogMessage payload ->
        Thoth.Json.Net.Encode.toString 0 (payload.Encode context device)

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
    let payload = { Types.Sent.LogMessagePayload.Message = JValue(msg) }
    LogMessage payload
