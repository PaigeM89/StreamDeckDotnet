// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open Fake.DotNet
open Fake.Core
open StreamDeck.Mimic
open Spectre.Console

type ErrorTypes =
| StreamDeckProcessCrashed of e : exn

let renderError (s : string) = AnsiConsole.Markup("[red]{0}[/]\n", s.EscapeMarkup())
let renderInfo (s : string) = AnsiConsole.Markup("[aqua]{0}[/]\n", s.EscapeMarkup())

let launchStreamDeck path =
    let buildExampleProject() =
        DotNet.build(fun c ->
            { c with
                    Configuration = DotNet.BuildConfiguration.Debug
            }
        ) "../../ExampleProject/Example/Example.fsproj"

    let argsList = [
        "--port"
        string 6969
        "--registerEvent"
        "\"registerPlugin\""
        "--info"
        "\"\""
        "--pluginUUID"
        (Guid.NewGuid()).ToString("N")
    ]
    printfn "args list is %A" argsList

    let absolutePath = IO.Path.GetFullPath(path)

    try
        let cmd = Command.RawCommand("dotnet", Arguments.OfArgs (absolutePath::argsList))

        renderInfo $"Command to be run is:\n%A{cmd}"
        let processResult = 
            CreateProcess.fromCommand cmd
            |> Proc.run
        Ok processResult
    with
    | ex ->
        StreamDeckProcessCrashed ex |> Error

[<EntryPoint>]
let main argv =
    printfn "Welcome to the StreamDeck Mimic application! This application attempts to mimic a StreamDeck with better logging."

    let args = ArgsParsing.parseArgs argv
    renderInfo ($"%A{args}")

    printfn "Launching stream deck application..."
    let r = launchStreamDeck args.PathToDll
    match r with
    | Ok _ ->
        printfn "Stream deck application ran successfully, exiting"
    | Error (StreamDeckProcessCrashed ex) ->
        renderError $"Error running process: %s{ex.Message}\n%s{ex.StackTrace}"

    printfn "\nClosing StreamDeck.Mimic ..."
    0 // return an integer exit code