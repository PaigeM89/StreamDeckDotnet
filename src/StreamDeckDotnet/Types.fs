namespace StreamDeckDotnet

open System

module Types =
  open Newtonsoft.Json
  open Newtonsoft.Json.Linq
  open Thoth.Json.Net
  open FsToolkit.ErrorHandling

  let tryDecodePayload decoder targetType payload =
    result {
      let! payload = Decode.fromString decoder payload
      return targetType payload
    }

  type EventMetadata = {
    /// The URI of the Action. Eg, "com.elgato.example.action"
    Action : string option
    
    /// A string describing the event, eg "didReceiveSettings"
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
      static member Decoder : Decoder<EventMetadata> = 
        Decode.object (fun get -> {
          Action = get.Optional.Field "action" Decode.string
          Event = get.Required.Field "event" Decode.string
          Context = get.Optional.Field "context" Decode.string
          Device = get.Optional.Field "device" Decode.string
          Payload = get.Optional.Field "payload" Decode.string
        })
  
  let decodeEventMetadata (str : string) =
    Decode.fromString EventMetadata.Decoder str

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

    //Encodes the payload with a wrapper containing metadata
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
    
    type RegisterPlugin = {
      Event : string
      PluginGuid : Guid
    } with
      member this.Encode() =
        Encode.object [
          "event", Encode.string this.Event
          "uuid", Encode.guid this.PluginGuid 
        ]
      static member Create event id =
        {
          Event = event
          PluginGuid = id
        }

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
  | RegisterPlugin of payload: RegisterPlugin
  | InRegisterEvent of pluginUUID : Guid
  | LogMessage of LogMessagePayload
  with
    member this.Encode context device =
      match this with
      | RegisterPlugin payload -> payload.Encode() |> Thoth.Json.Net.Encode.toString 0
      | InRegisterEvent id -> Thoth.Json.Net.Encode.toString 0 (JValue(id))
      | LogMessage payload ->
        Thoth.Json.Net.Encode.toString 0 (payload.Encode context device)

  let (|InvariatnEqual|_|) (str: string) arg =
    if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0 then Some() else None

  module EventNames =
    [<Literal>]
    let DidReceiveSettings = "didreceivesettings"
    [<Literal>]
    let KeyDown = "keydown"
    [<Literal>]
    let KeyUp = "keyup"
    [<Literal>]
    let SystemDidWakeUp = "systemdidwakeup"

  let createLogEvent (msg : string) =
    let payload = { Types.Sent.LogMessagePayload.Message = JValue(msg) }
    LogMessage payload
