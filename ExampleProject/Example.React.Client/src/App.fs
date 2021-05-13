module App

open Example.React.Client.Websockets
open Fable.React
open Fable.React.Props
open Feliz

let sdpiItem = "sdpi-item"
let msgClass = "message"

let htmlPItem content =
  Html.p [
    prop.className sdpiItem
    prop.text (string content)
  ]

// this has to be a function component to not crash on start
let Counter = React.functionComponent(fun (port, uuid, event, info, actionInfo) ->
  let (count, setCount) = React.useState(0)
  let (socket, setSocket) = React.useState(None)
  let (lastMsg, setLastMsg) = React.useState("")

  let connectSocket() = 
    let websocket = Websocket(port, uuid, setLastMsg)
    setSocket (Some websocket)

  React.useEffectOnce(connectSocket)

  Html.div [
    prop.className sdpiItem
    prop.children [
      Html.button [
        prop.className sdpiItem
        prop.style [ style.marginRight 5 ]
        prop.onClick (fun _ -> setCount(count + 1))
        prop.text "Increment"
      ]
      Html.button [
        prop.className sdpiItem
        prop.style [ style.marginLeft 5 ]
        prop.onClick (fun _ -> setCount(count - 1))
        prop.text "Decrement"
      ]

      Html.h1 [
        prop.className sdpiItem
        prop.text (string count)
      ]
      htmlPItem (sprintf "Port is %i" port)
      htmlPItem (sprintf "UUID is %A" uuid)
      htmlPItem (sprintf "event is %s" event)
      htmlPItem (sprintf "info is %s" info)
      htmlPItem (sprintf "actionInfo is %s" actionInfo)
      match socket with
      | Some _ -> htmlPItem "Socket is connected"
      | None -> htmlPItem "Connecting socket..."
    ]
  ]
)

open Browser.Dom

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

    // todo: connect web socket here

    let element = Counter(inPort, inPropertyInspectorUUID, inRegisterEvent, inInfo, inActionInfo)
    ReactDOM.render(element, document.getElementById "root")