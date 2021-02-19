namespace ExampleProject

open StreamDeckDotnet
open StreamDeckDotnet.Core
open StreamDeckDotnet.Context
open StreamDeckDotnet.Types
open StreamDeckDotnet.Types.Received
open StreamDeckDotnet.ActionRouting

open StreamDeckDotnet.Logging
open StreamDeckDotnet.Logger
open StreamDeckDotnet.Logger.Operators

module Routing =

  let logger = LogProvider.getLoggerByName("StreamDeckDotnet.Routing")

  let myAction (event : EventReceived) (next: EventFunc) (ctx : EventContext) = async {
    !! "in My Action handler, with event {event}" >>!+ ("event", event) |> logger.info
    let ctx' = Core.addLog $"in My Action handler, with event {event}" ctx
    return! Core.flow next ctx'
  }

  let errorHandler (err : PipelineFailure) : EventHandler =
    !! "in error handler, with pipeline failure {err}" >>!+ ("err", err) |> logger.info
    Core.log ($"in error handler, error: {err}")

  let routes : EventRoute = choose [
    KEY_DOWN >=> Core.log "in KEY_DOWN handler" >=> tryBindEvent errorHandler myAction
  ]