
var path = require("path");
var CONFIG = {
  outputDir: "./output"
};

module.exports = {
  mode: "development",
  entry: "./StreamDeckDotnet.Fable.fsproj",
  output: {
    path: resolve(CONFIG.outputDir),
    filename: "bundle.js",
  },
  module: {
    rules: [{
      test: /\.fs(x|proj)?$/,
      use: "fable-loader"
    },
    {
      test: /\.css$/i,
      use: ["style-loader", "css-loader"],
    }]
  }
}

function resolve(filePath) {
  return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}
