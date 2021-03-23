namespace StreamDeck.Mimic

open System
open Spectre.Console

module CLI =
  open StreamDeckDotnet
  open StreamDeckDotnet.Types
  open StreamDeckDotnet.Types.Received

  let now() = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff")

  let renderError (s : string) = AnsiConsole.Markup("[red][[{0}]]  {1}[/]\n", now(), s.EscapeMarkup())
  let renderInfo (s : string) = AnsiConsole.Markup("[aqua][[{0}]]  {1}[/]\n", now(), s.EscapeMarkup())
  let renderDebug (s : string) = AnsiConsole.Markup("[dodgerblue2][[{0}]]  {1}[/]\n", now(), s.EscapeMarkup())
  let renderPluginMessage (s : string) = AnsiConsole.Markup("[green][[{0}]]  Message from plugin:[/]\n[lime]{1}[/]\n", now(), s.EscapeMarkup())

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

  let renderSendEventMenu() =
      let prompt =
        Prompt.builder()
        |> Prompt.withTitle "Send Event"
        |> Prompt.withPageSize 10
        |> Prompt.withChoices SendEvent.sendEventList

      AnsiConsole.Prompt prompt

  let pickAndBuildSendEvent() =
      renderSendEventMenu()
      |> SendEvent.handleSendEventInput

  type Commands =
  | Exit
  | ReturnToMenu
  | SendEvent of payload : Types.Received.EventReceived option

  let inputToCommand (input : string) =
    match input.ToLowerInvariant() with
    | "exit" -> Exit
    | "check for message" ->
      renderInfo "Checking if web socket message has been received..."
      ReturnToMenu
    | "send event" ->
      pickAndBuildSendEvent() |> SendEvent
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

