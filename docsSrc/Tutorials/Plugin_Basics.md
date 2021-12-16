# Plugin Basics

First, familiarize yourself with the [Elgato SDK documentation](https://developer.elgato.com/documentation/stream-deck/sdk/overview/). That is the Actual Source Of Truth for all things streamdeck, and if any information there contradicts what is here, then their docs are probably correct.

These docs will give a brief overview of general plugin architecture, and then apply that architecture to this library, to produce a plugin.

# The Components of a Plugin

A plugin has 2 parts: the **Property Inspector**, which is the UI the user interacts with to configure an action, and the **Plugin**, the application launched to manage all instances of all actions allowed by that plugin. Nothing in the Plugin itself is supposed to be edited or managed by the user; all interaction should go through the Property Inspector.

## Property Inspector

The Property Inspector (PI) is created each time the user clicks an action in the Stream Deck Application to view or edit that action's settings. On creation, the application will look for a specific javascript function - `connectElgatoStreamDeckSocket(inPort, inPluginUUID, inRegisterEvent, inInfo)` - and invoke that function to connect the Stream Deck Application to the PI via websocket.

It's possible to take advantage of this library's routing by using Fable to write your javascript. See `Example.Client` in the `ExampleProject` folder in the repository for more information.

## Plugin

The backend Plugin is started by the Stream Deck Application, which manages all running plugin instances. It is given a set of command line arguments when starting: `-port <int> -pluginUUID <UUID> -registerEvent "registerEvent" -info "<some JSON object>"`, and must use those arguments when creating the web socket to communicate with the Stream Deck Application. 

This library can handle args parsing for you automatically, with `ArgsParsing.parseArgs <args>`. Pass the result of that call into `StreamDeckClient` along with your routes and your plugin is good to go.

```FSharp
[<EntryPoint>]
let main argv =
  let args = ArgsParsing.parseArgs argv
  let client = StreamDeckClient(args, routes)
  client.Run()
```