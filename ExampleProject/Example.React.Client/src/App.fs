module App

open Feliz

[<ReactComponent>]
let Counter() =
  let (count, setCount) = React.useState(0)
  Html.div [
    Html.button [
      prop.style [ style.marginRight 5 ]
      prop.onClick [ fun _ -> setCount(count + 1)
      prop.text "Increment" ]
    ]
    Html.button [
        prop.style [ style.marginLeft 5 ]
        prop.onClick (fun _ -> setCount(count - 1))
        prop.text "Decrement"
    ]

    Html.h1 count
  ]

open Browser.Dom

ReactDOM.render(Counter(), document.getElementById "root")

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


    startApp model