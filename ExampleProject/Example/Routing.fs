namespace ExampleProject

open StreamDeckDotnet
open StreamDeckDotnet.Types.Received

open StreamDeckDotnet.Logging
open StreamDeckDotnet.Logger
open StreamDeckDotnet.Logger.Operators

module Routing =

  let logger = LogProvider.getLoggerByName("StreamDeckDotnet.Routing")

  let myAction (event : EventReceived) (next: EventFunc) (ctx : EventContext) = async {
    !! "in My Action handler, with event {event}" >>!+ ("event", event) |> logger.info
    let ctx' = Core.addLogToContext $"in My Action handler, with event {event}" ctx
    return! next ctx'
  }

  let appearHandler (event : EventReceived) next ctx = async {
    !! "In Will Appear handler, with event {event}" >>!+ ("event", event) |> logger.info
    let ctx' = Core.addLogToContext $"In appear handler, with event {event}" ctx
    return! next ctx'
  }

  let errorHandler (err : PipelineFailure) : EventHandler =
    !! "in error handler, with pipeline failure {err}" >>!+ ("err", err) |> logger.info
    Core.log ($"in error handler, error: {err}")

  let routes : EventRoute = choose [
    KEY_DOWN >=> Core.log "in KEY_DOWN handler" >=> tryBindEvent errorHandler myAction
    WILL_APPEAR >=> Core.log "in WILL_APPEAR handler" >=> tryBindEvent errorHandler appearHandler
  ]