namespace StreamDeck.Mimic

open Spectre.Console

module CLI =
  let renderError (s : string) = AnsiConsole.Markup("[red]{0}[/]\n", s.EscapeMarkup())
  let renderInfo (s : string) = AnsiConsole.Markup("[aqua]{0}[/]\n", s.EscapeMarkup())
  let renderResponse (s : string) = AnsiConsole.Markup("[green]Response from plugin:[/]\n[lime]{0}[/]\n", s.EscapeMarkup())

  let menu = 
    [
      "Exit"
      "Send Message"
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