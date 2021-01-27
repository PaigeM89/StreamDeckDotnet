namespace StreamDeck.Mimic

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

  let echoWebSocket (httpContext : HttpContext) (next : unit -> Async<unit>) = async {
    if httpContext.WebSockets.IsWebSocketRequest then
        let! websocket  = httpContext.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
        // Create a thread-safe WebSocket from an existing websocket
        let threadSafeWebSocket = ThreadSafeWebSocket.createFromWebSocket websocket
        while threadSafeWebSocket.State = WebSocketState.Open do
            try
                let! result =
                     threadSafeWebSocket
                    |> ThreadSafeWebSocket.receiveMessageAsUTF8
                match result with
                | Ok(WebSocket.ReceiveUTF8Result.String text) ->
                    //Echo it back to the client
                    //do! WebSocket.sendMessageAsUTF8 websocket text
                    CLI.renderResponse text
                | Ok(WebSocket.ReceiveUTF8Result.Closed (status, reason)) ->
                    //printfn "Socket closed %A - %s" status reason
                    CLI.renderError ($"Socket Closed %A{status} - %s{reason}")
                | Error (ex) ->
                    //printfn "Receiving threw an exception %A" ex.SourceException
                    CLI.renderError $"Receiving threw an exception: %A{ex.SourceException}"
            with e ->
                //printfn "%A" e
                CLI.renderError ($"Error in catch block:\n  %A{e}")

    else
        do! next()
  }

  //Convenience function for making middleware with F# asyncs and funcs
  // source: https://github.com/TheAngryByrd/FSharp.Control.WebSockets
  let fuse (middlware : HttpContext -> (unit -> Async<unit>) -> Async<unit>) (app:IApplicationBuilder) =
    app.Use(fun env next ->
                middlware env (next.Invoke >> Async.AwaitTask)
                |> Async.StartAsTask :> Task)

  let configureWebSockets (appBuilder: IApplicationBuilder) =
    appBuilder.UseWebSockets()
    |> fuse (echoWebSocket)
    |> ignore

  let buildWebhost (port : int) = 
    async {
      let configBuilder = ConfigurationBuilder()
      let configBuilder = configBuilder.AddInMemoryCollection()
      let config = configBuilder.Build()
      return
        WebHostBuilder()
          .UseConfiguration(config)
          .UseKestrel()
          .Configure(fun app -> configureWebSockets app)
          .UseUrls($"http://*:%i{port}")
          .Build()
    }

  let startWebHost (host : IWebHost) : Async<IWebHost> = async {
    do! host.StartAsync() |> Async.AwaitTask
    return host
  }