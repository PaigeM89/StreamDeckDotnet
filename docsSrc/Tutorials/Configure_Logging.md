# Configure Logging

The Stream Deck Library has logging configured in a few areas, and you can hook into that logging with Serilog.

Configure an `appsettings.json` file that you load into a `ConfigurationBuilder`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override":{
        "StreamDeckDotnet":"Warning"
      }
    }
  }
}
```

This will log local application logs at the Debug level, and log anything from `StreamDeckDotnet` at the Warning level.

Here's how that file is used in the logging configuration:

```FSharp
// check out ExampleProject/Example/Program.fs to see this in context
open Serilog
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Configuration.Json

let configuration =
  ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build()

let log =
  LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .WriteTo.File("log.txt",
      outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:W3}] ({ThreadId}) {Message}{NewLine}{Exception}"
    )
    .WriteTo.Console(
      outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:W3}] ({ThreadId}) {Message}{NewLine}{Exception}"
    )
    .CreateLogger()
```


