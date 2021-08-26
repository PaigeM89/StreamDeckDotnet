#r "/Users/maxpaige/git/StreamDeckDotnet/src/StreamDeckDotnet/bin/Debug/netstandard2.0/StreamDeckDotnet.dll"
#r "nuget: Newtonsoft.Json"
#r "nuget: Thoth.Json.Net"

open StreamDeckDotnet
open StreamDeckDotnet.Types
open StreamDeckDotnet.Types.Received

(*
  This is an example message as received from the stream deck (newlines added by me).
  I'm not sure why the whole thing is a string - I think that's something in the decoding.

  "{
      \"action\":\"org.streamdeckdotnet.example.plugin\",
      \"context\":\"65DEF7689CEFA90E1BB3EAE127C03E47\",
      \"device\":\"89BFC41A68A05A303471FEA4EE372FA8\",
      \"event\":\"willAppear\",
      \"payload\":{\"coordinates\":{\"column\":4,\"row\":2},\"isInMultiAction\":false,\"settings\":{}}
    }"

  We need to mirror that message.
  Note that payload is an object, not a string.

  "{
    \"device\":\"89BFC41A68A05A303471FEA4EE372FA8\",
    \"deviceInfo\":{
      \"name\":\"Stream Deck\",
      \"size\":{
        \"columns\":5,
        \"rows\":3
      },
      \"type\":0},
    \"event\":\"deviceDidConnect\"
  }"
*)

open Newtonsoft.Json
open Newtonsoft.Json.Linq

let willAppear =
  let payload = {
    Settings = JToken.Parse("{}")
    Coordinates = {
      Column = 4
      Row = 2
    }
    State = None // state is missing from the payload, even though it isn't marked optional
    IsInMultiAction = false
  }
  WillAppear payload

let thothEncoded = willAppear.Encode None None

let encoded = willAppear.Encode (Some "context") (Some "Device")

let newtonsoftEncoded = Newtonsoft.Json.Linq.JObject.FromObject willAppear

let sampleInput =
  """{
      "action":"org.streamdeckdotnet.example.plugin",
      "context":"65DEF7689CEFA90E1BB3EAE127C03E47",
      "device":"89BFC41A68A05A303471FEA4EE372FA8",
      "event":"willAppear",
      "payload":{"coordinates":{"column":4,"row":2},"isInMultiAction":false,"settings":{}}
    }"""

open Thoth.Json.Net


let jtokenDecoder : Decoder<JToken> = fun _ v -> Ok v

/// Stores all data & metadata relevant to an event recieved by streamdeck.
/// Events to send back to streamdeck are stored in the context and are sent after the event is fully processed.
type EventMetadata2 = {
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
        Payload = get.Optional.Field "payload" jtokenDecoder
          //(Decode.object (fun token -> JToken.FromObject(token)))
      })

let eventMetadata =
  Decode.fromString EventMetadata2.Decoder sampleInput

match eventMetadata with
| Ok metadata ->
  match metadata.Payload with
  | Some payload ->
    string payload |> printfn "payload is %s"
  | None -> printfn "no payload"
| Error e -> printfn "error: %A" e
