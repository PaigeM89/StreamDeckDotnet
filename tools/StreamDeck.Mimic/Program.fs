// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open Microsoft.AspNetCore.Hosting
open System
open Fake.DotNet
open Fake.Core
open StreamDeck.Mimic
open StreamDeck.Mimic.CLI
open Spectre.Console

type ErrorTypes =
| StreamDeckProcessCrashed of e : exn

let exampleProjectArgs (args: Args) = 
    [
        "dotnet"
        args.PathToDll
        "-port"
        string args.Port
        "-pluginUUID"
        "'" + (args.PluginUUID.ToString("N")) + "'"
        "-registerEvent"
        "'" + args.RegisterEvent + "'"
        "-info"
        "'" + args.Info + "'"
    ] |> String.concat " "

let launchStreamDeck path =
    let buildExampleProject() =
        DotNet.build(fun c ->
            { c with
                    Configuration = DotNet.BuildConfiguration.Debug
            }
        ) "../../ExampleProject/Example/Example.fsproj"

    let argsList = [
        "-port"
        string 6969
        "-registerEvent"
        "\"registerPlugin\""
        "-info"
        "\"\""
        "-pluginUUID"
        (Guid.NewGuid()).ToString("N")
    ]
    printfn "args list is %A" argsList

    let absolutePath = IO.Path.GetFullPath(path)

    try
        let cmd = Command.RawCommand("dotnet", Arguments.OfArgs (absolutePath::argsList))

        renderInfo $"Command to be run is:\n%A{cmd}"
        //let processResult = 
        CreateProcess.fromCommand cmd
        |> Proc.run
        |> ignore
        Ok ()
    with
    | ex ->
        StreamDeckProcessCrashed ex |> Error

[<EntryPoint>]
let main argv =
    printfn "Welcome to the StreamDeck Mimic application! This application attempts to mimic a StreamDeck with better logging."

    let args = ArgsParsing.parseArgs argv
    renderInfo ($"%A{args}")

    renderInfo "Creating & starting web host..."
    let host =
        async {
            let! webhost = Webhost.buildWebhost args.Port
            return! Webhost.startWebHost webhost
        } |> Async.RunSynchronously

    
    renderInfo $"Web host started, StreamDeck.Mimic is ready to accept connections on port %i{args.Port}"
    let cmd = exampleProjectArgs args
    renderInfo $"Run the Example project with this command: %s{cmd}"
    let input = CLI.renderMainMenu()
    renderInfo $"User selected %A{input}"

    // renderInfo "Launching stream deck application..."
    // let r = launchStreamDeck args.PathToDll
    // match r with
    // | Ok _ ->
    //     let input = CLI.renderMainMenu()
    //     renderInfo $"User input is {input}"
    // | Error (StreamDeckProcessCrashed ex) ->
    //     renderError $"Error running process: %s{ex.Message}\n%s{ex.StackTrace}"

    printfn "\nClosing StreamDeck.Mimic ..."
    0 // return an integer exit code