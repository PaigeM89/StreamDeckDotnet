namespace StreamDeckDotnet

module Task =
  open System
  open System.Threading.Tasks

  let map (projection:'a -> 'b) (task: Task<'a>) =
    let r = new TaskCompletionSource<'b>()
    task.ContinueWith(fun (self: Task<_>) ->
        if self.IsFaulted then r.SetException(self.Exception.InnerExceptions)
        elif self.IsCanceled then r.SetCanceled()
        else r.SetResult(projection(self.Result))) |> ignore
    r.Task