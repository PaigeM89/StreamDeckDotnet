namespace StreamDeckDotnet

open System

module internal Encode =
  open Thoth.Json.Net

  /// Encodes the value of the string, or an empty string
  let stringOption so =
    so |> Option.defaultValue "" |> Encode.string

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
    let WillAppear = "willappear"
    [<Literal>]
    let WillDisappear = "willdisappear"
    [<Literal>]
    let TitleParametersDidChange = "titleparametersdidchange"
    [<Literal>]
    let DeviceDidConnect = "devicedidconnect"
    [<Literal>]
    let DeviceDidDisconnect = "devicediddisconnect"
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

    /// The `KeyDown` and `KeyUp` payload properties. Both events have the same properties.
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

      member this.Encode context device actionName =
        let payload = [
          "settings", Encode.string (this.Settings.ToString())
          "coordinates", Encode.object (this.Coordinates.Encode())
          "state", Encode.uint32 this.State
          "userDesiredState", Encode.uint32 this.UserDesiredState
          "isInMultiAction", Encode.bool this.IsInMultiAction
        ]
        encodeWithWrapper context device actionName payload

    /// Wrapper for the `KeyPayload` to encode correctly between keydown and keyup event types.
    /// This wrapper should be nearly transparent to most calling cases.
    type KeyPayloadDU = 
    | KeyDown of KeyPayload
    | KeyUp of KeyPayload
    with
      static member Decoder : Decoder<KeyPayload> = KeyPayload.Decoder

      member this.Encode context device =
        match this with
        | KeyDown p -> p.Encode context device "keyDown"
        | KeyUp p -> p.Encode context device "keyUp"

      /// Get the payload for the key event. This loses any information on if this is a keydown or keyup event.
      member this.Payload =
        match this with
        | KeyDown p -> p
        | KeyUp p -> p

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

    /// Received when an instance of an action will appear or will disappear from the stream deck,
    /// such as when the user changes profiles or opens/leaves a folder.
    type AppearPayload = {
      Settings : JToken
      Coordinates : Coordinates
      State : int
      IsInMultiAction : bool
    } with
      static member Decoder : Decoder<AppearPayload> = 
        Decode.object (fun get -> {
          Settings = get.Required.Field "settings" Decode.string |> JObject
          Coordinates = get.Required.Field "coordinates" Coordinates.Decoder
          State = get.Required.Field "state" Decode.int
          IsInMultiAction = get.Required.Field "isInMultiAction" Decode.bool
        })

      member this.Encode context device = 
        let payload = [
          "settings", Encode.string (this.Settings.ToString())
          "coordinates", Encode.object (this.Coordinates.Encode())
          "state", Encode.int this.State
          "isInMultiAction", Encode.bool this.IsInMultiAction
        ]
        encodeWithWrapper context device "willAppear" payload

    /// Describes the markup used to display the title for an action.
    type TitleParameters = {
      FontFamily: string option
      FontSize : int
      FontStyle : string option
      FontUnderline : bool
      ShowTitle : bool
      /// The alignment of the title, eg "bottom"
      TitleAlignment : string option
      /// The color of the title as a hex value, eg "#ffffff"
      TitleColor : string option
    } with
      static member Decoder : Decoder<TitleParameters> =
        Decode.object (fun get -> {
          FontFamily = get.Optional.Field "fontFamily" Decode.string
          FontSize = get.Required.Field "fontSize" Decode.int
          FontStyle = get.Optional.Field "fontStyle" Decode.string
          FontUnderline = get.Required.Field "fontUnderline" Decode.bool
          ShowTitle = get.Required.Field "showTitle" Decode.bool
          TitleAlignment = get.Optional.Field "titleAlignment" Decode.string
          TitleColor = get.Optional.Field "titleColor" Decode.string
        })

      member this.Encode() = 
        [
          "fontFamily", Encode.stringOption this.FontFamily
          "fontSize", Encode.int this.FontSize
          "fontStyle", Encode.stringOption this.FontStyle
          "fondUnderline", Encode.bool this.FontUnderline
          "showTitle", Encode.bool this.ShowTitle
          "TitleAlignment", Encode.stringOption this.TitleAlignment
          "TitleColor", Encode.stringOption this.TitleColor
        ]

    /// Payload for the title parameters, title, or related settings changing.
    type TitleParametersPayload = {
      Coordinates : Coordinates
      Settings: JToken
      State : int
      Title : string option
      TitleParameters : TitleParameters
    } with
      static member Decoder : Decoder<TitleParametersPayload> =
        Decode.object (fun get -> {
          Coordinates = get.Required.Field "coordinates" Coordinates.Decoder
          Settings = get.Required.Field "settings" Decode.string |> JObject
          State = get.Required.Field "state" Decode.int
          Title = get.Optional.Field "title" Decode.string
          TitleParameters = get.Required.Field "titleParameters" TitleParameters.Decoder
        })

      member this.Encode context device =
        let payload = [
          "coordinates", Encode.object (this.Coordinates.Encode())
          "settings", Encode.string (this.Settings.ToString())
          "state", Encode.int this.State
          "title", Encode.stringOption this.Title
          "titleParameters", Encode.object (this.TitleParameters.Encode())
        ]
        encodeWithWrapper context device "titleParameterDidChange" payload

    /// The size of the device.
    type Size = {
      Columns : int
      Rows : int
    } with
      static member Decoder : Decoder<Size> =
        Decode.object (fun get -> {
          Columns = get.Required.Field "columns" Decode.int
          Rows = get.Required.Field "rows" Decode.int
        })

      member this.Encode() =
        [
          "columns", Encode.int this.Columns
          "rows", Encode.int this.Rows
        ]

    /// The possible device types.
    type DeviceType = 
    | StreamDeck = 0
    | StreamDeckMini = 1
    | StreamDeckXL = 2
    | StreamDeckMobile = 3
    | CorsairGKeys = 4
    
    /// Convert an integer value to a `DeviceType`. All unknown values are defaulted to a `StreamDeck`.
    let DeviceTypeFromInt v : DeviceType =
        match v with
        | 1 -> DeviceType.StreamDeckMini
        | 2 -> DeviceType.StreamDeckXL
        | 3 -> DeviceType.StreamDeckMobile
        | 4 -> DeviceType.CorsairGKeys
        | _ -> DeviceType.StreamDeck

    /// Convert a `DeviceType` to an integer value.
    let DeviceTypeToInt (v : DeviceType) =
      match v with
      | DeviceType.StreamDeckMini -> 1
      | DeviceType.StreamDeckXL -> 2
      | DeviceType.StreamDeckMobile -> 3
      | DeviceType.CorsairGKeys -> 4
      | _ -> 0

    /// Information about the device. Received during `deviceDidConnect` events.
    type DeviceInfoPayload = {
      Name : string
      Type : DeviceType
      Size : Size
    } with
      static member Decoder : Decoder<DeviceInfoPayload> =
        Decode.object (fun get -> {
          Name = get.Required.Field "name" Decode.string
          Type = get.Required.Field "type" Decode.int |> DeviceTypeFromInt
          Size = get.Required.Field "size" Size.Decoder
        })

      member this.Encode context device =
        let payload = [
          "name", Encode.string this.Name
          "type", Encode.int (this.Type |> DeviceTypeToInt)
          "size", Encode.object (this.Size.Encode())
        ]
        encodeWithWrapper context device "deviceDidConnect" payload

    /// Name of an application that has launched or terminated
    type ApplicationPayload = {
      Application : string
    } with
      static member Decoder : Decoder<ApplicationPayload> =
        Decode.object (fun get -> {
          Application = get.Required.Field "application" Decode.string
        })

      member this.Encode context device eventName =
        let payload = [
          "application", Encode.string this.Application
        ]
        encodeWithWrapper context device eventName payload

    /// Wrapper for `ApplicationPayload` so the payload can be shared between Launch and Terminate event types.
    type ApplicationPayloadDU =
    | Launch of payload : ApplicationPayload
    | Terminate of payload : ApplicationPayload
    with
      static member Decoder = ApplicationPayload.Decoder

      member this.Encode context device =
        match this with
        | Launch p -> p.Encode context device "applicationDidLaunch"
        | Terminate p -> p.Encode context device "applicationDidTerminate"

      member this.Payload =
        match this with
        | Launch p -> p
        | Terminate p -> p

    /// Events sent from the stream deck application to the plugin.
    type EventReceived =
    /// Received when a Stream Deck key is pressed.
    | KeyDown of payload: KeyPayloadDU
    /// Received when a Stream Deck key is released after being pressed.
    | KeyUp of payload: KeyPayloadDU
    /// Received after sending a `getSettings` to get the persistent data stored for this action.
    | DidReceiveSettings of payload : SettingsPayload
    /// Received after sending a `getGlobalSettings` to retrieve global persistent data.
    | DidReceiveGlobalSettings of payload : GlobalSettingsPayload
    /// <summary>Received when an instance of an action is going to appear on a streamdeck.
    /// </summary>
    /// <remarks>
    /// This is triggered when:
    /// * The stream deck application is started
    /// * The user switches between profiles
    /// * The user sets a key to use your action
    /// * The user navigates to a folder that contains your action
    /// </remarks>
    | WillAppear of payload : AppearPayload
    /// <summary>Received when an instance of an action will not be displayed on a streamdeck any more.</summary>
    /// <remarks>
    /// This is triggered when:
    /// * The user switches profiles
    /// * The user deletes an instance of the action
    /// * The user leaves a folder that contains your action
    /// </remarks>
    | WillDisappear of payload : AppearPayload
    /// <summary>
    /// Received when an instance of an action will have the settings for its Title changed.
    /// </summary>
    | TitleParametersDidChange of payload : TitleParametersPayload
    /// <summary>
    /// Received when a device is plugged into the computer.
    /// </summary>
    /// <remarks>It is currently unknown if the plugin will receive this event if it is not visible on the streamdeck.</remarks>
    | DeviceDidConnect of payload : DeviceInfoPayload
      /// <summary>
    /// Received when a device is unplugged from the computer.
    /// </summary>
    /// <remarks>It is currently unknown if the plugin will receive this event if it is not visible on the streamdeck.</remarks>
    | DeviceDidDisconnect
    /// <summary>
    /// Received when a monitored application is launched.
    /// </summary>
    /// <remarks>
    /// Monitored applications are set via the manifest.json.
    /// </remarks>
    | ApplicationDidLaunch of payload : ApplicationPayloadDU
    /// <summary>
    /// Received when a monitored application is terminated.
    /// </summary>
    /// <remarks>
    /// Monitored applications are set via the manifest.json.
    /// </remarks>
    | ApplicationDidTerminate of payload : ApplicationPayloadDU
    /// <summary>Received when the computer wakes up from sleep.</summary>
    /// <remarks>This event could appear multiple times. There is no guarantee the device is available.</remarks>
    | SystemDidWakeUp // no payload on this event
    /// <summary>Received when the Property Inspector appears on the stream deck.</summary>
    /// <remarks>This is sent when the user selects an action in the stream deck software.</remarks>
    | PropertyInspectorDidAppear
    /// <summary>Received when the Property Inspector is no longer visible on the stream deck.</summary>
    /// <remarks>This is sent when the user clicks off an action in the stream deck software.</remarks>
    | PropertyInspectorDidDisappear
    /// <summary>Received when the Property Inspector sends a `SendToPlugin` event.</summary>
    | SendToPlugin
      with
        /// The name of the event in CamelCase.
        member this.GetName() =
          match this with
          | KeyDown _ -> "KeyDown"
          | KeyUp _ -> "KeyUp"
          | DidReceiveSettings _ -> "DidReceiveSettings"
          | DidReceiveGlobalSettings _ -> "DidReceiveGlobalSettings"
          | WillAppear _ -> "WillAppear"
          | WillDisappear _ -> "WillDisappear"
          | TitleParametersDidChange _ -> "TitleParametersDidChange"
          | DeviceDidConnect _ -> "DeviceDidConnect"
          | DeviceDidDisconnect _ -> "DeviceDidDisconnect"
          | ApplicationDidLaunch _ -> "ApplicationDidLaunch"
          | ApplicationDidTerminate _ -> "ApplicationDidTerminate"
          | SystemDidWakeUp -> "SystemDidWakeUp"
          | PropertyInspectorDidAppear -> "PropertyInspectorDidAppear"
          | PropertyInspectorDidDisappear -> "PropertyInspectorDidDisappear"
          | SendToPlugin -> "SendToPlugin"

        /// <summary>
        /// Encodes this event with the optional context and device information.
        /// </summary>
        /// <remarks>
        /// Most plugins will not need to encode EventReceived types. This is primarily useful for testing.
        /// </remarks>
        member this.Encode context device =
          match this with
          | KeyDown payload ->
            payload.Encode context device |> Encode.toString 0
          | KeyUp payload ->
            payload.Encode context device |> Encode.toString 0
          | DidReceiveSettings payload -> 
            payload.Encode context device |> Encode.toString 0
          | DidReceiveGlobalSettings payload ->
            payload.Encode context device |> Encode.toString 0
          | WillAppear payload ->
            payload.Encode context device |> Encode.toString 0
          | WillDisappear payload ->
            payload.Encode context device |> Encode.toString 0
          | TitleParametersDidChange payload ->
            payload.Encode context device |> Encode.toString 0
          | DeviceDidConnect payload ->
            payload.Encode context device |> Encode.toString 0
          // | DeviceDidDisconnect ->
          //   payload.Encode context device |> Encode.toString 0
          | _ -> ""

  /// Events sent to the stream deck application.
  module Sent =
    open Newtonsoft.Json.Linq

    /// 
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

