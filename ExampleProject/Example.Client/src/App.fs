module App

// open Fable.Core
// open Fable.Browser.Dom
// open Browser.Dom

// // Mutable variable to count the number of times we clicked the button
// let mutable count = 0

// // Get a reference to our button and cast the Element to an HTMLButtonElement
// let myButton = document.querySelector(".my-button") :?> Browser.Types.HTMLButtonElement

// // Register our listener
// myButton.onclick <- fun _ ->
//     count <- count + 1
//     myButton.innerText <- sprintf "You clicked: %i time(s)" count

//open Thoth.Json

// module Types =
//     // #if FABLE_COMPILER
//     // open Thoth.Json
//     // #else
//     // open Thoth.Json.Net
//     // #endif

//     type PropertyInspectorRegisterEvent = {
//         Event : string
//         UUID : System.Guid
//     } with
//         static member Default() = {
//             Event = "registerPropertyInspector"
//             UUID = System.Guid.Empty
//         }

//         member this.Encode() =
//             Thoth.Json.Encode.object [
//                 "event", Encode.string this.Event
//                 "uuid", Encode.guid this.UUID
//             ]

open Elmish
open Elmish.Bridge
module Websockets =
    open Browser
    open Browser.Types
    open Fable.SimpleJson

    let getBaseUrl() =
        let url =
            Dom.window.location.href
            |> Url.URL.Create
        url.protocol <- url.protocol.Replace("http", "ws")
        url.hash <- ""
        url

    type Websocket(port : int) =
        // let private onOpen() =
        //     let jsonToSend =

        let createWebsocket() =
            let wsref : WebSocket option ref = ref None
            let rec connect timeout server =
                printfn "attempting to connect web socket to %s with timeout %i..." server timeout
                match !wsref with
                | Some _ -> ()
                | None ->
                    let socket = WebSocket.Create server
                    wsref := Some socket
                    socket.onclose <- fun _ ->
                        Dom.window.setTimeout
                            ((fun () -> connect timeout server), timeout, ()) |> ignore
                    socket.onmessage <- fun e ->
                        Json.tryParseNativeAs(string e.data)
                        |> function
                            | Ok msg ->
                                // Browser.console.log("websocket message is", msg)
                                printfn "websocket msg is %A" msg
                            | _ ->
                                //Browser.console.log("could not parse message", e)
                                printfn "could not parse message %A" e
            connect (60000) (string (getBaseUrl()))

            ()
        
        do createWebsocket()

        member this.Send(payload: string) = ()
        member this.OnReceive(func : string -> unit) =
            func "hello"
            ()


module Models = 
    type Model = {
        Count : int
        Port : int
        PropertyInspectorUUID : System.Guid
        RegisterEvent : string
        Info : string
        ActionInfo : string
    }

    type Msg = 
    | Increment 
    | Decrement

module Updates =
    open Models
    
    let init () = 0
    
    let update (msg:Msg) count =
        match msg with
        | Increment -> count + 1
        | Decrement -> count - 1

module View =
    open Models
    open Fable.React.Props
    open Fable.React.Helpers
    open Fable.React.Standard

    let view model dispatch =
        div []
            [   button [ OnClick (fun _ -> dispatch Decrement) ] [ str "-" ]
                div [] [ str (sprintf "%A" model) ]
                button [ OnClick (fun _ -> dispatch Increment) ] [ str "+" ] 
            ]

open Elmish.React

// let private buildBridgeConfig (model : Models.Model) =
//     Bridge.endpoint '/'
//     |> Bridge.

let startApp () =
    Program.mkSimple Updates.init Updates.update View.view
    |> Program.withConsoleTrace
    //|> Program.withBridge "/"
    |> Program.withReactBatched "elmish-app"
    |> Program.run


let connectElgatoStreamDeckSocket 
        (inPort : int)
        (inPropertyInspectorUUID : System.Guid)
        (inRegisterEvent : string)
        (inInfo: string)
        (inActionInfo : string) =

    let model = {
        Models.Model.Count =  0
        Models.Model.Port = inPort
        Models.Model.PropertyInspectorUUID = inPropertyInspectorUUID
        Models.Model.RegisterEvent = inRegisterEvent
        Models.Model.Info = inInfo
        Models.Model.ActionInfo = inActionInfo
    }

    printfn "model %A" model

    printfn "creating web socket..."
    let websocket = Websockets.Websocket(model.Port)

    printfn "websocket create complete!"
    //create a websocket with the passed in port

    ()
