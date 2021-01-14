namespace Example.Shared

type ServerMessage =
| HelloWorld of string

type ClientMessage =
| HelloFromClient of string

module Types =
    // #if FABLE_COMPILER
    // open Thoth.Json
    // #else
    // open Thoth.Json.Net
    // #endif

    open Thoth.Json

    type PropertyInspectorRegisterEvent = {
        Event : string
        UUID : System.Guid
    } with
        static member Default() = {
            Event = "registerPropertyInspector"
            UUID = System.Guid.Empty
        }

        member this.Encode() =
            Thoth.Json.Encode.object [
                "event", Encode.string this.Event
                "uuid", Encode.guid this.UUID
            ]