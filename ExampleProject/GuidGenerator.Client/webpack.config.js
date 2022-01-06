// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

const { resolve } = require("path");
var path = require("path");

module.exports = {
    mode: "development",
    entry: "./src/App.fs.js",
    output: {
        path: resolve('./public'),
        filename: 'bundle.js',
        libraryTarget: 'var',
        library: 'EntryPoint'
    },
    devServer: {
        publicPath: "/",
        contentBase: "./public",
        port: 8090,
    },
}
