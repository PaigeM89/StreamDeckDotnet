namespace StreamDeckDotnet.Tests

open System
open System.Text
open System.Net
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks
open Expecto
open FSharp.Control.Tasks.Affine
open FsToolkit.ErrorHandling

open StreamDeckDotnet
open StreamDeckDotnet.Websockets

module Async =
    let lift a' = async { return a' }

module Task =
  let lift a = task { return a }

module WebsocketTests =
  let BufferSize = 1024 * 1024
  let cancelSource = new CancellationTokenSource()
  let token() = cancelSource.Token

  let stringToBytes (s : string) =
    Encoding.UTF8.GetBytes(s)

  let inline fillArraySegment (arrSeg : byref<ArraySegment<'a>>) (value : 'a array) =
    for i in 0 .. value.Length - 1 do
      arrSeg.[i] <- value.[i]

  let mockSocketAwait value (arrayBuffer: byref<ArraySegment<byte>>) (ct : CancellationToken) =
    let numBytes = Array.length value
    //arrayBuffer.Add value
    fillArraySegment &arrayBuffer value
    let retval = WebSocketReceiveResult(numBytes, WebSocketMessageType.Text, true)
    Task.lift retval

  [<Tests>]
  let tests = 
    testList "Web socket tests" [
      testList "await socket and fill buffer" [
        // testTask "Empty string with EOM returns empty string" {
        //   let input = "" |> stringToBytes
        //   //let mutable buffer = ArraySegment<_>(Array.zeroCreate (BufferSize))
        //   let func = mockSocketAwait input //&buffer input
        //   let! result = Websockets.awaitSocketAndFillBuffer func 
        //   Expect.equal true false "lol"
        // }
      ]
    ]