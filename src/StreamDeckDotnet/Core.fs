namespace StreamDeckDotnet


module Core =
  open Microsoft.Extensions.Logging
  open Context
  open Events
  open Thoth.Json.Net
  open FsToolkit.ErrorHandling

  // all actions can possibly result in an event being sent.
  type EventFuncResult = Async<Context.EventContext option>

  type EventFunc = Context.EventContext -> EventFuncResult

  type EventHandler = EventFunc -> EventFunc

  type ErrorHandler = exn -> ILogger -> EventHandler

  let skipPipeline : EventFuncResult = Async.lift None
  let earlyReturn : EventFunc = Some >> Async.lift

  let compose (action1 : EventHandler) (action2 : EventHandler) : EventHandler =
    fun final -> final |> action2 |> action1

  let (>=>) = compose

  let rec private chooseEventFunc (funcs : EventFunc list) : EventFunc =
    fun (ctx : Context.EventContext) ->
      async {
        match funcs with
        | [] -> return None
        | func :: tail ->
          let! result = func ctx
          match result with
          | Some c -> return Some c
          | None -> return! chooseEventFunc tail ctx
      }

  let choose (handlers : EventHandler list) : EventHandler = 
    fun (next : EventFunc) ->
      let funcs = handlers |> List.map (fun h -> h next)
      fun (ctx : Context.EventContext) -> chooseEventFunc funcs ctx

  let tryDecode decoder targetType payload =
    result {
      let! payload = Decode.fromString decoder payload
      return targetType payload
    }

  let tryBindEvent (errorHandler : PipelineFailure -> EventHandler) (successHandler : EventReceived -> EventHandler) : EventHandler =
    fun (next : EventFunc) (ctx: EventContext) -> async {
      let! result = ctx.TryBindEventAsync
      match result with
      | Ok event -> return! successHandler event next ctx
      | Error err -> return! errorHandler err next ctx
    }

  let EventMetadata (validate: string -> bool) : EventHandler =
    fun (next : EventFunc) (ctx : EventContext) ->
      let x = ctx.EventMetadata.Event
      printfn "\nAction received event is %s\n" x
      if validate x
      then next ctx
      else skipPipeline

  let validateAction (s : string) (t : string) =
    printfn "\ncomparing %s to %s\n" s t
    s.ToLowerInvariant() = t.ToLowerInvariant()

  //these structures are in Giraffe but i don't know what they do
  let KEY_DOWN : EventHandler = EventMetadata (validateAction EventNames.KeyDown)
  let SYSTEM_WAKE_UP : EventHandler = EventMetadata (validateAction EventNames.SystemDidWakeUp)
  let KEY_UP : EventHandler = EventMetadata (validateAction EventNames.KeyDown)

  let addLog (msg : string) (ctx : EventContext) =
    let log = Events.createLogEvent msg
    Context.addSendEvent log ctx

  let log (msg : string) : EventHandler =
    fun (next : EventFunc) (ctx : EventContext) ->
      addLog msg ctx
      next ctx

  let flow (_ : EventFunc) (ctx: EventContext) = Context.flow ctx
