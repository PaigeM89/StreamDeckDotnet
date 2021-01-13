namespace Example.Shared

type ServerMessage =
| HelloWorld of string

type ClientMessage =
| HelloFromClient of string