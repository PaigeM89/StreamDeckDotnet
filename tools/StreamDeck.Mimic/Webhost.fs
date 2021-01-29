namespace StreamDeck.Mimic

module Websocket =
  open System.Net.WebSockets
  open Microsoft.AspNetCore.Http
  open FSharp.Control.Websockets
  open FSharp.Control.Websockets.ThreadSafeWebSocket

  type SocketResult<'a> =
  | SocketOk of 'a
  | NotInitialized

  type Socket() =
    let mutable threadSafeWebSocket : ThreadSafeWebSocket option = None

    member this.Create(socket : WebSocket) =
      threadSafeWebSocket <- ThreadSafeWebSocket.createFromWebSocket socket |> Some

    member this.Send (msg : string) = async {
      match threadSafeWebSocket with
      | Some tsws -> 
        let! test = ThreadSafeWebSocket.sendMessageAsUTF8 tsws msg
        return SocketOk test
      | None -> return NotInitialized
    }

    member this.ReceiveAsUTF8() = async {
      match threadSafeWebSocket with
      | None -> return NotInitialized
      | Some tsws -> 
        let! msg =  ThreadSafeWebSocket.receiveMessageAsUTF8 tsws
        return SocketOk msg
    }

    member this.State() =
      match threadSafeWebSocket with
      | Some tsws -> tsws.State
      | None -> WebSocketState.None

    member this.IsOpen() =
      match threadSafeWebSocket with
      | Some tsws when tsws.State = WebSocketState.Open -> true
      | _ -> false

    member this.Initialized() = threadSafeWebSocket.IsSome


  let handleWebSocketRequest (socket : Socket) (handler) (ctx : HttpContext) (next : unit -> Async<unit>) = async {
    if ctx.WebSockets.IsWebSocketRequest then
      if not (socket.Initialized()) then
        let! ws = ctx.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
        socket.Create(ws)
      
      while socket.IsOpen() do
        try
          let! socketResult = socket.ReceiveAsUTF8()
          match socketResult with
          | SocketOk (Ok (WebSocket.ReceiveUTF8Result.String msg)) ->
            // CLI.renderResponse msg
            handler msg
          | SocketOk (Ok (WebSocket.ReceiveUTF8Result.Closed (status, reason))) ->
            CLI.renderError ($"Socket Closed %A{status} - %s{reason}")
          | SocketOk (Error ex) -> 
            CLI.renderError $"Receiving threw an exception: %A{ex.SourceException}"
          | NotInitialized ->
            CLI.renderError "Attempting to receive messages through uninitialized socket"
        with
        | e -> 
          CLI.renderError ($"Error in catch block:\n  %A{e}")
    else
      do! next()
  }

module Webhost =
  open StreamDeck.Mimic
  open Spectre.Console
  open System
  open System.Net
  open System.Net.WebSockets
  open System.Threading.Tasks
  open Microsoft.AspNetCore.Builder
  open Microsoft.AspNetCore.Hosting
  open Microsoft.AspNetCore.Http
  open Microsoft.Extensions.Configuration
  open FSharp.Control.Websockets

  // let echoWebSocket (httpContext : HttpContext) (next : unit -> Async<unit>) = async {
  //   if httpContext.WebSockets.IsWebSocketRequest then
  //       let! websocket  = httpContext.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
  //       // Create a thread-safe WebSocket from an existing websocket
  //       let threadSafeWebSocket = ThreadSafeWebSocket.createFromWebSocket websocket
  //       while threadSafeWebSocket.State = WebSocketState.Open do
  //           try
  //               let! result =
  //                    threadSafeWebSocket
  //                   |> ThreadSafeWebSocket.receiveMessageAsUTF8
  //               match result with
  //               | Ok(WebSocket.ReceiveUTF8Result.String text) ->
  //                   //Echo it back to the client
  //                   //do! WebSocket.sendMessageAsUTF8 websocket text
  //                   CLI.renderResponse text
  //               | Ok(WebSocket.ReceiveUTF8Result.Closed (status, reason)) ->
  //                   //printfn "Socket closed %A - %s" status reason
  //                   CLI.renderError ($"Socket Closed %A{status} - %s{reason}")
  //               | Error (ex) ->
  //                   //printfn "Receiving threw an exception %A" ex.SourceException
  //                   CLI.renderError $"Receiving threw an exception: %A{ex.SourceException}"
  //           with e ->
  //               //printfn "%A" e
  //               CLI.renderError ($"Error in catch block:\n  %A{e}")

  //   else
  //       do! next()
  // }

  //Convenience function for making middleware with F# asyncs and funcs
  // source: https://github.com/TheAngryByrd/FSharp.Control.WebSockets
  let fuse (middlware : HttpContext -> (unit -> Async<unit>) -> Async<unit>) (app:IApplicationBuilder) =
    app.Use(fun env next ->
                middlware env (next.Invoke >> Async.AwaitTask)
                |> Async.StartAsTask :> Task)

  let configureWebSockets (socket : Websocket.Socket) (appBuilder: IApplicationBuilder) =
    let hwsr = Websocket.handleWebSocketRequest socket (fun s -> ())
    appBuilder.UseWebSockets()
    //|> fuse (Websocket.echoWebSocket)
    |> fuse hwsr
    |> ignore

  let buildWebhost (port : int) (socket : Websocket.Socket) (handler : string -> unit) = 
    async {
      let configBuilder = ConfigurationBuilder()
      let configBuilder = configBuilder.AddInMemoryCollection()
      let config = configBuilder.Build()
      return
        WebHostBuilder()
          .UseConfiguration(config)
          .UseKestrel()
          .Configure(fun app -> configureWebSockets socket app)
          .UseUrls($"http://*:%i{port}")
          .Build()
    }

  let startWebHost (host : IWebHost) : Async<IWebHost> = async {
    do! host.StartAsync() |> Async.AwaitTask
    return host
  }