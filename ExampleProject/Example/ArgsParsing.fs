namespace Example

open System

module ArgsParsing = 
  open Argu
  open StreamDeckDotnet.Websockets

  type Arguments =
  | [<Mandatory>] Port of int
  | [<Mandatory>] Id of Guid
  | [<Mandatory>] RegisterEvent of string
  | Info of string
    interface IArgParserTemplate with
      member s.Usage =
        match s with
        | Port _ -> "Specify a port to connect to."
        | Id _ -> "Specify an Id for the plugin."
        | RegisterEvent _ -> "The event name to register with."
        | Info _ -> "JSON-formatted addtional information."

  let parsePort p =
    if p < 0 || p > int UInt16.MaxValue then
        failwith "invalid port number."
    else p

  let parseArgs args =
    let argsParser = ArgumentParser.Create<Arguments>(programName = "StreamDeckExample.exe")
    let results = argsParser.ParseCommandLine args

    let port = results.PostProcessResults (<@ Port @>, parsePort) |> List.head

    {
      StreamDeckSocketArgs.Port = port
      Id = results.GetResult Id
      RegisterEvent = results.GetResult RegisterEvent
      Info = results.GetResult Info
    }
