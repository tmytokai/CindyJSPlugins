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

    $ cd build/plugins/
    $ ln -s ../../audio/plugins/audio/
    $ cd ..
    (for macOS: open package.json and set the version of node-sass to 4.11.0 or later)
    $ node make build=release audio
    $ cp build/js/audio.js ../docs/dist/latest/audio/
    $ cd ..

## Building the plugin of UnityCindy3D

    $ cd build/plugins/
    $ ln -s ../../unitycindy3d/plugins/unitycindy3d/
    $ cd ..
    $ node make build=release unitycindy3d
    $ cp build/js/UnityCindy3D.js ../docs/dist/latest/unitycindy3d/
    $ cd ..

## Building the 3D viewer of UnityCindy3D

    1. Start UnityEditor.
    2. Open "unitycindy3d/viewer" folder.
    3. Open "Viewer" scene.
    4. Select "File" menu > "Build Settings" submenu.
    5. Select "WebGL" and push the "Switch Platform" button.
    6. Push the "Build" button.
    7. Select any build folder and push the "Save" button.
    8. Move "(build folder)/Build" and "(build folder)/TemplateData" into "docs/dist/latest/unitycindy3d/viewer"
