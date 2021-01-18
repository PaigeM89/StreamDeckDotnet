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
    
    type Websocket(port : int, uuid: System.Guid, messageHandler : string -> unit, openHandler: unit -> unit) =
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
                        openHandler()
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
                                // Browser.console.log("websocket message is", msg)
                                printfn "websocket msg is %A" msg
                                messageHandler msg
                            | _ ->
                                //Browser.console.log("could not parse message", e)
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
        Count : int
        Port : int
        PropertyInspectorUUID : System.Guid
        RegisterEvent : string
        Info : string
        ActionInfo : string
        Websocket : Websockets.Websocket option
    } with
        static member Empty() = {
            Count = 0
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
    | TestExternalMessage of count : int
    | UpdatePort of port : int

module Updates =
    open Models
    
    let sendRegisterEvent (model : Model) =
        let registerEvent = 
            Types.PropertyInspectorRegisterEvent.Create model.PropertyInspectorUUID
            |> Types.ClientSendEvent.PiRegisterEvent
        model, Cmd.ofMsg (Models.Msg.SendToSocket registerEvent)

    let sendRegisterEventSub (model: Model) =
        let registerEvent = 
            Types.PropertyInspectorRegisterEvent.Create model.PropertyInspectorUUID
            |> Types.ClientSendEvent.PiRegisterEvent
            |> Models.Msg.SendToSocket
        let sub dispatch =
            Browser.Dom.window.setTimeout((fun _ -> dispatch registerEvent), 3000)
            |> ignore
        Cmd.ofSub sub

    let sendRegisterEventFunc (model : Model) () =
        let registerEvent = 
            Types.PropertyInspectorRegisterEvent.Create model.PropertyInspectorUUID
            |> Types.ClientSendEvent.PiRegisterEvent
        model.Send(registerEvent)

    let init (initialModel : Model) =
        initialModel , Cmd.ofMsg Connect
    
    let msgPrinter msg =
        printfn "Message: %s" msg

    let update (msg:Msg) (model: Model) : (Model * Cmd<Models.Msg>)=
        printfn "In update function"
        match msg with
        | Connect ->
            let onOpen = sendRegisterEventFunc model
            let ws = Websockets.Websocket(model.Port, model.PropertyInspectorUUID, msgPrinter, onOpen)
            let model = { model with Websocket = Some ws }


            //let st = Browser.Dom.window.setTimeout ((fun _ -> dispatch (sendRegisterEvent model)), 2000)
            model, Cmd.none

            // let registerEvent = 
            //     Types.PropertyInspectorRegisterEvent.Create model.PropertyInspectorUUID
            //     |> Types.ClientSendEvent.PiRegisterEvent
            // { model with Websocket = Some ws }, Cmd.ofMsg (Models.Msg.SendToSocket registerEvent)
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
        | TestExternalMessage count ->
            printfn "handling test external message scenario"
            model, Cmd.ofMsg (UpdatePort count)


module View =
    open Models
    open Fable.React
    open Fable.React.Props
    open Fable.React.Helpers
    open Fable.React.Standard

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

    let model = {
        Models.Model.Count =  0
        Models.Model.Port = inPort
        Models.Model.PropertyInspectorUUID = inPropertyInspectorUUID
        Models.Model.RegisterEvent = inRegisterEvent
        Models.Model.Info = inInfo
        Models.Model.ActionInfo = inActionInfo
        Models.Model.Websocket = None
    }

    printfn "model created from external invoke is %A" model

    // printfn "creating web socket..."
    // let websocket = Websockets.Websocket(model.Port, model.PropertyInspectorUUID)

    // let model' = { model with Websocket = Some websocket }

    startApp model
