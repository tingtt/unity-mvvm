# DesignDoc: Code Generation

## Abstract/Summary

This document describes the code generation system that provides type-safe accessors for Unity assets (scenes and audio files). The system automatically generates C# code from project assets, enabling compile-time type checking and reducing runtime errors.

## Background/Motivation

In standard Unity development, asset references are typically managed through string-based paths or manual references, which leads to several problems:

- **Runtime errors**: Typos in asset paths are only detected at runtime
- **Refactoring difficulty**: Renaming assets requires manual updates across the codebase
- **Poor discoverability**: Developers must remember exact asset names and paths
- **Lack of type safety**: No compile-time validation for asset loading operations

This code generation system addresses these issues by automatically creating type-safe accessors that mirror the project's asset structure, aligning with the project's MVVM paradigm where C# scripts should be independent and reviewable without launching Unity.

## Goals

- Type safe scene loading calls
- Type safe audio asset loading calls
- Type safe GameObject references with hierarchical structure

## Usage

```sh
make gen
```

## Known errors

### Unity reported an compile error

```text
Unity reported an compile error.
Please fix compile error or stash changed scripts outside Editor.

To proceed, temporarily stash all non-Editor C# scripts:
        git stash push -u -m "stash non-Editor C# scripts" -- $(ls Assets/**/*.(cs|meta) | grep -v 'Assets/Editor/')
        make gen
        git stash pop
```

When compile error occurred, Unity cannot run code generation scripts.
Resolve compile errors or follow displayed command to stash it.

## Proposed Design

## `gen` script (in [`Makefile`](../Makefile))

`gen` script runs `Generate.Run()` in [`Assets/Editor/Generate.cs`](../Assets/Editor/Generate.cs) via Unity CLI.
Unity CLI path specified with make ENV `UNITY_VERSION`, `UNITY_PATH_MAC` and `UNITY_PATH_WIN`.

## [`Assets/Editor/Generate.cs`](../Assets/Editor/Generate.cs)

`Run()` calls [generators](#generators).

### Generators

in [`Assets/Editor/Generator`](../Assets/Editor/Generator)

#### [AssetAccessor Generator](../Assets/Editor/Generator/AssetAccessor/AssetAccessor.cs)

Generating accessor ([Assets/Generated/AssetAccessor](../Assets/Generated/AssetAccessor/)) for scenes, audio assets, and GameObjects.

Scenes can be loaded with:

```cs
AssetsAccessor.Scene.SceneA.Load()
```

Audio asset can be loaded with:

```cs
AudioClip seA = AssetsAccessor.Audio.SE.SeA.Load()
```

GameObjects can be accessed with hierarchical structure matching Unity's hierarchy tree:

```cs
// Access GameObject directly
GameObject canvas = AssetAccessor.Scene.Menu.GameObject.Canvas.Get()

// Access child GameObject (hierarchical)
GameObject background = AssetAccessor.Scene.Menu.GameObject.Canvas.Background.Get()

// Access components (type-safe)
Image backgroundImage = AssetAccessor.Scene.Menu.GameObject.Canvas.Background.Component.Image.Get()
RectTransform rectTransform = AssetAccessor.Scene.Menu.GameObject.Canvas.Background.Component.RectTransform.Get()
```

##### GameObject Accessor Implementation Details

The GameObject accessor generator parses Unity scene files (.unity) in YAML format to extract:

1. **GameObject definitions** with their unique IDs and names
2. **Transform/RectTransform blocks** to map component IDs to GameObject IDs
3. **Parent-child relationships** through `m_Father` references in Transform blocks
4. **Component information** from MonoBehaviour and standard Unity component blocks

The generator creates a hierarchical class structure that mirrors Unity's scene hierarchy:

- Root GameObjects become top-level classes under `AssetAccessor.Scene.<SceneName>.GameObject`
- Child GameObjects become nested classes inside their parent's class
- Each GameObject class provides:
  - `Get()` method to retrieve the GameObject instance
  - Nested `Component` class containing type-safe accessors for each attached component

**Component Detection:**

The generator automatically detects and creates type-safe accessors for:

- **Unity UI components**: Image, Button, Text, Canvas, CanvasScaler, GraphicRaycaster, etc.
- **Standard Unity components**: Transform, RectTransform, Camera, AudioListener, Light, etc.
- **Known third-party packages**: TextMeshPro (TMPro.*), Cinemachine, Input System
- **Custom MonoBehaviour scripts**: User-defined scripts attached to GameObjects

Components are detected by parsing:

- `MonoBehaviour` blocks with `m_EditorClassIdentifier` (for UI and third-party components)
- Standard Unity component blocks by their class IDs (!u!223 for Canvas, !u!224 for RectTransform, etc.)

Each detected component gets its own nested class under `GameObject.<Name>.Component.<ComponentName>` with a type-safe `Get()` method that returns the specific component type.

**Custom Script Accessor Generation:**

For custom MonoBehaviour scripts attached to GameObjects, the generator provides enhanced accessibility:

1. **Script instance access**: Available through `Component.Script.Get<ScriptName>()` method
2. **Direct member access**: Public properties, methods, and fields are exposed directly on the GameObject class for convenient access
3. **Nested type resolution**: Types defined within the script class (enums, nested classes) are automatically resolved with proper aliasing

Example for a `ModeToggle` script with a public `Mode` enum and `CurrentMode` property:

```csharp
// Access the ModeToggle GameObject
GameObject toggle = AssetAccessor.Scene.WaitAndSettingForOwner.GameObject.Canvas.Panel.ModeToggle.Get()

// Get the script instance directly
ModeToggle script = AssetAccessor.Scene.WaitAndSettingForOwner.GameObject.Canvas.Panel.ModeToggle.Component.Script.GetModeToggle()

// Access public members directly (no need to call GetModeToggle() first)
ModeToggle.Mode currentMode = AssetAccessor.Scene.WaitAndSettingForOwner.GameObject.Canvas.Panel.ModeToggle.CurrentMode
AssetAccessor.Scene.WaitAndSettingForOwner.GameObject.Canvas.Panel.ModeToggle.CurrentMode = ModeToggle.Mode.Expert
```

**Custom Script Member Parsing:**

The generator scans custom script source files (`.cs` files in Assets directory) to extract:

- **Public properties**: With both getters and setters (read-write properties)
- **Public methods**: Excluding Unity lifecycle methods (Awake, Start, Update, etc.)
- **Method parameters**: Including proper type resolution for nested types

**Type Alias Handling:**

To avoid name conflicts between GameObject names and script class names (e.g., a GameObject named "ModeToggle" with an attached "ModeToggle" script), the generator uses C# using aliases:

```csharp
// using PrefabScript<ClassName> = <ClassName>;
using PrefabScriptModeToggle = ModeToggle;
```

This allows the generated code to reference both the GameObject accessor class and the script type without ambiguity. Nested types defined within custom scripts are automatically prefixed with the aliased class name (e.g., `Mode` becomes `PrefabScriptModeToggle.Mode`).

**PrefabInstance Support:**

The generator fully supports PrefabInstances in Unity scenes, treating them as regular GameObjects in the hierarchy:

- **Virtual GameObject IDs**: PrefabInstances are assigned synthetic IDs (`prefab_<id>`) to enable hierarchy tracking
- **Parent-child relationships**: PrefabInstances respect `m_TransformParent` references to appear under correct parent GameObjects
- **Component extraction**: Components are loaded from both sources:
  - **Scene overrides**: Components added or modified in the scene (`m_AddedComponents`)
  - **Prefab definitions**: Original components from the prefab file itself
- **GUID-based resolution**: Prefab files are located by searching `.meta` files for matching GUIDs
- **Stripped GameObject mapping**: Stripped GameObjects and components from PrefabInstances are mapped to virtual IDs

When a PrefabInstance is detected in the scene:

1. The generator extracts the prefab GUID from the `PrefabInstance` block
2. Searches all `.meta` files in the Assets directory to locate the actual prefab file
3. Parses the prefab YAML to extract components from the root GameObject
4. Merges prefab components with any scene-side modifications
5. Deduplicates components to avoid duplicate accessors

This allows seamless access to PrefabInstance GameObjects and their components, even when all components come purely from the prefab with no scene modifications:

```csharp
// Access PrefabInstance GameObject (same as regular GameObject)
GameObject background = AssetAccessor.Scene.Menu.GameObject.Canvas.Background.Get()

// Access components from prefab (Image, Button, etc.)
Image backgroundImage = AssetAccessor.Scene.Menu.GameObject.Canvas.Background.Component.Image.Get()
Button backgroundButton = AssetAccessor.Scene.Menu.GameObject.Canvas.Background.Component.Button.Get()
```

**Special handling:**

- **Duplicate names**: Appends numeric suffixes (e.g., `Canvas`, `Canvas1`, `Canvas2`)
- **Component name conflicts**: Appends `Component` suffix if component name matches the scene name
- **Reserved words**: Appends underscore suffix to avoid conflicts (e.g., `GameObject` → `GameObject_`)
- **File organization**: Generates separate files per scene in `Assets/Generated/AssetAccessor/GameObject/<SceneName>.cs`

The parser uses regex patterns to extract YAML blocks:

```csharp
// Match GameObject blocks: --- !u!1 &<id>
var gameObjectPattern = @"--- !u!1 &(\d+)\s+GameObject:.*?m_Name:\s*(.+?)$"

// Match PrefabInstance blocks with GUID and parent transform
var prefabInstancePattern = @"--- !u!1001 &(\d+)\s+PrefabInstance:.*?m_TransformParent: \{fileID: (\d+)\}.*?propertyPath: m_Name\s+value: (.+?)\s+.*?guid: ([0-9a-f]+),"

// Match stripped GameObjects from PrefabInstances
var strippedGameObjectPattern = @"--- !u!1 &(\d+) stripped\s+GameObject:.*?m_PrefabInstance: \{fileID: (\d+)\}"

// Match Transform/RectTransform blocks: --- !u!224 or --- !u!4 &<id>
var transformBlockPattern = @"--- !u!(?:224|4) &(\d+)\s+(?:RectTransform|Transform):.*?(?=^--- |\z)"

// Match MonoBehaviour components (including stripped) with editor class identifier
var monoBehaviourPattern = @"--- !u!114 &\d+(?:\s+stripped)?\s+MonoBehaviour:.*?m_GameObject:\s*\{fileID:\s*(\d+)\}.*?m_EditorClassIdentifier:\s*(.+?)$"
```

The multiline regex with lookahead ensures all blocks are captured, including those at the end of the file. PrefabInstance-specific patterns enable seamless integration of prefab content into the generated accessor hierarchy.

## [`AudioCacheController`](../Assets/App/Library/AudioCacheController.cs)

`AudioCacheController` manages audio assets, designed to optimize audio asset loading based on the loaded scene.

## [`GameObjectCacheController`](../Assets/App/Library/GameObjectCacheController.cs)

`GameObjectCacheController` manages GameObject references in the current scene. When a scene is loaded via `AssetAccessor.Scene.<SceneName>.Load()`, the controller:

1. Clears the cache if the scene has changed
2. Recursively caches all GameObjects in the scene hierarchy
3. Provides fast lookup through a `Dictionary<string, GameObject>` mapping

**Key features:**

- **Scene-aware caching**: Automatically refreshes when scene changes
- **Null safety**: Validates cached references before returning
- **Fallback mechanism**: Uses `GameObject.Find()` when cache misses occur
- **Recursive caching**: Captures all GameObjects including children

The cache is populated by traversing `SceneManager.GetActiveScene().GetRootGameObjects()` and recursively caching all children, ensuring efficient GameObject access without repeated scene queries.
