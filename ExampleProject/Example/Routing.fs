namespace ExampleProject

open StreamDeckDotnet.Core

module Routing =

  let myAction (event : Events.EventReceived) (next: EventFunc) (ctx : ActionContext) = async {
    let! thing = Core.addLog $"in My Action handler, with event {event}" ctx
    match thing with
    | Some ctx -> return! Core.flow next ctx
    | None -> return! Core.flow next ctx
  }

  let errorHandler (msg : string) : EventHandler = Core.log ($"in error handler, error: {msg}")

  let routes = choose [
    KEY_DOWN >=> Core.log "in KEY_DOWN handler" >=> tryBindEventPayload errorHandler myAction
  ]