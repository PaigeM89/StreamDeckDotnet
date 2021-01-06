namespace StreamDeckDotnet

module Types =
  //open System.Text.Json
  open Newtonsoft.Json
  open Newtonsoft.Json.Linq
  open Thoth.Json.Net
  open FsToolkit.ErrorHandling

  let tryDecode decoder targetType payload =
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
  /// Recieved when a Stream Deck key is pressed
  | KeyDown of payload: KeyPayload
  /// Received when a Stream Deck key is released after being pressed.
  | KeyUp of payload: KeyPayload

  type EventSent =
  | LogMessage of LogMessagePayload

  module EventNames =
    [<Literal>]
    let DidReceiveSettings = "didReceiveSettings"
    [<Literal>]
    let KeyDown = "keyDown"

  let createLogEvent (msg : string) = 
    let payload = { Types.Sent.LogMessagePayload.Message = JObject(msg) }
    LogMessage payload

module Context =
  open FsToolkit.ErrorHandling

  type ActionReceived = {
    /// The URI of the Action. Eg, "com.elgato.example.action"
    Action : string
    
    /// A string describing the action, eg "didReceiveSettings"
    Event : string

    /// A unique, opaque, non-controlled ID for the instance's action.
    /// This identifies the specific button being pressed for a given action,
    /// which is relevant for actions that allow multiple instances.
    Context : string
    
    /// A unique, opaque, non-controlled ID for the device that is sending or receiving the action.
    Device : string

    /// The raw JSON describing the payload for this event.
    Payload : string
  }

  type ActionContext = {
    ActionReceived : ActionReceived

    /// The event received from the stream deck, if any.
    EventReceived : Events.EventReceived option

    /// The event to send to the stream deck, if any.
    EventsToSend : Events.EventSent list option
  } with
    member this.TryBindEventAsync =
      async {
        let! result = asyncResult {
          let! decoder = 
            let event = this.ActionReceived.Event.ToLowerInvariant()
            match event with
            | Events.EventNames.KeyDown ->
              Ok (Types.tryDecode Types.Received.KeyPayload.Decoder Events.EventReceived.KeyDown)
            | _ ->
              Error $"Unknown event type: {event}"
          return! decoder this.ActionReceived.Payload
        }
        return result
      }
  
  let addSendEvent e ctx = async {
    match ctx.EventsToSend with
    | None ->
      return { ctx with EventsToSend = Some [e] } |> Some
    | Some x ->
      return { ctx with EventsToSend = Some (e :: x) } |> Some
  }

  let flow (ctx : ActionContext) = Some ctx |> Async.lift 

