namespace StreamDeckDotnet

open System

module internal Encode =
#if FABLE_COMPILER
  open Thoth.Json
#else
  open Thoth.Json.Net
#endif

  /// Encodes a guid in a way the streamdeck will read it, because the stream deck application
  /// apparently does string matching instead of anything correct.
  let encodeGuid (g : Guid) = g.ToString("N").ToUpperInvariant()

  /// Encodes the value of the string, or an empty string
  let stringOption so =
    so |> Option.defaultValue "" |> Encode.string

  let maybeEncode n o f =
    match o with
    | Some x -> [ n, f x]
    | None -> []

  let jsonObject (v : JsonValue) = v

module internal Decode =

  /// "Decodes" a jtoken value by simply returning that value.
  let jToken = fun _ v -> Ok v

[<AutoOpen>]
module Types =
  open Newtonsoft.Json
  open Newtonsoft.Json.Linq
#if FABLE_COMPILER
  open Thoth.Json
#else
  open Thoth.Json.Net
#endif
  open FsToolkit.ErrorHandling
  open StreamDeckDotnet.Logging
  open StreamDeckDotnet.Logger
  open StreamDeckDotnet.Logger.Operators

  let rec private logger = LogProvider.getLoggerByQuotation <@ logger @>

  let (|InvariantEqual|_|) (str: string) arg =
    if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0 then Some() else None

  /// Event names in camelCase, which matches the case used in messages from the stream deck application.
  module EventNames =
    [<Literal>]
    let DidReceiveSettings = "didReceiveSettings"
    [<Literal>]
    let DidReceiveGlobalSettings = "didReceiveGlobalSettings"
    [<Literal>]
    let KeyDown = "keyDown"
    [<Literal>]
    let KeyUp = "keyUp"
    [<Literal>]
    let WillAppear = "willAppear"
    [<Literal>]
    let WillDisappear = "willDisappear"
    [<Literal>]
    let TitleParametersDidChange = "titleParametersDidChange"
    [<Literal>]
    let DeviceDidConnect = "deviceDidConnect"
    [<Literal>]
    let DeviceDidDisconnect = "deviceDidDisconnect"
    [<Literal>]
    let ApplicationDidLaunch = "applicationDidLaunch"
    [<Literal>]
    let ApplicationDidTerminate = "applicationDidTerminate"
    [<Literal>]
    let SystemDidWakeUp = "systemDidWakeUp"
    [<Literal>]
    let PropertyInspectorDidAppear = "propertyInspectorDidAppear"
    [<Literal>]
    let PropertyInspectorDidDisappear = "propertyInspectorDidDisappear"
    [<Literal>]
    let SendToPlugin = "sendToPlugin"
    [<Literal>]
    let OpenUrl = "openUrl"
    [<Literal>]
    let SetTitle = "setTitle"
    [<Literal>]
    let SetImage = "setImage"
    [<Literal>]
    let SetState = "setState"
    [<Literal>]
    let SwitchToProfile = "switchToProfile"
    [<Literal>]
    let SetSettings = "setSettings"
    [<Literal>]
    let GetSettings = "getSettings"
    [<Literal>]
    let SetGlobalSettings = "setGlobalSettings"
    [<Literal>]
    let GetGlobalSettings = "getGlobalSettings"
    [<Literal>]
    let ShowAlert = "showAlert"
    [<Literal>]
    let ShowOk = "showOk"
    [<Literal>]
    let SendToPropertyInspector = "sendToPropertyInspector"



  /// Attempts to decode the payload to the target type constructer using the given Decoder.
  let tryDecodePayload decoder targetType payload =
    result {
      let! payload = Decode.fromString decoder payload
      //let! payload = Decode. decoder payload
      return targetType payload
    }

  let tryDecodePayloadJson decoder targetType payload =
    result {
      !! "Attempting to decode json payload of '{payload}'"
      >>!+ ("payload", payload)
      |> logger.info
      let! payload = Decode.fromValue "" decoder payload
      return targetType payload
    }


  /// Stores all data & metadata relevant to an event recieved by streamdeck.
  /// Events to send back to streamdeck are stored in the context and are sent after the event is fully processed.
  type EventMetadata = {
    /// The URI of the Action. Eg, "com.elgato.example.action"
    Action : string option

    /// A string describing the event, eg "didReceiveSettings"
    Event : string

    /// A unique, opaque, non-controlled ID for an action's instance.
    /// This identifies the specific button being pressed for a given action,
    /// which is relevant for actions that allow multiple instances.
    Context : string option

    /// A unique, opaque, non-controlled ID for the device that is sending or receiving the action.
    Device : string option

    /// The raw JSON describing the payload for this event.
    Payload : JToken option
  } with
      static member Decoder : Decoder<EventMetadata> =
        Decode.object (fun get -> {
          Action = get.Optional.Field "action" Decode.string
          Event = get.Required.Field "event" Decode.string
          Context = get.Optional.Field "context" Decode.string
          Device = get.Optional.Field "device" Decode.string
          // "decode" the payload as it is, keeping it a jtoken
          Payload = get.Optional.Field "payload" Decode.jToken
            //(Decode.object (fun token -> JToken.FromObject(token)))
        })

  /// Creates a new `EventMetadata` that is built from decoding the given string, or returns an error message on decode failure.
  let decodeEventMetadata (str : string) =
    Decode.fromString EventMetadata.Decoder str

  /// Encodes an Event without a payload.
  let encodeWithoutPayload (context: string option) (device : string option) event =
    Encode.object [
      if context.IsSome then "context", Option.get context |> Encode.string
      if device.IsSome then "device", Option.get device |> Encode.string
      "event", Encode.string event
    ]

  /// Encodes an Event with a payload that is just the given JToken.
  let encodeWithJson (context: string option) (device : string option) event (json : JToken) =
    Encode.object [
      if context.IsSome then "context", Option.get context |> Encode.string
      if device.IsSome then "device", Option.get device |> Encode.string
      "event", Encode.string event
      "payload", json
    ]

  //Encodes the payload with a wrapper containing metadata
  let encodeWithWrapper (context: string option) (device : string option) event payload =
    Encode.object [
      if context.IsSome then "context", Option.get context |> Encode.string
      if device.IsSome then "device", Option.get device |> Encode.string
      "event", Encode.string event
      // encode the payload as an object
      "payload", Encode.object payload
    ]

  /// <summary>Events received from the stream deck.</summary>
  /// These events have an `Encode` method because they are referenced by `StreamDeck.Mimic`,
  /// but normal plugin writing should not need to encode these event types.
  module Received =

    let toJToken (s : string) =
      if String.IsNullOrWhiteSpace s then
        !! "string is null or whitespace, cannot create jtoken from it. Creating empty token." |> logger.info
        JToken.Parse("{}")
      else
        !! "Parsing string '{s}' into jtoken" >>!- ("s", s) |> logger.trace
        let token = JToken.Parse(s)
        !! "Token created is {t}" >>!+ ("t", token) |> logger.trace
        token

    /// The (x,y) location of an instance of an action. The top left is [0,0], with the bottom right being [xMax, yMax].
    /// On a standard 15-key stream deck, the bottom right is [4,2].
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
      State: uint option
      UserDesiredState: uint option
      IsInMultiAction: bool
    } with
      static member Decoder : Decoder<KeyPayload> =
        Decode.object (fun get -> {
          Settings = get.Required.Field "settings" Decode.jToken
          Coordinates = get.Required.Field "coordinates" Coordinates.Decoder
          State = get.Optional.Field "state" Decode.uint32
          UserDesiredState = get.Optional.Field "userDesiredState" Decode.uint32
          IsInMultiAction = get.Required.Field "isInMultiAction" Decode.bool
        })

      member this.Encode context device actionName =
        let payload = [
          "settings", this.Settings
          "coordinates", Encode.object (this.Coordinates.Encode())
          yield! Encode.maybeEncode "state" this.State Encode.uint32
          yield! Encode.maybeEncode "userDesiredState" this.UserDesiredState Encode.uint32
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
        | KeyDown p -> p.Encode context device EventNames.KeyDown
        | KeyUp p -> p.Encode context device EventNames.KeyUp

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
          Settings = get.Required.Field "settings" Decode.jToken
          Coordinates = get.Required.Field "coordinates" Coordinates.Decoder
          IsInMultiAction = get.Required.Field "isInMultiAction" Decode.bool
        })

      member this.Encode context device =
        let payload = [
          "settings", Encode.string (this.Settings.ToString())
          "coordinates", Encode.object (this.Coordinates.Encode())
          "isInMultiAction", Encode.bool this.IsInMultiAction
        ]
        encodeWithWrapper context device EventNames.DidReceiveSettings payload

    /// Settings bag for the plugin across all instances.
    type GlobalSettingsPayload = {
      Settings : JToken
    } with
      static member Decoder : Decoder<GlobalSettingsPayload> =
        Decode.object (fun get -> {
          Settings = get.Required.Field "settings" Decode.jToken
        })

      member this.Encode context device =
        let payload = [
          "settings", Encode.string (this.Settings.ToString())
        ]
        encodeWithWrapper context device EventNames.DidReceiveGlobalSettings payload

    /// Received when an instance of an action will appear or will disappear from the stream deck,
    /// such as when the user changes profiles or opens/leaves a folder.
    type AppearPayload = {
      Settings : JToken
      Coordinates : Coordinates
      // the docs don't say this is optional but it's not found in at least 1 real-world example
      State : int option
      IsInMultiAction : bool
    } with
      static member Decoder : Decoder<AppearPayload> =
        Decode.object (fun get -> {
          Settings = get.Required.Field "settings" Decode.jToken
          Coordinates = get.Required.Field "coordinates" Coordinates.Decoder
          State = get.Optional.Field "state" Decode.int
          IsInMultiAction = get.Required.Field "isInMultiAction" Decode.bool
        })

      member this.Encode context device =
        let payload = [
          "settings", this.Settings
          "coordinates", Encode.object (this.Coordinates.Encode())
          yield! Encode.maybeEncode "state" this.State Encode.int
          "isInMultiAction", Encode.bool this.IsInMultiAction
        ]
        encodeWithWrapper context device EventNames.WillAppear payload

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
          Settings = get.Required.Field "settings" Decode.jToken
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
        encodeWithWrapper context device EventNames.TitleParametersDidChange payload

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
        encodeWithWrapper context device EventNames.DeviceDidConnect payload

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
        | Launch p -> p.Encode context device EventNames.ApplicationDidLaunch
        | Terminate p -> p.Encode context device EventNames.ApplicationDidTerminate

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
    /// <summary>Received after sending a `getSettings` to get the persistent data stored for this action.</summary>
    /// <remarks>Can also be received if the Property Inspector updates settings for this action instance.</remarks>
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
    | SendToPlugin of payload : JToken
    /// <summary>Received by the Property Inspector when the plugin sends a `SendToPropertyInspector` event.</summary>
    /// <remarks></remarks>
    | SendToPropertyInspector of payload : JToken
      with
        /// The name of the event in camelCase.
        member this.GetName() =
          match this with
          | KeyDown _ -> EventNames.KeyDown
          | KeyUp _ -> EventNames.KeyUp
          | DidReceiveSettings _ -> EventNames.DidReceiveSettings
          | DidReceiveGlobalSettings _ -> EventNames.DidReceiveGlobalSettings
          | WillAppear _ -> EventNames.WillAppear
          | WillDisappear _ -> EventNames.WillDisappear
          | TitleParametersDidChange _ -> EventNames.TitleParametersDidChange
          | DeviceDidConnect _ -> EventNames.DeviceDidConnect
          | DeviceDidDisconnect -> EventNames.DeviceDidDisconnect
          | ApplicationDidLaunch _ -> EventNames.ApplicationDidLaunch
          | ApplicationDidTerminate _ -> EventNames.ApplicationDidTerminate
          | SystemDidWakeUp -> EventNames.SystemDidWakeUp
          | PropertyInspectorDidAppear -> EventNames.PropertyInspectorDidAppear
          | PropertyInspectorDidDisappear -> EventNames.PropertyInspectorDidDisappear
          | SendToPlugin _ -> EventNames.SendToPlugin
          | SendToPropertyInspector _ -> EventNames.SendToPropertyInspector

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
          | DeviceDidDisconnect ->
            encodeWithoutPayload context device  EventNames.DeviceDidDisconnect|> Encode.toString 0
          | ApplicationDidLaunch payload ->
            payload.Encode context device |> Encode.toString 0
          | ApplicationDidTerminate payload ->
            payload.Encode context device |> Encode.toString 0
          | SystemDidWakeUp ->
            encodeWithoutPayload context device  EventNames.DeviceDidDisconnect|> Encode.toString 0
          | PropertyInspectorDidAppear ->
            encodeWithoutPayload context device  EventNames.DeviceDidDisconnect|> Encode.toString 0
          | PropertyInspectorDidDisappear ->
            encodeWithoutPayload context device  EventNames.DeviceDidDisconnect|> Encode.toString 0
          | SendToPlugin payload ->
            encodeWithJson context device EventNames.SendToPlugin payload |> Encode.toString 0
          | SendToPropertyInspector payload ->
            encodeWithJson context device EventNames.SendToPropertyInspector payload |> Encode.toString 0

  /// Events sent to the stream deck application.
  module Sent =
    open Newtonsoft.Json.Linq

    /// Some messages can be set to affect only the hardware, only the software, or both.
    type Target =
    | HardwareAndSoftware = 0
    | Hardware = 1
    | Software = 2

    let private TargetToInt (t : Target) =
      match t with
      | Target.HardwareAndSoftware -> 0
      | Target.Hardware -> 1
      | Target.Software -> 2
      | _ -> 0

    let private TargetFromInt (v : int) =
      match v with
      | 1 -> Target.Hardware
      | 2 -> Target.Software
      | _ -> Target.HardwareAndSoftware

    type LogMessagePayload = {
      Message : string
    } with
        member this.Encode context device =
          let payload = [
            "message", Encode.string (string this.Message)
          ]
          encodeWithWrapper context device "logMessage" payload

        static member Decoder : Decoder<LogMessagePayload> =
          Decode.object (fun get -> {
            Message = get.Required.Field "message" Decode.string
          })

    type RegisterPluginPayload = {
      Event : string
      PluginGuid : Guid
    } with
      member this.Encode() =
        Encode.object [
          "event", Encode.string this.Event
          "uuid", Encode.string (this.PluginGuid.ToString("N").ToUpperInvariant())
        ]
      static member Create event id =
        {
          Event = event
          PluginGuid = id
        }

    type OpenUrlPayload = {
      Url : string
    } with
      member this.Encode context device =
        [ "url", Encode.string this.Url ]
        |> encodeWithWrapper context device EventNames.OpenUrl

      static member Decoder : Decoder<OpenUrlPayload> =
        Decode.object(fun get -> {
          Url = get.Required.Field "url" Decode.string
        })


    type SetTitlePayload = {
      /// If None, the user-entered title will be set.
      Title : string option
      /// Specify if the title will change on the hardware, software, or both.
      Target : Target
      /// If None, the title will apply to all states of the action.
      State : int option
    } with
      member this.Encode context device =
        [
          yield! Encode.maybeEncode "title" this.Title (Encode.string)
          "target", Encode.int (this.Target |> TargetToInt)
          yield! Encode.maybeEncode "state" this.State (Encode.int)
        ]
        |> encodeWithWrapper context device EventNames.SetTitle

      static member Decoder : Decoder<SetTitlePayload> =
        Decode.object(fun get -> {
          Title = get.Optional.Field "title" Decode.string
          Target = get.Required.Field "target" Decode.int |> TargetFromInt
          State = get.Optional.Field "state" Decode.int
        })

    type SetImagePayload = {
      /// A base-64 encoded string for the image, or an SVG image
      Image : string
      /// Specify if the title will change on the hardware, software, or both.
      Target : Target
      /// If None, the title will apply to all states of the action.
      State : int option
    } with
      member this.Encode context device =
        [
          "image", Encode.string this.Image
          "target", Encode.int (this.Target |> TargetToInt)
          yield! Encode.maybeEncode "state" this.State (Encode.int)
        ]
        |> encodeWithWrapper context device EventNames.SetImage

      member this.Decoder : Decoder<SetImagePayload> =
        Decode.object(fun get -> {
          Image = get.Required.Field "image" Decode.string
          Target = get.Required.Field "target" Decode.int |> TargetFromInt
          State = get.Optional.Field "state" Decode.int
        })

    type SetStatePayload = {
      State : int
    } with
      member this.Encode context device =
        [
          "state", Encode.int this.State
        ]
        |> encodeWithWrapper context device EventNames.SetState

      static member Decoder : Decoder<SetStatePayload> =
        Decode.object(fun get -> {
          State = get.Required.Field "state" Decode.int
        })

    type SwitchToProfilePayload = {
      Profile : string
    } with
      member this.Encode context device =
        [
          "profile", Encode.string this.Profile
        ]
        |> encodeWithWrapper context device EventNames.SwitchToProfile

      static member Decoder : Decoder<SwitchToProfilePayload> =
        Decode.object(fun get -> {
          Profile = get.Required.Field "profile" Decode.string
        })



    /// Events sent from this plugin to the stream deck application.
    type EventSent =
    /// <summary>Sent after the plugin web socket has been initialized.</summary>
    /// <remarks>This event is sent automatically by the web socket handler and does not need to be sent manually.</remarks>
    | RegisterPlugin of payload: RegisterPluginPayload
    //| InRegisterEvent of pluginUUID : Guid
    /// <summary>Send messages to the Stream Deck to add to the logs.</summary>
    /// <remarks>
    /// This only logs a simple string and does not support structured logging.
    /// Logs are found in `~/Library/Logs/StreamDeck/` on macOS and `%appdata%\Elgato\StreamDeck\logs\` on Windows.
    ///</remarks>
    | LogMessage of LogMessagePayload
    /// <summary>Send a json object of settings to be persisted for this action instance in the stream deck application.</summary>
    /// <remarks>
    /// Sending this will automatically send a `DidReceiveSettings` callback to the Property Inspector with the new settings.
    /// Similarly, if the Property Inspector updates settings, then this plugin will receive the updated settings.
    /// </remarks>
    | SetSettings of payload : JToken
    /// <summary>Request the persistent data for this action instance from the stream deck application.</summary>
    /// <remarks>The stream deck application will respond with a `DidReceiveSettings` event.</remarks>
    | GetSettings
    /// <summary>Send a json object of the settings to be persisted for all instances of this plugin.</summary>
    /// <remarks>
    /// This can also be set via the Property Inspector. Setting this will send a `DidReceiveGlobalSettings` event
    /// to the Property Inspector.
    ///
    /// These settings will be saved to the Keychain on macOS and to the Cerdential Store on Windows. This is useful for storing a shared token,
    /// for example.
    /// </remarks>
    | SetGlobalSettings of payload : JToken
    /// <summary>Request the global persistent data for (meaning "relevant to", not "for each") all instances of this action.</summary>
    | GetGlobalSettings
    /// <summary>Open the given URL on the default system browser.</summary>
    | OpenUrl of payload : OpenUrlPayload
    /// <summary>Update the title for an instance of the action.</summary>
    /// <remarks>The title must already be set to visible for the change to be visible</remarks>
    | SetTitle of paylod : SetTitlePayload
    /// <summary>Change the image displayed by an instance of the action.</summary>
    | SetImage of payload : SetImagePayload
    /// Temporarily show an alert icon on the image for an instance of the action.
    | ShowAlert
    /// Temporarily show an OK checkmark on the image for an instance of the action.
    | ShowOk
    /// Change the state for an instance of the action for an action that supports multiple states.
    | SetState of payload : SetStatePayload
    /// Tell the stream deck application to switch to one of the predefined profiles from the `manifest.json`.
    | SwitchToProfile of payload : SwitchToProfilePayload
    /// Send a payload to the Property Inspector for an instance of the action.
    | SendToPropertyInspector of payload : JToken
    with
      /// Encodes this event to a json-ified string to send to the stream deck application.
      /// Events are encoded automatically when the web socket finishes handling an event.
      member this.Encode context device =
        let encode x = Encode.toString 0 x
        match this with
        | RegisterPlugin payload -> payload.Encode() |> encode
        | LogMessage payload -> payload.Encode context device |> encode
        | SetSettings payload -> encodeWithWrapper context device EventNames.SetSettings ["payload", payload] |> encode
        | GetSettings -> encodeWithWrapper context device EventNames.GetSettings [] |> encode
        | SetGlobalSettings payload -> encodeWithWrapper context device EventNames.SetGlobalSettings ["payload", payload] |> encode
        | GetGlobalSettings -> encodeWithWrapper context device EventNames.GetGlobalSettings [] |> encode
        | OpenUrl payload -> payload.Encode context device |> encode
        | SetTitle payload -> payload.Encode context device |> encode
        | SetImage payload -> payload.Encode context device |> encode
        | ShowAlert -> encodeWithWrapper context device EventNames.ShowAlert [] |> encode
        | ShowOk -> encodeWithWrapper context device EventNames.ShowOk [] |> encode
        | SetState payload -> payload.Encode context device |> encode
        | SwitchToProfile payload -> payload.Encode context device |> encode
        | SendToPropertyInspector payload -> encodeWithWrapper context device EventNames.SendToPropertyInspector [ "payload", payload ] |> encode

  /// Creates a Log event containing the given message.
  let createLogEvent (msg : string) =
    let payload = { Sent.LogMessagePayload.Message = msg }
    Sent.LogMessage payload

  /// Creates an Ok event to temporarily show an Ok icon on the action.
  let createOkEvent() = Sent.ShowOk
  /// Creates an Alert event to temporarily show an Alert icon on the action.
  let createAlertEvent() = Sent.ShowAlert

