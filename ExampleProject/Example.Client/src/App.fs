module App

open Elmish
open Elmish.Bridge
open Example.Shared

module Websockets =
    open Browser
    open Browser.Types
    open Fable.SimpleJson

    let getBaseUrl port =
        let url =
            Dom.window.location.href
            |> Url.URL.Create
        url.protocol <- url.protocol.Replace("http", "ws")
        url.hash <- ""
        url

    let getWebsocketServerUrl (port: int) =
        sprintf "ws://localhost:%i" port

    //https://github.com/fable-compiler/fable-browser/blob/master/src/WebSocket/Browser.WebSocket.fs
    
    type Websocket(port : int, uuid: System.Guid, messageHandler : string -> unit) =
        let mutable msgQueue : string list = []
        let wsref : WebSocket option ref = ref None
        
        let createWebsocket() =
            let rec connect timeout server =
                printfn "attempting to connect web socket to %s with timeout %i..." server timeout
                match !wsref with
                | Some _ -> ()
                | None ->
                    let socket = WebSocket.Create server
                    wsref := Some socket
                    // we send our registration event after opening the socket without using the OnOpen event
                    socket.onerror <- fun _ ->
                        printfn "Socket had error!"
                    socket.onopen <- fun e ->
                        printfn "Socket was opened, on open being called! %A" e
                        printfn "MsgQueue is %A" msgQueue
                        msgQueue |> List.rev |> List.iter socket.send
                    socket.onclose <- fun _ ->
                        printfn "Socket was closed!"
                        Dom.window.setTimeout
                            ((fun () -> connect timeout server), timeout, ()) |> ignore
                    socket.onmessage <- fun e ->
                        Json.tryParseNativeAs(string e.data)
                        |> function
                            | Ok msg ->
                                printfn "websocket msg is %A" msg
                                messageHandler msg
                            | _ ->
                                printfn "could not parse message %A" e
            connect (60000) (getWebsocketServerUrl port)
            printfn "Websocket finished connect(), returning out of constructor"
            match !wsref with
            | Some ws -> 
                printfn "Socket state is %A" ws.readyState
            | None -> printfn "socket is none"
            ()
        
        do createWebsocket()

        member this.IsOpen() =
            printfn "in IsOpen func"
            match !wsref with
            | Some ws -> ws.readyState = WebSocketState.OPEN
            | None -> false

        member this.SendToSocket(payload: string) = 
            if this.IsOpen() then
                match !wsref with
                | Some ws ->
                    let payload = Json.stringify payload
                    printfn "websocket sending \"%s\"" payload
                    ws.send payload
                | None -> ()
            else
                let payload = Json.stringify payload
                msgQueue <- payload :: msgQueue


module Models = 
    type Model = {
        Port : int
        PropertyInspectorUUID : System.Guid
        RegisterEvent : string
        Info : string
        ActionInfo : string
        Websocket : Websockets.Websocket option
    } with
        static member Empty() = {
            Port = 0
            PropertyInspectorUUID = System.Guid.Empty
            RegisterEvent = ""
            Info = ""
            ActionInfo = ""
            Websocket = None
        }

        member this.Send (e : Example.Shared.Types.ClientSendEvent) =
            match this.Websocket with
            | Some ws -> ws.SendToSocket(e.Encode())
            | None -> ()

    type Msg = 
    | Connect
    | SendToSocket of toSend : Types.ClientSendEvent
    //| TestExternalMessage of count : int
    | UpdatePort of port : int

module Updates =
    open Models
    
    let sendRegisterEvent (model : Model) =
        let registerEvent = 
            Types.PropertyInspectorRegisterEvent.Create model.PropertyInspectorUUID
            |> Types.ClientSendEvent.PiRegisterEvent
        model, Cmd.ofMsg (Models.Msg.SendToSocket registerEvent)

    // let sendRegisterEventFunc (model : Model) () =
    //     let registerEvent = 
    //         Types.PropertyInspectorRegisterEvent.Create model.PropertyInspectorUUID
    //         |> Types.ClientSendEvent.PiRegisterEvent
    //     model.Send(registerEvent)

    let init (initialModel : Model) =
        initialModel , Cmd.ofMsg Connect
    
    let msgPrinter msg =
        printfn "Message: %s" msg

    /// Recieves the pure string from the web socket, decodes it, and handles it
    let msgHandler msg =
        let decoded = 


    let update (msg:Msg) (model: Model) : (Model * Cmd<Models.Msg>)=
        printfn "In update function"
        match msg with
        | Connect ->
            let ws = Websockets.Websocket(model.Port, model.PropertyInspectorUUID, msgPrinter)
            let model = { model with Websocket = Some ws }

            model, Cmd.none
        | SendToSocket toSend ->
            printfn "Sending to socket: %A" toSend
            match model.Websocket with
            | None -> model, Cmd.none // silently fail because yay
            | Some ws ->
                let encoded = toSend.Encode()
                printfn "encoded payload being sent is %s" encoded
                ws.SendToSocket(encoded)
                model, Cmd.none
        | UpdatePort port -> { model with Port = port }, Cmd.none
        // | TestExternalMessage count ->
        //     printfn "handling test external message scenario"
        //     model, Cmd.ofMsg (UpdatePort count)


module View =
    open Models
    open Fable.React
    open Fable.React.Props
    open Fable.React.Helpers
    open Fable.React.Standard

    // https://github.com/fable-compiler/fable-react/blob/master/src/Fable.React.Standard.fs

    let view (model: Models.Model) dispatch =
        printfn "drawing updated view from model %A" model
        let sdpiWrapper = Class "sdpi-wrapper"
        let sdpiItem = Class "sdpi-item"
        let msgClass = Class "message"
        div [ sdpiWrapper ] [
            div [ sdpiItem] [
                details [ msgClass ] [ 
                    summary [] [ str (sprintf "Plugin UUID is %s" (model.PropertyInspectorUUID.ToString("N")))]
                ]
            ]
        ]

open Elmish.React

let startApp (initialModel : Models.Model) =
    Program.mkProgram 
            Updates.init 
            Updates.update
            View.view
    |> Program.withConsoleTrace
    //attempt to register the plugin after 3 seconds
    |> Program.withSubscription Updates.sendRegisterEventSub
    |> Program.withReactBatched "elmish-app"
    |> Program.runWith initialModel

let connectStreamDeck 
        (inPort : int)
        (inPropertyInspectorUUID : System.Guid)
        (inRegisterEvent : string)
        (inInfo: string)
        (inActionInfo : string) =

    printfn 
        "Args are: inPort: %A\nInPI_UUID: %A\nregister Event: %s\ninfo: %s\n actionInfo: %s"
        inPort
        inPropertyInspectorUUID
        inRegisterEvent
        inInfo
        inActionInfo

    let model = {
        Models.Model.Port = inPort
        Models.Model.PropertyInspectorUUID = inPropertyInspectorUUID
        Models.Model.RegisterEvent = inRegisterEvent
        Models.Model.Info = inInfo
        Models.Model.ActionInfo = inActionInfo
        Models.Model.Websocket = None
    }

    printfn "model created from external invoke is %A" model

    startApp model
