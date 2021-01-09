open System
open StreamDeckDotnet.Websockets
open StreamDeckDotnet.Engine
open StreamDeckDotnet.Logging
open Serilog
open Example

let log = 
  LoggerConfiguration()
    .MinimumLevel.Verbose()
    .Enrich.FromLogContext()
    .Enrich.With(new ThreadIdEnricher())
    .WriteTo.File("log.txt",
      outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss zzz} [{Level:W3}] ({ThreadId}) {Message}{NewLine}{Exception}"
    )
    .WriteTo.Console(
      outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss zzz} [{Level:W3}] ({ThreadId}) {Message}{NewLine}{Exception}"
    )
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
  try
    client.Run()
  with
  | e ->
    Log.setMessage "Error running web socket: {msg}"
    >> Log.addContext "msg" e.Message
    >> Log.addExn e
    |> logger.error

  printfn "Exiting client.run, exiting program"
  0 // return an integer exit code