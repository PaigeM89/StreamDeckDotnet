open System
open System.IO
open StreamDeckDotnet
open StreamDeckDotnet.Logging
open Serilog
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Configuration.Json
open Example
open Example.ArgsParsing

let configuration =
  ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build()

let log =
  LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .Enrich.With(ThreadIdEnricher())
    .WriteTo.File("log.txt",
      outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:W3}] ({ThreadId}) {Message}{NewLine}{Exception}"
    )
    .WriteTo.Console(
      outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:W3}] ({ThreadId}) {Message}{NewLine}{Exception}"
    )
    .CreateLogger()

LogProvider.setLoggerProvider (Providers.SerilogProvider.create ())
Serilog.Log.Logger <- log

let logger = LogProvider.getLoggerByName("Example.Program")

[<EntryPoint>]
let main argv =
  Log.setMessage("Starting example project") |> logger.trace
  for arg in argv do
    Log.setMessage($"Arg is '{arg}'") |> logger.trace
  let routes = ExampleProject.Routing.routes

  Log.setMessage "Parsing args..." |> logger.trace
  let args = ArgsParsing.parseArgs argv
  Log.setMessage "Parsed args are {pargs}" >> Log.addContextDestructured "pargs" args |> logger.info

  Log.setMessage("Creating client") |> logger.trace
  let client = StreamDeckClient(args, routes)
  try
    Log.setMessage("Running client") |> logger.trace
    client.Run()
  with
  | e ->
    Log.setMessage "Error running web socket: {msg}"
    >> Log.addContext "msg" e.Message
    >> Log.addExn e
    |> logger.error

  Log.setMessage "Exiting client.run, exiting program" |> logger.trace

  printfn "Exiting client.run, exiting program"
  0 // return an integer exit code
