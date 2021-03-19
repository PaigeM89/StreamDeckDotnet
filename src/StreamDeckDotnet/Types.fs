namespace StreamDeckDotnet

open System

[<AutoOpen>]
module Types =
  open Newtonsoft.Json
  open Newtonsoft.Json.Linq
  open Thoth.Json.Net
  open FsToolkit.ErrorHandling
  open StreamDeckDotnet.Logging
  open StreamDeckDotnet.Logger
  open StreamDeckDotnet.Logger.Operators

  let private logger = LogProvider.getLoggerByName("StreamDeckDotnet.Types")

  let (|InvariantEqual|_|) (str: string) arg =
    if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0 then Some() else None

  /// Event names in all lower case for invariant case equals checks.
  module EventNames =
    [<Literal>]
    let DidReceiveSettings = "didreceivesettings"
    [<Literal>]
    let DidReceiveGlobalSettings = "didreceiveglobalsettings"
    [<Literal>]
    let KeyDown = "keydown"
    [<Literal>]
    let KeyUp = "keyup"
    [<Literal>]
    let SystemDidWakeUp = "systemdidwakeup"

  /// Attempts to decode the payload to the target type constructer using the given Decoder.
  let tryDecodePayload decoder targetType payload =
    result {
      let! payload = Decode.fromString decoder payload
      return targetType payload
    }

  /// Stores all data & metadata relevant to an event recieved by streamdeck. 
  /// Events to send back to streamdeck are stored in the context and are sent after the event is fully processed.
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

  //Encodes the payload with a wrapper containing metadata
  let encodeWithWrapper (context: string option) (device : string option) event payload =
    Encode.object [
      if context.IsSome then "context", Option.get context |> Encode.string
      if device.IsSome then "device", Option.get device |> Encode.string
      "event", Encode.string event
      // encode the payload as an object, purge the \n that gets added when tostring()'d,
      // and encode again to generate a string containing a json object.
      // e.g., "{ \"property1\" : \"value\", \"propety2\": 0 }"
      "payload", ((Encode.object payload).ToString().Replace("\n", "")) |> Encode.string
    ]

  /// <summary>Events received from the stream deck.</summary>
  /// These events have an `Encode` method because they are referenced by `StreamDeck.Mimic`,
  /// but normal plugin writing should not need to encode these event types.
  module Received =

    let toJToken (s : string) =
      !! "Parsing string '{s}' into jtoken" >>!- ("s", s) |> logger.trace
      let token = JToken.Parse(s)
      !! "Token created is {t}" >>!+ ("t", token) |> logger.trace
      token

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

      member this.Encode() = [
        "column", Encode.int this.Column
        "row", Encode.int this.Row
      ]

    type KeyPayload = {
      Settings: JToken
      Coordinates: Coordinates
      State: uint
      UserDesiredState: uint
      IsInMultiAction: bool
    } with
      static member Decoder : Decoder<KeyPayload> =
        Decode.object (fun get -> {
          Settings = get.Required.Field "settings" Decode.string |> toJToken
          Coordinates = get.Required.Field "coordinates" Coordinates.Decoder
          State = get.Required.Field "state" Decode.uint32
          UserDesiredState = get.Required.Field "userDesiredState" Decode.uint32
          IsInMultiAction = get.Required.Field "isInMultiAction" Decode.bool
        })

      member this.Encode context device =
        let payload = [
          "settings", Encode.string (this.Settings.ToString())
          "coordinates", Encode.object (this.Coordinates.Encode())
          "state", Encode.uint32 this.State
          "userDesiredState", Encode.uint32 this.UserDesiredState
          "isInMultiAction", Encode.bool this.IsInMultiAction
        ]
        encodeWithWrapper context device "keyDown" payload

    /// Settings bag for the instance of the action.
    type SettingsPayload = {
      Settings : JToken
      Coordinates : Coordinates
      IsInMultiAction : bool
    } with
      static member Decoder : Decoder<SettingsPayload> =
        Decode.object (fun get -> {
          Settings = get.Required.Field "settings" Decode.string |> JObject
          Coordinates = get.Required.Field "coordinates" Coordinates.Decoder
          IsInMultiAction = get.Required.Field "isInMultiAction" Decode.bool
        })

      member this.Encode context device =
        let payload = [
          "settings", Encode.string (this.Settings.ToString())
          "coordinates", Encode.object (this.Coordinates.Encode())
          "isInMultiAction", Encode.bool this.IsInMultiAction
        ]
        encodeWithWrapper context device "didReceiveSettings" payload

    /// Settings bag for the plugin across all instances.
    type GlobalSettingsPayload = {
      Settings : JToken
    } with
      static member Decoder : Decoder<GlobalSettingsPayload> =
        Decode.object (fun get -> {
          Settings = get.Required.Field "settings" Decode.string |> JObject
        })

      member this.Encode context device =
        let payload = [
          "settings", Encode.string (this.Settings.ToString())
        ]
        encodeWithWrapper context device "didReceiveGlobalSettings" payload

    // type WillAppearPayload = {
    //   Settings : JToken
    //   Coordinates : Coordinates
    //   State : int
    //   IsInMultiAction : bool
    // } with
    //   static member Decoder : Decoder<GlobalSettingsPayload> = 


    /// Events sent from the stream deck application to the plugin.
    type EventReceived =
    /// Received when a Stream Deck key is pressed.
    | KeyDown of payload: KeyPayload
    /// Received when a Stream Deck key is released after being pressed.
    | KeyUp of payload: KeyPayload
    /// Received after sending a `getSettings` to get the persistent data stored for this action.
    | DidReceiveSettings of payload : SettingsPayload
    /// Received after sending a `getGlobalSettings` to retrieve global persistent data.
    | DidReceiveGlobalSettings of payload : GlobalSettingsPayload
    /// Received when an instance of an action is going to appear on a streamdeck, such as when
    /// plugging in the device or when entering a folder containing that action instance.
    | WillAppear
    /// Received when the computer wakes up from sleep.
    /// This event could appear multiple times. There is no guarantee the device is available.
    | SystemWakeUp // no payload on this event
      with
        /// The name of the event in CamelCase.
        member this.GetName() =
          match this with
          | KeyDown _ -> "KeyDown"
          | KeyUp _ -> "KeyUp"
          | DidReceiveSettings _ -> "DidReceiveSettings"
          | DidReceiveGlobalSettings _ -> "DidReceiveGlobalSettings"
          | WillAppear _ -> "WillAppear"
          | SystemWakeUp -> "SystemWakeUp"

        /// Encodes this event with the optional context & device information.
        member this.Encode context device =
          match this with
          | DidReceiveSettings payload -> 
            payload.Encode context device |> Thoth.Json.Net.Encode.toString 0
          | KeyDown payload ->
            payload.Encode context device |> Thoth.Json.Net.Encode.toString 0
          | KeyUp payload ->
            payload.Encode context device |> Encode.toString 0
          | _ -> ""

  /// Events sent to the stream deck.
  module Sent =
    open Newtonsoft.Json.Linq

    type LogMessagePayload = {
      Message : JValue
    } with
        member this.Encode context device =
          let payload = [
            "message", Encode.string (string this.Message)
          ]
          encodeWithWrapper context device "logMessage" payload
    
    type RegisterPluginPayload = {
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

    type EventSent =
    | RegisterPlugin of payload: RegisterPluginPayload
    | InRegisterEvent of pluginUUID : Guid
    | LogMessage of LogMessagePayload
    with
      member this.Encode context device =
        match this with
        | RegisterPlugin payload -> payload.Encode() |> Thoth.Json.Net.Encode.toString 0
        | InRegisterEvent id -> Thoth.Json.Net.Encode.toString 0 (JValue(id))
        | LogMessage payload ->
          Thoth.Json.Net.Encode.toString 0 (payload.Encode context device)


  let createLogEvent (msg : string) =
    let payload = { Sent.LogMessagePayload.Message = JValue(msg) }
    Sent.LogMessage payload

