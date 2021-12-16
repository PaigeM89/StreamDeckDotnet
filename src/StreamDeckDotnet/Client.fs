namespace StreamDeckDotnet

open StreamDeckDotnet.Logging
open StreamDeckDotnet.Logging.Operators

[<AutoOpen>]
module Client =
  open FsToolkit.ErrorHandling

  let private logger = LogProvider.getLoggerByName("StreamDeckDotnet.Client")

  /// <summary>
  /// Handles an individual message by decoding it to an <see cref="Types.EventMetada" /> instance and passing it to the
  /// given <see cref="Core.EventHandler" /> (usually a collection of routes created via <see cref="Core.choose" />). This returns an error
  /// if the given message was not able to be decoded into <see cref="Types.EventMetadata" /> instance.
  /// </summary>
  /// <remarks>
  /// This function is useful if you have established your own way of receiving raw event messages from the Stream Deck application
  /// but want to hook into event routing and have the message processed by the Event Handler.
  /// </remarks>
  /// <param name="routes">The <see cref="Core.EventHandler" /> (usually a collection of routes created via <see cref="Core.choose" />) that will process incoming events.</param>
  /// <param name="msg">The raw incoming message from the Stream Deck application.</param>
  let socketMsgHandler (routes: Core.EventHandler) (msg : string) = asyncResult {
    //first decode into an EventMetadata
    !!! "Decoding event metadata from msg '{msg}'"
    >>!- ("msg", msg)
    |> logger.info
    let! eventMetadata = Types.decodeEventMetadata msg
    !!! "Building context from metadata object {meta}"
    >>!+ ("meta", eventMetadata)
    |> logger.info
    //then build the context
    let ctx = EventContext(eventMetadata)

    let initHandler = fun ctx -> AsyncOption.retn ctx

    !!! "Beginning Event Handling. Metadata is {meta}."
    >>!+ ("meta", eventMetadata)
    |> logger.trace

    match eventMetadata.Payload with
    | Some payload ->
      !!! "Event metadata payload is {payload}"
      >>!+ ("payload", (string payload))
      |> logger.trace
    | None ->
      !!! "Event metadata did not bind a payload" |> logger.trace

    //now match the context to the known routes
    match! routes initHandler ctx with
    | Some ctx ->
      !!! "Event {name} was successfully handled" >>!+ ("name", ctx.EventName) |> logger.trace
      return ctx
    | None ->
      !!! "No route found to handle event {eventName}" >>!+ ("eventName", ctx.EventName) |> logger.warn
      return ctx
  }

  let private forceMessageHandling (routes: Core.EventHandler) (msg : string) = async {
    let! r = socketMsgHandler routes msg
    match r with
    | Ok x -> return x
    | Error e ->
      !!! "Received Error {e} when handling msg {msg}"
      >>!+ ("e", e) >>!+ ("msg", msg)
      |> logger.error
      return failwithf "%A" e
  }

  // handles application registration & sends a `RegisterPlugin` event to streamdeck application.
  let private handleSocketRegister (args : Websockets.StreamDeckSocketArgs) () =
    let register = Sent.RegisterPluginPayload.Create args.RegisterEvent args.PluginUUID
    !!! "Creating registration event of {event}"
    >>!+ ("event", register)
    |> logger.info
    let sendEvent = Sent.EventSent.RegisterPlugin register
    sendEvent.Encode None None

  /// <summary>
  /// Creates a new instance of the Stream Deck Client with the args and routes given.
  /// </summary>
  /// <param name="args">The args required to connect to the Stream Deck application.</param>
  /// <param name="handler">The <see cref="Core.EventHandler" /> (usually a collection of routes created via <see cref="Core.choose" />) that will process incoming events.</param>
  /// <example>
  /// <code>
  /// let main args =
  ///     let routes = choose [
  ///         KEY_DOWN >=> Core.log "In KEY_DOWN handler"
  ///     ]
  ///     let client = StreamDeckClient(args, routes)
  ///     client.Run()
  /// </code>
  /// </example>
  type StreamDeckClient(args : Websockets.StreamDeckSocketArgs, handler : Core.EventHandler) =
    let msgHandler = forceMessageHandling handler
    let registerHandler = handleSocketRegister args

    let _socket = Websockets.StreamDeckConnection(args, msgHandler, registerHandler)

    member _.Run() = _socket.Run()
