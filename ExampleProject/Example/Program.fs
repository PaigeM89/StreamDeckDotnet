// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open StreamDeckDotnet.Websockets
open StreamDeckDotnet.Engine

[<EntryPoint>]
let main argv =
    let routes = ExampleProject.Routing.routes
    let args = {
      StreamDeckSocketArgs.Port = 0
      Id = System.Guid.Empty
      RegisterEvent = ""
      Info = ""
    }

    let client = StreamDeckClient(args, routes)
    client.Run()

    printfn "Exiting client.run, exiting program"
    0 // return an integer exit code