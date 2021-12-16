namespace StreamDeckDotnet.Fable

[<AutoOpen>]
module Client =
  open StreamDeckDotnet.Logging
  open StreamDeckDotnet.Logging.Operators
  open StreamDeckDotnet.Types
  open StreamDeckDotnet.Context
  open StreamDeckDotnet.Core
  open FsToolkit.ErrorHandling

  /// <summary>
  /// Handles Event messages in a string format and processes them using a given set of routes.
  /// </summary>
  /// <param name="routes">The routes to use to handle the events.</param>
  /// <param name="msg">The event to handle. This is usually the message received from a web socket.</param>
  /// <returns>An `AsyncResult` of either a successful `EventContext` or an error `string`.</returns>
  let socketMsgHandler (routes : EventHandler) (msg : string) : Async<Result<EventContext, string>> = asyncResult {
    let logger = LogProvider.getLoggerByName "StreamDeckDotnet.Fable.Client.socketMsgHandler"
    let! eventMetadata = decodeEventMetadata msg
    !!! "Event metadata is: {metadata}" >>!+ ("metadata", eventMetadata) |> logger.debug

    let ctx = EventContext(eventMetadata)
    let initHandler = fun _ -> AsyncOption.retn ctx

    // match! gave errors when building via yarn, thus the let! instead.
    !!! "Initializing handling" |> logger.trace
    let! handledContext = routes initHandler ctx
    !!! "Handling complete" |> logger.trace

    match handledContext with
    | Some ctx ->
      string ctx |> printfn "Handler found, returning handled context: %A"
      return ctx
    | None ->
      string ctx |> printfn "No handler found, returning default context: %A"
      return ctx
  }

