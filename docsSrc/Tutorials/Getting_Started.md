# Getting Started

## Install

    [lang=bash]
    paket install StreamDeckDotnet

## Create Routes

Events from the Stream Deck Application are handled in a pipeline, where the first handler to return `Some<EventContext>` handles that event and the rest of the pipelines are skipped. Incoming events are mapped into an `EventContext`, containing event metadata (action instance, action coordinates, or device id, for example). The event payload can be mapped to the appropriate type easily during the routing.

```Fsharp
open StreamDeckDotnet
open StreamDeckDotnet.Routing.EventBinders
open StreamDeckDotnet.Types.Received

let keyDownHandler (keyPayload : KeyPayload) next (ctx : EventContext) = async {
  match ctx.TryGetContextGuid() with
  | Some contextId ->
    // helper functions allow for chaining
    let ctx = ctx |> addLogToContext $"In Key Down Handler for context %O{contextId}" |> addShowOk
    return! next ctx
  | None ->
    // The context itself is an object, so it can have fields modified. (The horror!)
    ctx.AddLog($"In key down handler, no context was found")
    ctx.AddAlert()
    return! next ctx
}

let routes : EventRoute = choose [
    // functions can be added to the pipeline, like this logging function,
    // which appends a log to the context's events to send back to the Stream Deck
    KEY_DOWN >=> Core.log "in KEY_DOWN handler" >=> tryBindKeyDownEvent errorHandler keyDownHandler

    // Don't actually do this unless you want a LOT of logs
    Core.logWithContext "Unsupported event type" >=> showAlert
  ]
```

Routes are checked in order, so the more specific routes should be at the top. Two routes with the same key type, such as 2 routes beginning with `KEY_DOWN`, will need to have different steps in the event pipeline to differentiate them, or one will always shadow the other.

There are two important parts to point out:
1. Nothing stops you from writing a fundamentally flawed pipeline: `KEY_DOWN >=> Core.log "In my handler" >=> tryBindSendToPluginEvent errorHandler keyDownHandler`. This route will handle every `KEY_DOWN` event, but will try to parse the payload to a `SendToPluginEvent`, which will result in an error and invoke the error handler.
2. Many payloads contain a `Settings` value as a `JsonValue`. This is a user-definable object you can shove values into. The framework doesn't provide a way to bind or map this `JToken` to your custom `Settings` type, so you'll need to write your own mapper.

### The Fish (>=>)

The `>=>` operator combines two `EventHandler` functions into a single function, allowing us to chain functions to handle an event. It's the main building block for creating an event handling pipeline. At the end of a function composed by `>=>`, if the `EventContext` is returned (ie, `Some ctx`, not `None`), that pipeline will be considered to have successfully handled that request; further possible evaluations will not happen & that returned context will send its events. Thus it is best to have the most specific event pipelines first in the list, and the most general pipelines at the bottom of the list.

## Create & Run client

```Fsharp
[<EntryPoint>]
let main argv =
  let args = ArgsParsing.parseArgs argv
  let client = StreamDeckClient(args, routes)
  client.Run()
```

The client will automatically create the websocket to communicate with the Stream Deck Application.
