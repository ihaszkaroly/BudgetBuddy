// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

var path = require("path");

module.exports = {
    entry: './src/Main.fs.js',
    output: {
        path: path.join(__dirname, './dist'),
        filename: 'bundle.js'
    },
    mode: 'development',
    devServer: {
        static: {
            directory: path.join(__dirname, './public')
        },
        port: 8080
    },
    module: {
        rules: [
            {
                test: /\.fs(x|proj)?$/,
                use: {
                    loader: 'fable-loader'
                }
            }
        ]
    },
    resolve: {
        modules: ["node_modules", "src"]
    }
};
