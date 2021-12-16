namespace StreamDeckDotnet

#if !FABLE_COMPILER
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

  let inline failWith<'a> (ex : exn) =
    Task<'a>.FromException(ex)
#endif
// [<RequireQualifiedAccess>]
// module internal Exception =
//   open System.Runtime.ExceptionServices

//   let Reraise ex =
//       (ExceptionDispatchInfo.Capture ex).Throw ()
//       Unchecked.defaultof<_>

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
