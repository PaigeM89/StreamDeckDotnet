namespace StreamDeckDotnet.Fable

[<AutoOpen>]
module Client =
  open StreamDeckDotnet.Types
  open StreamDeckDotnet.Context
  open StreamDeckDotnet.Core
  open FsToolkit.ErrorHandling

  let socketMsgHandler (routes : EventHandler) (msg : string) : Async<Result<EventContext, string>> = asyncResult {
    let! eventMetadata = decodeEventMetadata msg
    let ctx = EventContext(eventMetadata)
    let initHandler = fun ctx -> AsyncOption.retn ctx
    match! routes initHandler ctx with
    | Some ctx ->
      return ctx
    | None ->
      // if no routes are found, return the context
      return ctx
  }

