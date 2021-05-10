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

  /// F# mappers for the spectre builder
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

  [<Literal>]
  let ExitTxt = "Exit"
  [<Literal>]
  let SendEventTxt = "Send Event"
  [<Literal>]
  let CreateActionInstanceTxt = "Create Action Instance"
  [<Literal>]
  let SetActiveActionTxt = "Set Active Action Instance"
  [<Literal>]
  let EditActiveActionTxt = "Edit Active Action Instance"

  type Commands =
  /// Exit the application
  | Exit
  /// Return to the main menu
  | ReturnToMenu
  /// Send an Event
  | SendEvent of payload : Types.Received.EventReceived option
  // | CreateActionInstance
  // | SetActiveAction
  // | EditActiveAction

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

  let (|ExitCommand|_|) (input : string) = 
    if input.ToLowerInvariant() = (ExitTxt.ToLowerInvariant()) then
      Some (fun () -> Exit)
    else
      None
  let (|SendEventCommand|_|) (input : string) = 
    if input.ToLowerInvariant() = (SendEventTxt.ToLowerInvariant()) then
      Some (fun () -> pickAndBuildSendEvent() |> SendEvent)
    else
      None

  // let (|CreateActionInstanceCommand|_|) (input : string) =
  //   if input.ToLowerInvariant() = (CreateActionInstanceTxt.ToLowerInvariant()) then
  //     Some (fun () -> CreateActionInstance)
  //   else
  //     None

  // let (|SetActiveAction|_|) (input : string) =
  //   if input.ToLowerInvariant() = (SetActiveActionTxt.ToLowerInvariant()) then
  //     Some (fun () -> SetActiveAction)
  //   else
  //     None

  // let (|EditActiveAction|_|) (input : string) =
  //   if input.ToLowerInvariant() = (EditActiveActionTxt.ToLowerInvariant()) then
  //     Some (fun () -> EditActiveAction)
  //   else
  //     None

  let renderMainMenu() =
    // let coreOptions = [ ExitTxt; CreateActionInstanceTxt ]
    // let options = if state.ActiveAction.IsSome then [ EditActiveActionTxt; SendEventTxt] @ coreOptions else coreOptions
    let options = [|
      ExitTxt
      SendEventTxt
    |]
    let prompt =
      Prompt.builder() |> Prompt.withTitle "Main Menu" |> Prompt.withPageSize 10 |> Prompt.withChoices options
    AnsiConsole.Prompt(prompt)

  let inputToCommand (input : string) =
    match input with
    | ExitCommand f -> f()
    | SendEventCommand f -> f()
    | _ -> 
      renderError $"Unrecognized user input: ${input}"
      ReturnToMenu

  let mainMenuLoop  sendEventHandler  =
    let loop() = renderMainMenu() |> inputToCommand
    let rec runLoop() =
      match loop() with
      | ReturnToMenu -> runLoop()
      | SendEvent e ->
        sendEventHandler e
        runLoop()
      // | CreateActionInstance ->
      //   actionInstanceHandler()
      // | SetActiveAction ->
      //   actionInstanceHandler()
      | Exit -> ()
    
    runLoop()

