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
- (WIP) Type safe GameObject refers

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

Generating accessor ([Assets/Generated/AssetAccessor](../Assets/Generated/AssetAccessor/)) for scenes and audio assets.

Scenes can be loaded with:

```cs
AssetsAccessor.Scene.SceneA.Load()
```

Audio asset can be loaded with:

```cs
AudioClip seA = AssetsAccessor.Audio.SE.SeA.Load()
```

#### WIP: GameObjectAccessor Generator

## [`AudioCacheController`](../Assets/App/Library/AudioCacheController.cs)

`AudioCacheController` managing audio assets, designed to optimize audio asset loading based on the loaded scene.
