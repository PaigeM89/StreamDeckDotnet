namespace StreamDeck.Mimic

open System
open Spectre.Console

module Statics =
  let device = "device(@(1)[4057/96/AL44H1C18339]): 1.0.170133" |> Some
  let context = Guid.NewGuid().ToString("N") |> Some

module CLI =
  open StreamDeckDotnet
  let renderError (s : string) = AnsiConsole.Markup("[red]{0}[/]\n", s.EscapeMarkup())
  let renderInfo (s : string) = AnsiConsole.Markup("[aqua]{0}[/]\n", s.EscapeMarkup())
  let renderResponse (s : string) = AnsiConsole.Markup("[green]Response from plugin:[/]\n[lime]{0}[/]\n", s.EscapeMarkup())

  let menu = 
    [
      "Exit"
      "Check for message"
      "Send Event"
    ] |> List.toArray

  module Prompt =
    type Prompt = SelectionPrompt<string>
    let builder() = new SelectionPrompt<string>()

    let withTitle title (bldr : Prompt) = 
      bldr.Title <- title
      bldr

    let withPageSize size (bldr : Prompt) = 
      bldr.PageSize <- size
      bldr

    let withChoices menu (bldr : Prompt) = 
      bldr.AddChoices(menu)

  let renderMainMenu() =
    let prompt =
      Prompt.builder() |> Prompt.withTitle "Main Menu" |> Prompt.withPageSize 10 |> Prompt.withChoices menu
    AnsiConsole.Prompt(prompt)

    
  module SendEvent =
    open StreamDeckDotnet.Types
    open StreamDeckDotnet.Types.Received

    let sendEventList =
      [
        "Exit Menu"
        "Key Down"
      ] |> List.toArray

    let renderSendEventMenu() =
      let prompt =
        Prompt.builder()
        |> Prompt.withTitle "Send Event"
        |> Prompt.withPageSize 10
        |> Prompt.withChoices sendEventList

      AnsiConsole.Prompt prompt

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

    let buildKeyDownEvent() =
      {
        KeyPayload.Settings = jo()
        KeyPayload.Coordinates = baseCoords
        KeyPayload.State = 0u
        KeyPayload.UserDesiredState = 0u
        KeyPayload.IsInMultiAction = false
      } |> Events.EventReceived.KeyDown

    let handleSendEventInput (input : string) =
      let bldr event payload = metadataBuilder Statics.context event payload
      match input.ToLowerInvariant() with
      | "key down" ->
        buildKeyDownEvent() |> Some
      | _ -> None

    let pickAndBuildSendEvent() =
      renderSendEventMenu()
      |> handleSendEventInput

  type Commands =
  | Exit
  | ReturnToMenu
  | SendEvent of payload : Events.EventReceived option

  let inputToCommand (input : string) =
    match input.ToLowerInvariant() with
    | "exit" -> Exit
    | "check for message" -> ReturnToMenu
    | "send event" ->
      SendEvent.pickAndBuildSendEvent() |> SendEvent
    | _ ->
      renderError $"Unrecognized user input: ${input}"
      Exit

  let mainMenuLoop sendEventHandler =
    let loop() = renderMainMenu() |> inputToCommand
    let rec runLoop() =
      match loop() with
      | ReturnToMenu -> runLoop()
      | SendEvent e ->
        sendEventHandler e
        runLoop()
      | Exit -> ()
    
    runLoop()

