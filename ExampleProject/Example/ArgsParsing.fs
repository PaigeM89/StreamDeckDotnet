namespace Example

open System

module ArgsParsing = 
  open Argu
  open StreamDeckDotnet.Websockets
  open StreamDeckDotnet.Logging

  let logger = LogProvider.getLoggerByName("Example.ArgsParsing")

  type Arguments =
  | [<Mandatory>][<AltCommandLine("-port")>] Port of int
  | [<Mandatory>][<AltCommandLine("-pluginUUID")>] PluginUUID of Guid
  | [<Mandatory>][<AltCommandLine("-registerEvent")>] RegisterEvent of string
  | [<AltCommandLine("-info")>]Info of string
    interface IArgParserTemplate with
      member s.Usage =
        match s with
        | Port _ -> "Specify a port to connect to."
        | PluginUUID _ -> "Specify a UUID/GUID for the plugin."
        | RegisterEvent _ -> "The event name to register with."
        | Info _ -> "JSON-formatted addtional information."

  let parsePort p =
    if p < 0 || p > int UInt16.MaxValue then
        failwith "invalid port number."
    else p

  let parseArgs args =
    try
      Log.setMessage "Creating args parser" |> logger.trace
      let argsParser = ArgumentParser.Create<Arguments>(programName = "StreamDeckExample.exe")
      
      Log.setMessage "Parsing args" |> logger.trace
      let results = argsParser.ParseCommandLine args

      let port = results.PostProcessResults (<@ Port @>, parsePort) |> List.head
      Log.setMessage "Creating Args object from parsed args" |> logger.trace
      {
        StreamDeckSocketArgs.Port = port
        PluginUUID = results.GetResult PluginUUID
        RegisterEvent = results.GetResult RegisterEvent
        Info = results.GetResult Info
      }
    with
    | ex ->
      Log.setMessage "Exception while parsing args: {msg}"
      >> Log.addContextDestructured "msg" ex.Message
      >> Log.addExn ex
      |> logger.error
      raise ex