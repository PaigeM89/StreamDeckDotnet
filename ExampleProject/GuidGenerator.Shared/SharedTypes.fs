module GuidGenerator.SharedTypes

open System

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type PropertyInspectorSettings = {
  LastGeneratedGuid : Guid
} with
  static member Create g = { LastGeneratedGuid = g }

  member this.Encode() =
    Encode.object [
      "lastGeneratedGuid", Encode.guid this.LastGeneratedGuid
    ]
  static member Decoder : Decoder<PropertyInspectorSettings> =
    Decode.object(fun get -> {
      LastGeneratedGuid = get.Required.Field "lastGeneratedGuid" Decode.guid
    })


