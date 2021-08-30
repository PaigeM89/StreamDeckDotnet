namespace StreamDeck.Mimic

open System

module Statics =
  // taken from a real 15 key streamdeck
  let device = "device(@(1)[4057/96/AL44H1C18339]): 1.0.170133" |> Some
  let context = Guid.NewGuid().ToString("N") |> Some


module SendEvent =
  open StreamDeckDotnet
  open StreamDeckDotnet.Types.Received

  module EventMenuOptions =
      [<Literal>]
      let ExitMenu = "Exit Menu"
      [<Literal>]
      let KeyDown = "Key Down"
      [<Literal>]
      let KeyUp = "Key Up"
      [<Literal>]
      let WillAppear = "Will Appear"

  let sendEventList =
    [
      EventMenuOptions.ExitMenu
      EventMenuOptions.KeyDown
      EventMenuOptions.WillAppear
    ] |> List.toArray

  let metadataBuilder context event payload =
    {
      EventMetadata.Action = Some "org.streamdeckdotnet.mimic"
      EventMetadata.Event = event
      EventMetadata.Context = context
      EventMetadata.Device = None
      EventMetadata.Payload = payload()
    }

  let baseCoords = {
    Coordinates.Column = 0
    Coordinates.Row = 0
  }

  module EventBuilders =
    let buildKeyPayload() =
      {
        KeyPayload.Settings = Thoth.Json.Net.JsonValue.Parse("{}")
        KeyPayload.Coordinates = baseCoords
        KeyPayload.State = Some 0u
        KeyPayload.UserDesiredState = None
        KeyPayload.IsInMultiAction = false
      }

    let buildKeyDownEvent() =
      buildKeyPayload() |> Received.KeyPayloadDU.KeyDown |> EventReceived.KeyDown

    // this is based off a real payload found in the logs during testing
    let buildAppearPayload() =
      {
        AppearPayload.Coordinates = baseCoords
        AppearPayload.IsInMultiAction = false
        AppearPayload.Settings = Newtonsoft.Json.Linq.JToken.Parse("{}")
        AppearPayload.State = None
      }

    let buildWillAppearEvent() =
      buildAppearPayload() |> EventReceived.WillAppear

  let handleSendEventInput (input : string) =
    match input.ToLowerInvariant() with
    | InvariantEqual EventMenuOptions.KeyDown ->
      EventBuilders.buildKeyDownEvent() |> Some
    | InvariantEqual EventMenuOptions.WillAppear ->
      EventBuilders.buildWillAppearEvent() |> Some
    | _ -> None

