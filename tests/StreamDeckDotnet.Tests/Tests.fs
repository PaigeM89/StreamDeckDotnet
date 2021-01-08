namespace StreamDeckDotnet.Tests

open System
open Expecto
open StreamDeckDotnet

module Async =
    let lift a' = async { return a' }

module RoutingEngineTests =
    open StreamDeckDotnet.Context
    open StreamDeckDotnet.Core
    open StreamDeckDotnet.Engine
    open StreamDeckDotnet.Types
    
    module ActionReceived =
        let empty() = {
            Action = None
            Event = ""
            Context = None
            Device = None
            Payload = None
        }

    module TestRoutes =
        open StreamDeckDotnet.ActionRouting

        let myAction (event : Events.EventReceived) (next: ActionFunc) (ctx : ActionContext) = async {
            Core.addLog $"in My Action handler, with event {event}" ctx
            return! Core.flow next ctx
        }

        let errorHandler (err : ActionFailure) : ActionHandler = Core.log ($"in error handler, error: {err}")

        let basicRoute = action "" >=> tryBindEvent errorHandler myAction

        let multiStepRoute = action "action" >=> Core.log "node1"  >=> Core.log "node2"

        let multipleRoutes = choose [
            action "action1" >=> tryBindEvent errorHandler (fun x next ctx -> addLog $"action 1: {x}" ctx; Core.flow next ctx)
            action "action2" >=> tryBindEvent errorHandler (fun x next ctx -> addLog $"action 2: {x}" ctx; Core.flow next ctx)
        ]

        let emptyRoute = action "" >=> tryBindEvent errorHandler (fun x next ctx -> Core.flow next ctx)

    let emptyContext = ActionContext(ActionReceived.empty())
    let withAction name = ActionContext({ emptyContext.ActionReceived with Action = Some name})
    let next = fun (ctx : ActionContext) ->  Some ctx |> Async.lift

    [<Tests>]
    let tests =
        testList "Routing Engine tests" [
            testCase "Single route returns a context" <| fun _ ->
                let ctx = withAction ""
                let output = inspectRoute TestRoutes.basicRoute next ctx |> Async.RunSynchronously
                Expect.isSome output "Should still get a context back"
            testCase "Empty route returns no events to write" <| fun _ ->
                let ctx = withAction ""
                let output = inspectRoute TestRoutes.emptyRoute next ctx |> Async.RunSynchronously
                match output with
                | Some ctx ->
                    Expect.equal (Seq.length (ctx.GetEventsToSend())) 0 "Should not find events to send"
                | None -> failwith "Did not find context when it should have"
            testCase "Single route returns an event to write" <| fun _ ->
                let ctx = withAction ""
                let output = inspectRoute TestRoutes.basicRoute next ctx |> Async.RunSynchronously
                match output with
                | Some ctx ->
                    Expect.equal (Seq.length (ctx.GetEventsToSend())) 1 "Should have created a single event to send"
                | None -> failwith "Did not find context when it should have"
            testCase "Multi step route has all nodes visited" <| fun _ ->
                let ctx = withAction "action"
                let output = inspectRoute TestRoutes.multiStepRoute next ctx |> Async.RunSynchronously
                match output with
                | Some ctx ->
                    let expected = [ "node1"; "node2" ] |> List.map Events.createLogEvent
                    Expect.equal (ctx.GetEventsToSend()) expected "Should have visited all nodes with logging."
                | None -> failwith "Did not find context when it should have"
            testCase "Inspect multiple routes picks based on action" <| fun _ ->
                let ctx = withAction "action1"
                let output = inspectRoute TestRoutes.multipleRoutes next ctx |> Async.RunSynchronously
                Expect.isSome output "Should still get a context"
        ]