# Fable Minimal App

This example builds a barebones Property Inspector using some elements from `StreamDeckDotnet` for event handling.

This example is not yet complete.

## Requirements

* [dotnet SDK](https://www.microsoft.com/net/download/core) 3.0 or higher
* [node.js](https://nodejs.org) with [npm](https://www.npmjs.com/)
* An F# editor like Visual Studio, Visual Studio Code with [Ionide](http://ionide.io/) or [JetBrains Rider](https://www.jetbrains.com/rider/).

## Building and running the app

* Install JS dependencies: `yarn install`
* Install F# dependencies: `yarn build`
* Publish the app: `yarn prod`
* Copy everything to deploy directory: `./copyToDeploy.sh`

Once the deploy folder (`../jorg.StreamDeckDotnet.Example.sdPlugin`) contains both the back end plugin and the property inspector, you can copy that plugin to your plugins directory and test it.

## Project structure

### npm

JS dependencies are declared in `package.json`, while `package-lock.json` is a lock file automatically generated.

### Webpack

[Webpack](https://webpack.js.org) is a JS bundler with extensions, like a static dev server that enables hot reloading on code changes. Fable interacts with Webpack through the `fable-loader`. Configuration for Webpack is defined in the `webpack.config.js` file. Note this sample only includes basic Webpack configuration for development mode, if you want to see a more comprehensive configuration check the [Fable webpack-config-template](https://github.com/fable-compiler/webpack-config-template/blob/master/webpack.config.js).


### Web assets

The `index.html` file and other assets like an icon can be found in the `public` folder.

## Running

The app launches when `connectElgatoStreamDeckSocket` is called.

Try calling it with this:

```
EntryPoint.connectElgatoStreamDeckSocket(1349, '806a7b6665e64acc8f264167c5ba4f09', 'registerPlugin', '', '');
```

