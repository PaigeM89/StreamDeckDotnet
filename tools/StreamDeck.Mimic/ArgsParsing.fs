namespace StreamDeck.Mimic

open System

type Args = {
  PathToDll : string
  Port : int
  PluginUUID : Guid
  RegisterEvent : string
  Info : string
}

module ArgsParsing =
  open Argu
  
  type Arguments =
  | [<Mandatory>] [<AltCommandLine("--pathToDll")>] PathToDll of string
  | Port of int
  | [<AltCommandLine("--pluginUUID")>] PluginUUID of Guid
  | [<AltCommandLine("--registerEvent")>] RegisterEvent of string
  | Info of string
    interface IArgParserTemplate with
      member s.Usage =
        match s with
        | PathToDll _ -> "The absolute path to the DLL to test this project with."
        | Port _ -> "Specify a port to connect to."
        | PluginUUID _ -> "Specify a UUID/GUID for the plugin."
        | RegisterEvent _ -> "The event name to register with."
        | Info _ -> "Additional information in JSON format."
  
  let parsePort p =
    if p < 0 || p > int UInt16.MaxValue then
        failwith "invalid port number."
    else p

  let parseArgs args =
    let argsParser = ArgumentParser.Create<Arguments>(programName = "StreamDeck.Mimic.exe")
    let results = argsParser.ParseCommandLine args

    let port = results.PostProcessResults (<@ Port @>, parsePort) |> List.tryHead |> Option.defaultValue 1349
    let pluginUuid = 
      match results.TryGetResult(PluginUUID) with
      | Some x -> x
      | None -> System.Guid.NewGuid()

    let registerEvent =
      match results.TryGetResult(RegisterEvent) with
      | Some x -> x
      | None -> "registerEvent"

    let info = 
      match results.TryGetResult(Info) with
      | Some x -> x
      | None -> ""

    {
      PathToDll = results.GetResult Arguments.PathToDll
      Port = port
      PluginUUID = pluginUuid
      RegisterEvent = registerEvent
      Info = info
    }