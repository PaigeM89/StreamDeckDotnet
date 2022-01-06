namespace StreamDeckDotnet.Tests

open System
open Expecto
open StreamDeckDotnet

module TestHelpers =
    open StreamDeckDotnet.Types
    open StreamDeckDotnet.Types.Sent
    open StreamDeckDotnet.Types.Received


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
    let myAction (event : EventReceived) (next: EventFunc) (ctx : EventContext) = async {
        let ctx' = Core.addLogToContext $"in My Action handler, with event {event}" ctx
        return! Core.flow next ctx'
    }

    let emptyContext = EventContext( EventMetadataModule.empty()  )
    let withEvent name : EventContext =
        let ar = { EventMetadataModule.empty() with Event = name}
        EventContext(ar)
    let next = fun (ctx : EventContext) ->  Some ctx |> Async.lift

    let runTest route next ctx =
        async {
            match! next ctx with
            | Some ctx -> return! route next ctx
            | None -> return! skipPipeline
        } |> Async.RunSynchronously

    let printContextLogEvents (ctx: EventContext) =
        ctx.GetEventsToSend()
        |> List.map (fun x ->
            match x with
            | EventSent.LogMessage { Message = payload } -> payload.ToString()
            | _ -> ""
        )

module RoutingEngineTests =
    open StreamDeckDotnet.Core
    open StreamDeckDotnet.Types
    open StreamDeckDotnet.Routing
    open TestHelpers

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
                    let expected = [ "node2"; "node1" ]
                    let actual = printContextLogEvents ctx
                    Expect.equal actual expected "Should have visited all nodes with logging."
                | None -> failwith "Did not find context when it should have"

            testCase "Inspect multiple routes picks based on action - action1" <| fun _ ->
                let ctx = withEvent EventNames.SystemDidWakeUp  //pick a payload-less event
                let route = choose [
                    // bind that event successfully (because it has no payload)
                    eventMatch EventNames.SystemDidWakeUp  >=> tryBindEvent errorHandler (fun x next ctx -> ctx |> addLogToContext $"action 1: {x}" |> Core.flow next)
                    eventMatch "action2" >=> tryBindEvent errorHandler (fun x next ctx -> ctx |> addLogToContext $"action 2: {x}" |>  Core.flow next)
                ]
                let output = runTest route next ctx
                Expect.isSome output "Should still get a context"
                let ctxOutput = output.Value
                let outputEvents = printContextLogEvents ctxOutput
                let expected = [ "action 1: SystemDidWakeUp" ]
                Expect.equal outputEvents expected "Should get expected log event"

            testCase "Inspect multiple routes picks based on action - action2" <| fun _ ->
                let ctx = withEvent "action1"
                let route = choose [
                    eventMatch "action1" >=> tryBindEvent errorHandler (fun x next ctx -> ctx |>  addLogToContext $"action 1: {x}" |>  Core.flow next)
                    eventMatch "action2" >=> tryBindEvent errorHandler (fun x next ctx -> ctx |> addLogToContext $"action 2: {x}" |>  Core.flow next)
                ]
                let output = runTest route next ctx
                Expect.isSome output "Should still get a context"

            testCase "bind event binds empty payload event" <| fun _ ->
                let ctx = withEvent EventNames.SystemDidWakeUp
                let route = tryBindEvent errorHandler (fun x next ctx -> ctx |> addLogToContext "successfully bound event" |> flow next )
                let output = runTest route next ctx
                match output with
                | Some ctx ->
                    let expected = [ "successfully bound event" ]
                    let actual = printContextLogEvents ctx
                    Expect.equal actual expected "Should have visited all nodes with logging."
                | None -> failwith "Did not find context when it should have"
        ]

/// Tests for adding/removing events to send from the context
module ContextSendEventTests =
    open TestHelpers
    open StreamDeckDotnet.Core
    open StreamDeckDotnet.Routing

    let action = "action"

    [<Tests>]
    let tests = testList "Context Events To Send" [
        testCase "Adding no log events returns no log events" <| fun _ ->
            let ctx = withEvent action
            let route = eventMatch action >=> Core.flow
            let output = runTest route next ctx
            match output with
            | Some ctx ->
                let outputEvents = ctx.GetEventsToSend()
                Expect.equal outputEvents [] "Should not have generated any events to send"
            | None -> failwith "Did not find context when it should have"
        testCase "Adding 2 events returns those events in the reverse insert order" <| fun _ ->
            let ctx = withEvent action
            let route =
                eventMatch action >=> Core.log "node1"  >=> Core.log "node2" >=> Core.log "node3"
            let output = runTest route next ctx
            match output with
            | Some ctx ->
                let actual = printContextLogEvents ctx
                Expect.equal actual [ "node3"; "node2"; "node1" ] "Should not have generated any events to send"
            | None -> failwith "Did not find context when it should have"
    ]

module DotnetClientTests =
  open TestHelpers
  open StreamDeckDotnet

  let runToResult ar = ar |> Async.RunSynchronously

  /// Tests that make sure the socket message handler processes events
  [<Tests>]
  let tests = testList "Socket message handler tests" [
    testCase "Empty routes processes empty message" <| fun _ ->
      let routes = choose []
      let msg = """{ "event": "invalidEventName" }"""
      let result = socketMsgHandler routes msg |> runToResult
      match result with
      | Ok ctx ->
        Expect.equal (ctx.GetEventsToSend()) [] "Should not get any events to be sent"
      | Error e -> Expect.isTrue false (sprintf "Expected to process route, instead got error %A" e)
    testCase "Routes process empty message" <| fun _ ->
      let routes = choose [
        KEY_DOWN >=> tryBindKeyDownEvent (fun _ -> Core.log("Error handler")) (fun payload next ctx -> next ctx)
      ]
      let msg = """{ "event": "invalidEventName" }"""
      let result = socketMsgHandler routes msg |> runToResult
      match result with
      | Ok ctx ->
        Expect.equal (ctx.GetEventsToSend()) [] "Should not get any events to be sent"
      | Error e -> Expect.isTrue false (sprintf "Expected to process route, instead got error %A" e)
    testCase "Routes process empty message with error handling" <| fun _ ->
      let routes = choose [
        // removing the KEY_DOWN check should cause the error handler to fire
        tryBindKeyDownEvent (fun _ -> Core.log("Error handler")) (fun payload next ctx -> next ctx)
      ]
      let msg = """{ "event": "invalidEventName" }"""
      let result = socketMsgHandler routes msg |> runToResult
      match result with
      | Ok ctx ->
        let expected = [ Types.Sent.LogMessage { Message = "Error handler" } ]
        Expect.equal (ctx.GetEventsToSend()) expected "Should get a log event to be sent"
      | Error e -> Expect.isTrue false (sprintf "Expected to process route, instead got error %A" e)
  ]
