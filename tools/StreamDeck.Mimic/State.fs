namespace StreamDeck.Mimic

open System
open StreamDeckDotnet.Types
open StreamDeckDotnet.Types.Received

module State =
  open System.Net.WebSockets
  open Microsoft.AspNetCore.Hosting
  open FSharp.Control.Websockets
  open FSharp.Control.Websockets.Stream
  open FSharp.Control.Websockets.ThreadSafeWebSocket

  type ActionInstance = {
    Context : Guid
    Coordinates : Received.Coordinates
    Socket : WebSocket option
  }

  module ActionInstance =
    let private makeCoords x y = 
      {
        Column = x
        Row = y
      }

    let init() = {
      Context = Guid.NewGuid()
      Coordinates = makeCoords 0 0
      Socket = None
    }

    let withCoords x y instance : ActionInstance = {
      instance with
        Coordinates = makeCoords x y
    }

    let withContext id instance : ActionInstance = { instance with Context = id }

  type ActionInstanceCommands = 
  | Create
  | Delete
  | UpdateCoordinates
  | ConnectWebsocket

  type State = {
    Actions : ActionInstance list
    /// index of the action in the list that is active
    ActiveAction : int option
  }

  module State =
    let init() = { Actions = []; ActiveAction = None }