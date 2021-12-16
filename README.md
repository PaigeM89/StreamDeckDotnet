# StreamDeckDotnet

`StreamDeckDotnet` is a full framework for handling communication with an Elgato Stream Deck. It uses a Giraffe-like pipeline builder for simple, readable, and concise event pipelines.

Events are handled in a pipeline with filters:

```Fsharp
open StreamDeckDotnet
open StreamDeckDotnet.Routing.EventBinders
open StreamDeckDotnet.Types.Received

let routes : EventRoute = choose [
    // functions can be added to the pipeline, like this logging function,
    // which appends a log to the context's events to send back to the Stream Deck
    KEY_DOWN >=> Core.log "in KEY_DOWN handler" >=> tryBindKeyDownEvent errorHandler keyDownHandler
    WILL_APPEAR >=> Core.log "in WILL_APPEAR handler" >=> tryBindEvent errorHandler appearHandler
  ]
```

`tryBindKeyDownEvent` will bind the payload from a `KEY_DOWN` event and pass that to the given handler, allowing you to skip right to the parts you care about. 

The handler also gets an `EventContext`, which it writes outbound events to and can peek at event metadata when needed.

```Fsharp
let keyDownHandler (keyPayload : KeyPayload) next (ctx : EventContext) = async {
  match ctx.TryGetContextGuid() with
  | Some contextId ->
    // helper functions allow for chaining
    let ctx = ctx |> addLogToContext $"In Key Down Handler for context %O{contextId}" |> addShowOk
    return! next ctx
  | None ->
    // The context itself is an object, so it can have fields modified.
    ctx.AddLog($"In key down handler, no context was found")
    ctx.AddAlert()
    return! next ctx
}
```

Creating the stream deck itself only requires the application arguments and the routes:

```Fsharp
[<EntryPoint>]
let main argv =
  let args = ArgsParsing.parseArgs argv
  let client = StreamDeckClient(args, routes)
  client.Run()
```

## Testing and developing a plugin

On macOS:

Plugins live in `~/Library/Application Support/com.elgato.StreamDeck/Plugins/`
Logs live in `~/Library/Logs/StreamDeck/`

Debug a property inspector [from this link](http://localhost:23654) - 
  See [this link](https://developer.elgato.com/documentation/stream-deck/sdk/create-your-own-plugin/) from elgato for more info

elgato docs: https://developer.elgato.com/documentation/stream-deck/sdk/events-received/#didreceivesettings

# Stream Deck Mimic

This in-progress tool is designed to mimic a Stream Deck Application but with more visibility into what exactly is being sent & received. It is best to test with the real Stream Deck software when possible, but if you need to peek more into what is being sent, this tool can help.

Currently, the tool has barely any events implemented. Pull requests welcome! There's a lot I want to exapnd on with it and just haven't had the time for it yet.

-----------------------------------------------

# TODO

Interesting log when exiting the stream deck application:
```
07:32:45.0706          void ESDCustomPlugin::QuitPlugin(): Plugin 'Example Plugin' is still alive after closing the websocket. Quit it.
```

## General

* Test w/ software
* Test w/ streamdeck

## Example

[ ] Figure out websockets in Elmish
    * maybe try (Fable.Reaction)[https://fablereaction.readthedocs.io/en/latest/fable.reaction/getting_started.html] ?
    * maybe the model shouldn't be aware of the socket? this might make building a subscription easier, since the socket can invoke it by updating the model. i think.
[ ] And/Or Redo the example in react

## Mimic

[ ] Set action state
[ ] Set action coordinates
[ ] Batch send events with delays
  * allows testing "double tap key" event types (as an example)
  * allows stress testing
[ ] test multi instance & combine with batch send
[ ] Save last command run
[ ] Save/cache commands to run later 
[ ] Load manifest(s) for actions
[ ] Figure out packaging/distribution for consuming
[ ] Figure out how to host the property inspector as well
    * [ ] Tie the plugin & the backend together

## SD.NET

* [ ] Minimize deploy size, either as part of this package and/or as an example
* Do we need some way to send events outside of handling events? eg, timer?

## Documetnation

* start

---

## Builds

GitHub Actions |
:---: |
[![GitHub Actions](https://github.com/PaigeM89/StreamDeckDotnet/workflows/Build%20master/badge.svg)](https://github.com/PaigeM89/StreamDeckDotnet/actions?query=branch%3Amaster) |
[![Build History](https://buildstats.info/github/chart/PaigeM89/StreamDeckDotnet)](https://github.com/PaigeM89/StreamDeckDotnet/actions?query=branch%3Amaster) |

## NuGet 

Package | Stable | Prerelease
--- | --- | ---
StreamDeckDotnet | [![NuGet Badge](https://buildstats.info/nuget/StreamDeckDotnet)](https://www.nuget.org/packages/StreamDeckDotnet/) | [![NuGet Badge](https://buildstats.info/nuget/StreamDeckDotnet?includePreReleases=true)](https://www.nuget.org/packages/StreamDeckDotnet/)

---

### Developing

Make sure the following **requirements** are installed on your system:

- [dotnet SDK](https://www.microsoft.com/net/download/core) 3.0 or higher
- [Mono](http://www.mono-project.com/) if you're on Linux or macOS.

or

- [VSCode Dev Container](https://code.visualstudio.com/docs/remote/containers)


---

### Environment Variables

- `CONFIGURATION` will set the [configuration](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build?tabs=netcore2x#options) of the dotnet commands.  If not set, it will default to Release.
  - `CONFIGURATION=Debug ./build.sh` will result in `-c` additions to commands such as in `dotnet build -c Debug`
- `GITHUB_TOKEN` will be used to upload release notes and Nuget packages to GitHub.
  - Be sure to set this before releasing
- `DISABLE_COVERAGE` Will disable running code coverage metrics.  AltCover can have [severe performance degradation](https://github.com/SteveGilham/altcover/issues/57) so it's worth disabling when looking to do a quicker feedback loop.
  - `DISABLE_COVERAGE=1 ./build.sh`


---

### Building


```sh
> build.cmd <optional buildtarget> // on windows
$ ./build.sh  <optional buildtarget>// on unix
```

The bin of your library should look similar to:

```
$ tree src/MyCoolNewLib/bin/
src/MyCoolNewLib/bin/
└── Debug
    ├── net461
    │   ├── FSharp.Core.dll
    │   ├── MyCoolNewLib.dll
    │   ├── MyCoolNewLib.pdb
    │   ├── MyCoolNewLib.xml
    └── netstandard2.1
        ├── MyCoolNewLib.deps.json
        ├── MyCoolNewLib.dll
        ├── MyCoolNewLib.pdb
        └── MyCoolNewLib.xml

```

---

### Build Targets

- `Clean` - Cleans artifact and temp directories.
- `DotnetRestore` - Runs [dotnet restore](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-restore?tabs=netcore2x) on the [solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2019).
- [`DotnetBuild`](#Building) - Runs [dotnet build](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build?tabs=netcore2x) on the [solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2019).
- `DotnetTest` - Runs [dotnet test](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test?tabs=netcore21) on the [solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2019).
- `GenerateCoverageReport` - Code coverage is run during `DotnetTest` and this generates a report via [ReportGenerator](https://github.com/danielpalme/ReportGenerator).
- `WatchTests` - Runs [dotnet watch](https://docs.microsoft.com/en-us/aspnet/core/tutorials/dotnet-watch?view=aspnetcore-3.0) with the test projects. Useful for rapid feedback loops.
- `GenerateAssemblyInfo` - Generates [AssemblyInfo](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.applicationservices.assemblyinfo?view=netframework-4.8) for libraries.
- `DotnetPack` - Runs [dotnet pack](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-pack). This includes running [Source Link](https://github.com/dotnet/sourcelink).
- `SourceLinkTest` - Runs a Source Link test tool to verify Source Links were properly generated.
- `PublishToNuGet` - Publishes the NuGet packages generated in `DotnetPack` to NuGet via [paket push](https://fsprojects.github.io/Paket/paket-push.html).
- `GitRelease` - Creates a commit message with the [Release Notes](https://fake.build/apidocs/v5/fake-core-releasenotes.html) and a git tag via the version in the `Release Notes`.
- `GitHubRelease` - Publishes a [GitHub Release](https://help.github.com/en/articles/creating-releases) with the Release Notes and any NuGet packages.
- `FormatCode` - Runs [Fantomas](https://github.com/fsprojects/fantomas) on the solution file.
- `BuildDocs` - Generates Documentation from `docsSrc` and the [XML Documentation Comments](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/) from your libraries in `src`.
- `WatchDocs` - Generates documentation and starts a webserver locally.  It will rebuild and hot reload if it detects any changes made to `docsSrc` files, libraries in `src`, or the `docsTool` itself.
- `ReleaseDocs` - Will stage, commit, and push docs generated in the `BuildDocs` target.
- [`Release`](#Releasing) - Task that runs all release type tasks such as `PublishToNuGet`, `GitRelease`, `ReleaseDocs`, and `GitHubRelease`. Make sure to read [Releasing](#Releasing) to setup your environment correctly for releases.
---


### Releasing

- [Start a git repo with a remote](https://help.github.com/articles/adding-an-existing-project-to-github-using-the-command-line/)

```sh
git add .
git commit -m "Scaffold"
git remote add origin https://github.com/user/MyCoolNewLib.git
git push -u origin master
```

- [Create your NuGeT API key](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package#create-api-keys)
    - [Add your NuGet API key to paket](https://fsprojects.github.io/Paket/paket-config.html#Adding-a-NuGet-API-key)

    ```sh
    paket config add-token "https://www.nuget.org" 4003d786-cc37-4004-bfdf-c4f3e8ef9b3a
    ```

    - or set the environment variable `NUGET_TOKEN` to your key


- [Create a GitHub OAuth Token](https://help.github.com/articles/creating-a-personal-access-token-for-the-command-line/)
  - You can then set the environment variable `GITHUB_TOKEN` to upload release notes and artifacts to github
  - Otherwise it will fallback to username/password

- Then update the `CHANGELOG.md` with an "Unreleased" section containing release notes for this version, in [KeepAChangelog](https://keepachangelog.com/en/1.1.0/) format.

NOTE: Its highly recommend to add a link to the Pull Request next to the release note that it affects. The reason for this is when the `RELEASE` target is run, it will add these new notes into the body of git commit. GitHub will notice the links and will update the Pull Request with what commit referenced it saying ["added a commit that referenced this pull request"](https://github.com/TheAngryByrd/MiniScaffold/pull/179#ref-commit-837ad59). Since the build script automates the commit message, it will say "Bump Version to x.y.z". The benefit of this is when users goto a Pull Request, it will be clear when and which version those code changes released. Also when reading the `CHANGELOG`, if someone is curious about how or why those changes were made, they can easily discover the work and discussions.

Here's an example of adding an "Unreleased" section to a `CHANGELOG.md` with a `0.1.0` section already released.

```markdown
## [Unreleased]

### Added
- Does cool stuff!

### Fixed
- Fixes that silly oversight

## [0.1.0] - 2017-03-17
First release

### Added
- This release already has lots of features

[Unreleased]: https://github.com/user/MyCoolNewLib.git/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/user/MyCoolNewLib.git/releases/tag/v0.1.0
```

- You can then use the `Release` target, specifying the version number either in the `RELEASE_VERSION` environment
  variable, or else as a parameter after the target name.  This will:
  - update `CHANGELOG.md`, moving changes from the `Unreleased` section into a new `0.2.0` section
    - if there were any prerelease versions of 0.2.0 in the changelog, it will also collect their changes into the final 0.2.0 entry
  - make a commit bumping the version:  `Bump version to 0.2.0` and adds the new changelog section to the commit's body
  - publish the package to NuGet
  - push a git tag
  - create a GitHub release for that git tag

macOS/Linux Parameter:

```sh
./build.sh Release 0.2.0
```

macOS/Linux Environment Variable:

```sh
RELEASE_VERSION=0.2.0 ./build.sh Release
```


