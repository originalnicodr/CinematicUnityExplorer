<p align="center">
  <img align="center" src="img/icon.png">
</p>

<p align="center">
  🎥 Fork of the excellent UnityExplorer mod made by <a href="https://github.com/sinai-dev">sinai-dev</a> focused on providing tools for creating marketing material for Unity games. You can check out the fork features here.
</p>
<p align="center">
  To see the forks features I have worked on so far check out the <a href="#fork-features">Fork Features</a> section.
</p>
<p align="center">
  ✔️ Supports most Unity versions from 5.2 to 2021+ (IL2CPP and Mono).
</p>
<p align="center">
  ✨ Powered by <a href="https://github.com/yukieiji/UniverseLib">UniverseLib</a>
</p>

# Releases  [![](https://img.shields.io/github/downloads/originalnicodr/UnityExplorer/total.svg)](../../releases)

[![](https://img.shields.io/github/release/originalnicodr/UnityExplorer.svg?label=version)](../../releases/latest) [![](https://img.shields.io/github/workflow/status/originalnicodr/UnityExplorer/Build%20UnityExplorer)](https://github.com/originalnicodr/UnityExplorer/actions) [![](https://img.shields.io/github/downloads/originalnicodr/UnityExplorer/latest/total.svg)](../../releases/latest)

⚡ Thunderstore releases: [BepInEx Mono](https://thunderstore.io/package/originalnicodr/UnityExplorer) | [BepInEx IL2CPP](https://gtfo.thunderstore.io/package/originalnicodr/UnityExplorer_IL2CPP) | [MelonLoader IL2CPP](https://boneworks.thunderstore.io/package/originalnicodr/UnityExplorer_IL2CPP_ML)

## Release schedule

Nightly builds can be found [here](https://github.com/originalnicodr/UnityExplorer/actions).

## BepInEx

| Release | IL2CPP(CoreCLR) | IL2CPP(Unhollower) | Mono |
| ------- | ------ |  ------ | ---- |
| BIE 6.X be.647+ | ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx.Unity.IL2CPP.CoreCLR.zip) | ✖️ n/a | ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx6.Unity.Mono.zip) |
| BIE 6.X be.472 to be.577 | ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx.IL2CPP.CoreCLR.zip) | ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx.IL2CPP.zip)  | ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx6.Mono.zip) |
| BIE 5.X | ✖️ n/a |  ✖️ n/a |  ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx5.Mono.zip) |

1. Unzip the release file into a folder
2. Take the `plugins/originalnicodr-UnityExplorer` folder and place it in `BepInEx/plugins/`

<i>Note: BepInEx 6 is obtainable via [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be)</i>

## MelonLoader

| Release | IL2CPP | Mono |
| ------- | ------ | ---- |
| ML 0.6.x  | ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.IL2CPP.CoreCLR.zip) | ✖️ |
| ML 0.6(only alpha build)  | ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.IL2CPP.net6preview.zip) | ✖️ |
| ML 0.5  | ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.IL2CPP.zip) | ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.Mono.zip) | 

1. Unzip the release file into a folder
2. Copy the DLL inside the `Mods` folder into your MelonLoader `Mods` folder
3. Copy all of the DLLs inside the `UserLibs` folder into your MelonLoader `UserLibs` folder

## Standalone

| IL2CPP | Mono |
| ------ | ---- |
| ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.IL2CPP.zip) | ✅ [link](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.Mono.zip) | 

The standalone release can be used with any injector or loader of your choice, but it requires you to load the dependencies manually.

1. Ensure the required libs are loaded - UniverseLib, HarmonyX and MonoMod. Take them from the [`UnityExplorer.Editor`](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.Editor.zip) release if you need them.
2. For IL2CPP, load Il2CppAssemblyUnhollower and start an [Il2CppAssemblyUnhollower runtime](https://github.com/knah/Il2CppAssemblyUnhollower#required-external-setup)
2. Load the UnityExplorer DLL
3. Create an instance of Unity Explorer with `UnityExplorer.ExplorerStandalone.CreateInstance();`
4. Optionally subscribe to the `ExplorerStandalone.OnLog` event to handle logging if you wish

## Unity Editor

1. Download the [`UnityExplorer.Editor`](https://github.com/originalnicodr/UnityExplorer/releases/latest/download/UnityExplorer.Editor.zip) release.
2. Install the package, either by using the Package Manager and importing the `package.json` file, or by manually dragging the folder into your `Assets` folder.
3. Drag the `Runtime/UnityExplorer` prefab into your scene, or create a GameObject and add the `Explorer Editor Behaviour` script to it.

# Common issues and solutions

Although UnityExplorer should work out of the box for most Unity games, in some cases you may need to tweak the settings for it to work properly.

To adjust the settings, open the config file:
* BepInEx: `BepInEx\config\com.sinai.unityexplorer.cfg`
* MelonLoader: `UserData\MelonPreferences.cfg`
* Standalone: `originalnicodr-UnityExplorer\config.cfg`

Try adjusting the following settings and see if it fixes your issues:
* `Startup_Delay_Time` - increase to 5-10 seconds (or more as needed), can fix issues with UnityExplorer being destroyed or corrupted during startup.
* `Disable_EventSystem_Override` - if input is not working properly, try setting this to `true`.

If these fixes do not work, please create an issue in this repo and I'll do my best to look into it.

# Features

<p align="center">
  <a href="https://raw.githubusercontent.com/originalnicodr/UnityExplorer/master/img/preview.png">
    <img src="img/preview.png" />
  </a>
</p>

### Inspector API

If you want to inspect an object or Type from outside the C# console, use the `InspectorManager` class:

**To inspect an object:**
```csharp
UnityExplorer.InspectorManager.Inspect(theObject);
```

**To inspect a Type:**
```cs
UnityExplorer.InspectorManager.Inspect(typeof(SomeClass));
```

### Object Explorer

* Use the <b>Scene Explorer</b> tab to traverse the active scenes, as well as the DontDestroyOnLoad and HideAndDontSave objects.
  * The "HideAndDontSave" scene contains objects with that flag, as well as Assets and Resources which are not in any scene but behave the same way.
  * You can use the Scene Loader to easily load any of the scenes in the build (may not work for Unity 5.X games)
* Use the <b>Object Search</b> tab to search for Unity objects (including GameObjects, Components, etc), C# Singletons or Static Classes.
  * Use the UnityObject search to look for any objects which derive from `UnityEngine.Object`, with optional filters
  * The singleton search will look for any classes with a typical "Instance" field, and check it for a current value. This may cause unexpected behaviour in some IL2CPP games as we cannot distinguish between true properties and field-properties, so some property accessors will be invoked.

### Inspector

The inspector is used to see detailed information on objects of any type and manipulate their values, as well as to inspect C# Classes with static reflection.

* The <b>GameObject Inspector</b> (tab prefix `[G]`) is used to inspect a `GameObject`, and to see and manipulate its Transform and Components.
  * You can edit any of the input fields in the inspector (excluding readonly fields) and press <b>Enter</b> to apply your changes. You can also do this to the GameObject path as a way to change the GameObject's parent. Press the <b>Escape</b> key to cancel your edits.
  * <i>note: When inspecting a GameObject with a Canvas, the transform controls may be overridden by the RectTransform anchors.</i>
* The <b>Reflection Inspectors</b> (tab prefix `[R]` and `[S]`) are used for everything else
  * Automatic updating is not enabled by default, and you must press Apply for any changes you make to take effect.
  * Press the `▼` button to expand certain values such as strings, enums, lists, dictionaries, some structs, etc
  * Use the filters at the top to quickly find the members you are looking for
  * For `Texture2D`, `Image`, `Sprite` and `Material` objects, there is a `View Texture` button at the top of the inspector which lets you view the Texture(s) and save them as a PNG file. 
  * For `AudioClip` objects there is a `Show Player` button which opens an audio player widget. For clips which are loaded as `DecompressOnLoad`, there is also a button to save them to a `.wav` file. 

### C# Console

* The C# Console uses the `Mono.CSharp.Evaluator` to define temporary classes or run immediate REPL code.
* You can execute a script automatically on startup by naming it `startup.cs` and placing it in the `originalnicodr-UnityExplorer\Scripts\` folder (this folder will be created where you placed the DLL file).
* See the "Help" dropdown in the C# console menu for more detailed information.

### Hook Manager

* The Hooks panel allows you to hook methods at the click of a button for debugging purposes.
  * Simply enter any class and hook the methods you want from the menu. 
  * You can edit the source code of the generated hook with the "Edit Hook Source" button. Accepted method names are `Prefix` (which can return `bool` or `void`), `Postfix`, `Finalizer` (which can return `Exception` or `void`), and `Transpiler` (which must return `IEnumerable<HarmonyLib.CodeInstruction>`). You can define multiple patches if you wish.

### Mouse-Inspect

* The "Mouse Inspect" dropdown in the "Inspector" panel allows you to inspect objects under the mouse.
  * <b>World</b>: uses Physics.Raycast to look for Colliders
  * <b>UI</b>: uses GraphicRaycasters to find UI objects

### Freecam

* UnityExplorer provides a basic Free Camera which you can control with your keyboard and mouse.
* Unlike all other features of UnityExplorer, you can still use Freecam while UnityExplorer's menu is hidden.
* Supports using the game's main Camera or a separate custom Camera.
* See the Freecam panel for further instructions and details.

### Clipboard

* The "Clipboard" panel allows you to see your current paste value, or clear it (resets it to `null`)
  * Can copy the value from any member in a Reflection Inspector, Enumerable or Dictionary, and from the target of any Inspector tab
  * Can paste values onto any member in a Reflection Inspector
  * Non-parsable arguments in Method/Property Evaluators allow pasting values
  * The C# Console has helper methods `Copy(obj)` and `Paste()` for accessing the Clipboard

### Settings

* You can change the settings via the "Options" tab of the menu, or directly from the config file.
  * BepInEx: `BepInEx\config\com.sinai.unityexplorer.cfg`
  * MelonLoader: `UserData\MelonPreferences.cfg`
  * Standalone `{DLL_location}\originalnicodr-UnityExplorer\config.cfg`

# Building

1. Run the `build.ps1` powershell script to build UnityExplorer. Releases are found in the `Release` folder.

Building individual configurations from your IDE is fine, though note that the intial build process builds into `Release/<version>/...` instead of the subfolders that the powershell script uses. Batch building is not currently supported with the project.

# Fork Features

Keep in mind this is a WIP and I am still working on ironing out some stuff. To see the things I want to improve head out to the [issues page](https://github.com/originalnicodr/UnityExplorer/issues).

## Improved Freecam

- The Freecam can now move when the game is paused.
- Added tilt support for the camera with `Q` / `E`
- Added FoV control support with `Numpad +` / `Numpad -`
- Added another modifier key to move the camera in smaller increments `Alt`
- Added a hotkey to pause the game with `Pause` (also called `Break`)

## Lights manager

It allows you to spawn PointLights and SpotLights, as well as toggle the game's original lights to allow you to relight the scene however you want (a task that is pretty common for screenshots and lighting artists). Please note that this only turns off scene lights and won't turn off ambient light, lights created by emissive materials, effects, or shaders.

You can edit the light parameters by opening the object in the Object Explorer tab for now, but I plan on creating its own window to trim the available settings to only the things the user would be interested in.

If you want to move an already created light you can use the "Move to Camera" option after positioning the camera. But if you need to fine-tune its position or orientation you can do so by moving clicking on the Config button, Inspect Game Object, and editing the position and rotation there.

## Camera paths

It allows you to create nodes to build camera paths for videos and cinematics.

Features include:

- Add and delete camera nodes
- Copy camera position and rotation to a node
- Copy camera FoV to a node
- Move the camera position and rotation to the ones in the node
- Edit the number of frames it takes the camera to move from one specific node to the next.
- Close the camera path in a loop

# Acknowledgments

* [Sinai-dev](https://github.com/sinai-dev/) for his incredible work on the original UnityExplorer.
* [ManlyMarco](https://github.com/ManlyMarco) for [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor) \[[license](THIRDPARTY_LICENSES.md#runtimeunityeditor-license)\], the ScriptEvaluator from RUE's REPL console was used as the base for UnityExplorer's C# console.
* [Geoffrey Horsington](https://github.com/ghorsington) for [mcs-unity](https://github.com/sinai-dev/mcs-unity) \[no license\], used as the `Mono.CSharp` reference for the C# Console.

### Disclaimer

UnityExplorer is in no way associated with Unity Technologies. "Unity", Unity logos, and other Unity trademarks are trademarks or registered trademarks of Unity Technologies or its affiliates in the U.S. and elsewhere.
