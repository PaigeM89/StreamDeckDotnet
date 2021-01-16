namespace Example.Shared

type ServerMessage =
| HelloWorld of string

type ClientMessage =
| HelloFromClient of string

module Types =
    #if FABLE_COMPILER
    open Thoth.Json
    #else
    open Thoth.Json.Net
    #endif

    type PropertyInspectorRegisterEvent = {
        Event : string
        UUID : System.Guid
    } with
        static member Default() = {
            Event = "registerPropertyInspector"
            UUID = System.Guid.Empty
        }

        static member Create uuid = { PropertyInspectorRegisterEvent.Default() with UUID = uuid }

        member this.Encode() =
            Encode.object [
                "event", Encode.string this.Event
                "uuid", Encode.guid this.UUID
            ]

    type ClientSendEvent =
    | PiRegisterEvent of pire: PropertyInspectorRegisterEvent
    with
        member this.Encode() =
            let payload = 
                match this with
                | PiRegisterEvent e -> e.Encode() 
            
            Encode.toString 0 payload