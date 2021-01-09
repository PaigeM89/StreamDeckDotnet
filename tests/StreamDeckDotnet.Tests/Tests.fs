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
    open StreamDeckDotnet.ActionRouting
    
    module EventMetadataModule =
        let empty() = {
            Action = None
            Event = ""
            Context = None
            Device = None
            Payload = None
        }
        let thing() = ()

    let errorHandler (err : PipelineFailure) : EventHandler = Core.log ($"in error handler, error: {err}")
    let myAction (event : Events.EventReceived) (next: EventFunc) (ctx : EventContext) = async {
        Core.addLog $"in My Action handler, with event {event}" ctx
        return! Core.flow next ctx
    }

    let emptyContext = EventContext( EventMetadataModule.empty()  )
    let withAction name : EventContext = 
        let ar = { EventMetadataModule.empty() with Event = name}
        EventContext(ar)
    let next = fun (ctx : EventContext) ->  Some ctx |> Async.lift

    let runTest route next ctx = 
        evaluateStep route next ctx |> Async.RunSynchronously

    [<Tests>]
    let tests =
        testList "Routing Engine tests" [
            testCase "Single route returns a context" <| fun _ ->
                let ctx = withAction ""
                let route = eventMatch ""
                let output = runTest route next ctx
                Expect.isSome output "Should still get a context back"

            testCase "Empty route returns no events to write" <| fun _ ->
                let ctx = withAction ""
                let route = eventMatch ""
                let output = runTest route next ctx
                match output with
                | Some ctx ->
                    let outputEvents = (ctx.GetEventsToSend()) |> List.map (fun x -> x.ToString())
                    Expect.equal 
                        (Seq.length outputEvents) 
                        0 
                        (sprintf "Should not find events to send, but found %A.\nThis means it logged an error." outputEvents)
                | None -> failwith "Did not find context when it should have"

            testCase "Action route filters based on action name - matching" <| fun _ ->
                let ctx = withAction "action1"
                printfn "\nctx in unit test has action %s \n" ctx.EventMetadata.Event
                let route = eventMatch "action1"
                let output = runTest route next ctx
                Expect.isSome output "Should still get a context back"
            
            testCase "Action route filters based on action name - no matching" <| fun _ ->
                let ctx = withAction "action1"
                let route = eventMatch "action2"
                let output = runTest route next ctx
                Expect.isSome output "Should still get a context back"

            testCase "Single route returns an event to write" <| fun _ ->
                let ctx = withAction "action"
                let route = eventMatch "action" >=> Core.log "node1"
                let output = runTest route next ctx
                match output with
                | Some ctx ->
                    Expect.equal (Seq.length (ctx.GetEventsToSend())) 1 "Should have created a single event to send"
                | None -> failwith "Did not find context when it should have"

            ftestCase "Multi step route has all nodes visited" <| fun _ ->
                let ctx = withAction "action"
                let route = eventMatch "action" >=> Core.log "node1"  >=> Core.log "node2"
                let output = runTest route next ctx
                match output with
                | Some ctx ->
                    let expected = [ "node1"; "node2" ] |> List.map Events.createLogEvent
                    Expect.equal (ctx.GetEventsToSend()) expected "Should have visited all nodes with logging."
                | None -> failwith "Did not find context when it should have"
                
            testCase "Inspect multiple routes picks based on action" <| fun _ ->
                let ctx = withAction "action1"
                let route = choose [
                    eventMatch "action1" >=> tryBindEvent errorHandler (fun x next ctx -> addLog $"action 1: {x}" ctx; Core.flow next ctx)
                    eventMatch "action2" >=> tryBindEvent errorHandler (fun x next ctx -> addLog $"action 2: {x}" ctx; Core.flow next ctx)
                ]
                let output = runTest route next ctx
                Expect.isSome output "Should still get a context"
        ]