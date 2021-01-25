namespace StreamDeckDotnet.Tests

open System
open Expecto
open StreamDeckDotnet



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
    let withEvent name : EventContext = 
        let ar = { EventMetadataModule.empty() with Event = name}
        EventContext(ar)
    let next = fun (ctx : EventContext) ->  Some ctx |> Async.lift

    let runTest route next ctx = 
        evaluateStep route next ctx |> Async.RunSynchronously

    let printContextLogEvents (ctx: EventContext) =
        ctx.GetEventsToSendFromQueue()
        |> List.map (fun x -> 
            match x with
            | Events.EventSent.LogMessage { Message = payload } -> payload.ToString()
            | _ -> ""
        )

    [<Tests>]
    let tests =
        testList "Routing Engine tests" [
            testCase "Single route returns a context" <| fun _ ->
                let ctx = withEvent ""
                let route = eventMatch ""
                let output = runTest route next ctx
                Expect.isSome output "Should still get a context back"

            testCase "Empty route returns no events to write" <| fun _ ->
                let ctx = withEvent ""
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
                let ctx = withEvent "action1"
                let route = eventMatch "action1"
                let output = runTest route next ctx
                Expect.isSome output "Should still get a context back"
            
            testCase "Action route filters based on action name - non matching" <| fun _ ->
                let ctx = withEvent "action1"
                let route = eventMatch "action2"
                let output = runTest route next ctx
                Expect.isNone output "Should not get a context back when route event does not match"

            testCase "Single route returns an event to write" <| fun _ ->
                let ctx = withEvent "action"
                let route = eventMatch "action" >=> Core.log "node1"
                let output = runTest route next ctx
                match output with
                | Some ctx ->
                    Expect.equal (Seq.length (ctx.GetEventsToSend())) 1 "Should have created a single event to send"
                | None -> failwith "Did not find context when it should have"

            testCase "Multi step route has all nodes visited" <| fun _ ->
                let ctx = withEvent "action"
                let route = eventMatch "action" >=> Core.log "node1"  >=> Core.log "node2"
                let output = runTest route next ctx
                match output with
                | Some ctx ->
                    let expected = [ "node1"; "node2" ]
                    let actual = printContextLogEvents ctx
                    Expect.equal actual expected "Should have visited all nodes with logging."
                | None -> failwith "Did not find context when it should have"
                
            testCase "Inspect multiple routes picks based on action - action1" <| fun _ ->
                let ctx = withEvent "action1"
                let route = choose [
                    eventMatch "action1" >=> tryBindEvent errorHandler (fun x next ctx -> addLog $"action 1: {x}" ctx; Core.flow next ctx)
                    eventMatch "action2" >=> tryBindEvent errorHandler (fun x next ctx -> addLog $"action 2: {x}" ctx; Core.flow next ctx)
                ]
                let output = runTest route next ctx
                Expect.isSome output "Should still get a context"

            testCase "Inspect multiple routes picks based on action - action2" <| fun _ ->
                let ctx = withEvent "action2"
                let route = choose [
                    eventMatch "action1" >=> tryBindEvent errorHandler (fun x next ctx -> addLog $"action 1: {x}" ctx; Core.flow next ctx)
                    eventMatch "action2" >=> tryBindEvent errorHandler (fun x next ctx -> addLog $"action 2: {x}" ctx; Core.flow next ctx)
                ]
                let output = runTest route next ctx
                Expect.isSome output "Should still get a context"

            testCase "bind event binds empty payload event" <| fun _ ->
                let ctx = withEvent Events.EventNames.SystemDidWakeUp
                let route = tryBindEvent errorHandler (fun x next ctx -> addLog "successfully bound event" ctx; flow next ctx)
                let output = runTest route next ctx
                match output with
                | Some ctx ->
                    let expected = [ "successfully bound event" ]
                    let actual = printContextLogEvents ctx
                    Expect.equal actual expected "Should have visited all nodes with logging."
                | None -> failwith "Did not find context when it should have"
        ]