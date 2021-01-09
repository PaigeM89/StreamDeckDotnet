namespace Example

open Serilog
open Serilog.Core
open Serilog.Events

type ThreadIdEnricher() =
  interface ILogEventEnricher with
    member this.Enrich(logEvent : LogEvent, propertyFactory: ILogEventPropertyFactory) =
      logEvent.AddPropertyIfAbsent(
        propertyFactory.CreateProperty(
          "ThreadId",
          System.Threading.Thread.CurrentThread.ManagedThreadId
       )
      )