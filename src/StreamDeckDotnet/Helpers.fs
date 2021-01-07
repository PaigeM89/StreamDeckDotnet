namespace StreamDeckDotnet

module internal Task =
  open System
  open System.Threading.Tasks

  let map (projection:'a -> 'b) (task: Task<'a>) =
    let r = new TaskCompletionSource<'b>()
    task.ContinueWith(fun (self: Task<_>) ->
        if self.IsFaulted then r.SetException(self.Exception.InnerExceptions)
        elif self.IsCanceled then r.SetCanceled()
        else r.SetResult(projection(self.Result))) |> ignore
    r.Task

[<AutoOpen>]
module internal Async =
  let lift a' = async { return a' }

  let tryFinally (finalize: Async<unit>) (body: Async<'x>) : Async<'x> = async {
    let! result = Async.Catch body
    do! finalize
    return match result with
           | Choice1Of2 value -> value
           | Choice2Of2 exn -> raise exn
  }

module internal Logger =
  open StreamDeckDotnet.Logging

  /// Create a Log with the specified message.
  let message m = Log.setMessage m

  /// Add a Parameter to a Log.
  let withParam v = Log.addParameter v

  /// Add a Value for a specified Key in a Log. Does not destructure the Value.
  let withValue k v = Log.addContext k v

  /// Add a Value for a specified Key in a Log. Destructures the Value.
  /// Use this on records & objects. Primitives can use `withValue`.
  /// Find out more about Destructuring here:
  ///   https://github.com/serilog/serilog/wiki/Structured-Data#preserving-object-structure
  let withObject k v = Log.addContextDestructured k v

  /// Add an Exn to a Log.
  let withExn e = Log.addExn e

  /// Add an Exception to a log.
  let withException e = Log.addException e

  module Operators =
    /// Create a Log with the specified message.
    let (!!) m = message m

    /// Add a Parameter to a Log.
    let (>>!) log v = log >> withParam v

    /// Add a Value for the specified Key in a Log. Does not destructure the Value.
    let (>>!-) log (k, v) = log >> withValue k v

    /// Add a Value for a specified Key in a Log. Destructures the Value.
    let (>>!+) log (k, v) = log >> withObject k v

    /// Add an Exception to a log.
    let (>>!!) log e = log >> withException e