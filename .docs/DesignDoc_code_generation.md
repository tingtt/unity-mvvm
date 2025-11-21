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
- **Custom user scripts are excluded** to avoid type conflicts and maintain clean generated code

Components are detected by parsing:

- `MonoBehaviour` blocks with `m_EditorClassIdentifier` (for UI and third-party components)
- Standard Unity component blocks by their class IDs (!u!223 for Canvas, !u!224 for RectTransform, etc.)

Each detected component gets its own nested class under `GameObject.<Name>.Component.<ComponentName>` with a type-safe `Get()` method that returns the specific component type.

**Special handling:**

- **Duplicate names**: Appends numeric suffixes (e.g., `Canvas`, `Canvas1`, `Canvas2`)
- **Component name conflicts**: Appends `Component` suffix if component name matches the scene name
- **Reserved words**: Appends underscore suffix to avoid conflicts (e.g., `GameObject` → `GameObject_`)
- **File organization**: Generates separate files per scene in `Assets/Generated/AssetAccessor/GameObject/<SceneName>.cs`

The parser uses regex patterns to extract YAML blocks:

```csharp
// Match GameObject blocks: --- !u!1 &<id>
var gameObjectPattern = @"--- !u!1 &(\d+)\s+GameObject:.*?m_Name:\s*(.+?)$"

// Match Transform/RectTransform blocks: --- !u!224 or --- !u!4 &<id>
var transformBlockPattern = @"--- !u!(?:224|4) &(\d+)\s+(?:RectTransform|Transform):.*?(?=^--- |\z)"

// Match MonoBehaviour components with editor class identifier
var monoBehaviourPattern = @"--- !u!114 &\d+\s+MonoBehaviour:.*?m_GameObject:\s*\{fileID:\s*(\d+)\}.*?m_EditorClassIdentifier:\s*(.+?)$"
```

The multiline regex with lookahead ensures all blocks are captured, including those at the end of the file.

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
