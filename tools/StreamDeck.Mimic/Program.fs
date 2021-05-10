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

let exampleProjectWatch (args : Args) =
    [
        "dotnet"
        "watch"
        "run"
        "--"
        "--port"
        (string args.Port)
        "--pluginuuid"
        "'" + (args.PluginUUID.ToString("N")) + "'"
        "--registerevent"
        "'" + args.RegisterEvent + "'"
        "--info"
        //dotnet watch wants a space here if the arg is empty, but the runtime dll doesn't, and i think that's cool.
        if String.isNotNullOrEmpty args.Info then "'" + args.Info + "'" else "' '"
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
        CreateProcess.fromCommand cmd
        |> Proc.run
        |> ignore
        Ok ()
    with
    | ex ->
        StreamDeckProcessCrashed ex |> Error

let handler (socket : Websocket.Socket) (sendEvent : StreamDeckDotnet.Types.Received.EventReceived option) = 
    match sendEvent with
    | None -> ()
    | Some eventToSend -> 
        let msg = eventToSend.Encode Statics.context Statics.device
        CLI.renderInfo $"Message to send to web socket is:\n  %s{msg}"
        let result = socket.Send msg |> Async.RunSynchronously
        CLI.renderInfo $"Send message result is %A{result}"

let websocketMessageHandler (socket : Websocket.Socket) (msg : string) =
    CLI.renderPluginMessage msg

[<EntryPoint>]
let main argv =
    printfn "Welcome to the StreamDeck Mimic application! This application attempts to mimic a StreamDeck with better logging."

    let args = ArgsParsing.parseArgs argv
    renderInfo ($"%A{args}")

    let socket = Websocket.Socket()

    renderInfo "Creating & starting web host..."
    let host =
        async {
            let! webhost = Webhost.buildWebhost  args.Port socket (websocketMessageHandler socket)
            return! Webhost.startWebHost webhost
        } |> Async.RunSynchronously

    renderInfo $"Web host started, StreamDeck.Mimic is ready to accept connections on port %i{args.Port}"
    let cmd = exampleProjectArgs args
    renderInfo $"Run the Example project from the repository root with this command:\n%s{cmd}\n"
    let watchcmd = exampleProjectWatch args
    renderInfo $"Alternatively, navigate to ./ExampleProject/Example folder and run dotnet watch:\n%s{watchcmd}\n"
    try
        CLI.mainMenuLoop (handler socket)
    with
    | ex ->
        renderError $"Error running main menu loop:\n  %s{ex.Message}\n{ex.StackTrace}"

    printfn "\nClosing StreamDeck.Mimic ..."
    0 // return an integer exit code