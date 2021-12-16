namespace StreamDeckDotnet.Fable

[<AutoOpen>]
module Client =
  open StreamDeckDotnet.Types
  open StreamDeckDotnet.Context
  open StreamDeckDotnet.Core
  open StreamDeckDotnet.Routing
  open StreamDeckDotnet.EventBinders
  open FsToolkit.ErrorHandling

  let socketMsgHandler (routes : EventHandler) (msg : string) : Async<Result<EventContext, string>> = asyncResult {
    let! eventMetadata = decodeEventMetadata msg
    string eventMetadata |> printfn "Event metadata is %s"
    let ctx = EventContext(eventMetadata)
    let initHandler = fun _ -> AsyncOption.retn ctx
    // match! gave errors when building via yarn, thus the let! instead.
    printfn "Initializing handling"
    let! handledContext = routes initHandler ctx
    printfn "Handling complete"
    match handledContext with
    | Some ctx ->
      string ctx |> printfn "Handler found, returning handled context: %A"
      return ctx
    | None ->
      string ctx |> printfn "No handler found, returning default context: %A"
      return ctx
  }

