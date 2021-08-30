module GuidGenerator

open StreamDeckDotnet
open StreamDeckDotnet.EventBinders
open TextCopy

let keyDownHandler keyPayload next (ctx : EventContext) =  async {
  let guid = System.Guid.NewGuid()
  ClipboardService.SetText(string guid)

  return! next ctx
}

let errorHandler (err : PipelineFailure) : EventHandler =
  Core.log($"Error handling event: %A{err}")

let routes : EventRoute = choose [
  KEY_DOWN >=> tryBindKeyDownEvent errorHandler keyDownHandler >=> showOk
]

[<EntryPoint>]
let main argv =
  let args = ArgsParsing.parseArgs argv

  let client = StreamDeckClient(args, routes)
  try
    client.Run()
  with
  | e ->
    printfn "Error: %A" e
  0
