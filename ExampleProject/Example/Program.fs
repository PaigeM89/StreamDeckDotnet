// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open StreamDeckDotnet.Websockets
open StreamDeckDotnet.Engine
open StreamDeckDotnet.Logging
open Serilog
// open Serilog.Sinks.File
// open Serilog.Sinks.Console

let log = 
  LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.File("log.txt")
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger()

LogProvider.setLoggerProvider (Providers.SerilogProvider.create ())
Serilog.Log.Logger <- log

let logger = LogProvider.getLoggerByName("Example.Program")

[<EntryPoint>]
let main argv =
  Log.setMessage("Starting example project") |> logger.trace
  let routes = ExampleProject.Routing.routes
  let args = {
    StreamDeckSocketArgs.Port = 0
    Id = System.Guid.Empty
    RegisterEvent = ""
    Info = ""
  }

  Log.setMessage("Creating client") |> logger.trace
  let client = StreamDeckClient(args, routes)
  Log.setMessage("Running client") |> logger.trace
  client.Run()

  printfn "Exiting client.run, exiting program"
  //log.CloseAndFlush()
  0 // return an integer exit code