module App

open System
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

[<ReactComponent>]
let InfoDump(values: {| port: int; uuid : Guid; event : string; info : string; action : string |}) =
  Html.div [
    prop.className "sdpi-item-group"
    prop.children [
      Html.p (sprintf "Port: %i" values.port)
      Html.p (sprintf "UUID is %A" values.uuid)
      Html.p (sprintf "event is %s" values.event)
      Html.p (sprintf "info is %s" values.info)
      Html.p (sprintf "actionInfo is %s" values.action)
    ]
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
      // Html.button [
      //   prop.className sdpiItem
      //   prop.style [ style.marginRight 5 ]
      //   prop.onClick (fun _ -> setCount(count + 1))
      //   prop.text "Increment"
      // ]
      // Html.button [
      //   prop.className sdpiItem
      //   prop.style [ style.marginLeft 5 ]
      //   prop.onClick (fun _ -> setCount(count - 1))
      //   prop.text "Decrement"
      // ]

      // Html.h1 [
      //   prop.className sdpiItem
      //   prop.text (string count)
      // ]
      Html.details [
        Html.summary "PI Info"
        Html.div [ 
          InfoDump({| port = port; uuid = uuid; event = event; info = info; action = actionInfo |})
        ]
      ]
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