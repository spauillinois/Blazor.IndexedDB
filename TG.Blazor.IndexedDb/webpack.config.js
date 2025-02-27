import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import webpack from 'webpack';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

export default (env, args) =>({
    resolve: {
        extensions: ['.ts', '.js']
    },
    devtool: args.mode === 'development' ? 'inline-source-map' : 'none',
    module: {
        rules: [
            {
                test: /\.ts?$/,
                loader: 'ts-loader'
            }
        ]
    },
    entry: {
        "indexedDb.Blazor": './client/InitialiseIndexDbBlazor.ts'
    },
    optimization: {
        runtimeChunk: true,
    },
    output: {
        path: join(__dirname, '/wwwroot'),
        filename: '[name].js'
    }
});
