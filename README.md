# CindyJSPlugins

# Building

## Preparation

    ```
    $ git clone https://github.com/tmytokay/CindyJSPlugins.git
    $ cd CindyJSPlugins
    $ git clone https://github.com/CindyJS/CindyJS.git build
    $ patch -u build/make/build.js < make/build.js.patch
    ```

## Building the audio-plugin

    ```
    $ ln -rs audio/plugins/audio/ build/plugins/
    $ cd build
    $ node make build=release audio
    $ cp build/js/audio.js ../docs/dist/latest/audio/
    $ cd ..
    ```
