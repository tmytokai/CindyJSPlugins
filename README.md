# CindyJSPlugins

go to [Web page](https://tmytokai.github.io/CindyJSPlugins/)

---

# Building

## Preparation

    $ git clone https://github.com/tmytokai/CindyJSPlugins.git
    $ cd CindyJSPlugins
    $ git clone https://github.com/CindyJS/CindyJS.git build
    $ patch -u build/make/build.js < make/build.js.patch

## Building the plugin of audio-plugin

    $ ln -rs audio/plugins/audio/ build/plugins/
    $ cd build
    $ node make build=release audio
    $ cp build/js/audio.js ../docs/dist/latest/audio/
    $ cd ..

## Building the plugin of UnityCindy3D

    $ ln -rs unitycindy3d/plugins/unitycindy3d/ build/plugins/
    $ cd build
    $ node make build=release unitycindy3d
    $ cp build/js/UnityCindy3D.js ../docs/dist/latest/unitycindy3d/
    $ cd ..

## Building the 3D viewer of UnityCindy3D

    1. Start UnityEditor.
    1. Open "unitycindy3d/viewer" folder.
    1. Open "Viewer" scene.
    1. Select "File" menu > "Build Settings" submenu.
    1. Select "WebGL".
    1. Push the "Build" button.
    1. Input any build folder in the name textbox and push the "Save" button.
    1. Move "(build folder)/Build" and "(build folder)/TemplateData" into "docs/dist/latest/unitycindy3d/viewer"
