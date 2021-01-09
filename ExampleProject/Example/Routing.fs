namespace ExampleProject

open StreamDeckDotnet
open StreamDeckDotnet.Core
open StreamDeckDotnet.Context
open StreamDeckDotnet.Events
open StreamDeckDotnet.ActionRouting

module Routing =

  let myAction (event : Events.EventReceived) (next: EventFunc) (ctx : EventContext) = async {
    Core.addLog $"in My Action handler, with event {event}" ctx
    return! Core.flow next ctx
  }

  let errorHandler (err : PipelineFailure) : EventHandler = Core.log ($"in error handler, error: {err}")

  let routes : EventRoute = choose [
    KEY_DOWN >=> Core.log "in KEY_DOWN handler" >=> tryBindEvent errorHandler myAction
  ]