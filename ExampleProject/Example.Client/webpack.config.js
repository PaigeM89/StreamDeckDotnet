// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

const { resolve } = require("path");
var path = require("path");

module.exports = {
    mode: "development",
    entry: "./src/App.fsproj",
    // output: {
    //     path: path.join(__dirname, "./public"),
    //     filename: "bundle.js",
    // },
    output: {
        path: resolve('./output'),
        filename: 'bundle.js',
        libraryTarget: 'var',
        library: 'EntryPoint'
    },
    devServer: {
        // port to inline is from elmish.bridge
        // port: 8090,
        // proxy: {
        //     '/devui': {
        //         target: 'http://localhost:' + port,
        //         changeOrigin: true
        //     },
        //     '/': {
        //         target: 'ws://localhost:' + port,
        //         ws: true
        //     }
        // },
        // contentBase: './public',
        // hot: true,
        // inline: true,

        // original config
        publicPath: "/",
        contentBase: "./public",
        port: 8090,
    },
    module: {
        rules: [{
            test: /\.fs(x|proj)?$/,
            use: "fable-loader"
        }]
    }
}