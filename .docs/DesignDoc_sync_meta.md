# DesignDoc: Sync meta files

## Abstract/Summary

This document describes the meta file synchronization system that ensures Unity's `.meta` files are properly generated and synchronized with asset files. The system provides a command-line interface to force Unity's Asset Database refresh, which is essential for maintaining consistency in version control and CI/CD pipelines.

## Background/Motivation

Unity automatically generates `.meta` files for every asset in the project to track asset GUIDs and import settings. However, several issues can arise:

- **Missing meta files**: When assets are created or moved outside Unity, corresponding `.meta` files may not be generated automatically
- **Version control conflicts**: Meta files can get out of sync during git operations (merge, rebase, checkout)
- **CI/CD pipeline issues**: Automated builds may fail if meta files are not properly synchronized
- **Manual Unity launch required**: Developers typically need to open Unity Editor to trigger meta file generation

This synchronization system addresses these issues by providing a headless way to force Unity's Asset Database refresh, enabling automated workflows and reducing the need for manual Unity Editor interactions.

## Goals

- Force Unity Asset Database refresh from command line
- Enable automated meta file generation in git hooks and CI/CD pipelines
- Provide both verbose and quiet execution modes
- Handle concurrent Unity instance conflicts gracefully

## Usage

```sh
# Standard sync with output
make sync

# Quiet sync (suppress Unity logs)
make sync-quiet
```

## Known errors

### Unity instance already running

**Error message:**

```text
Multiple Unity instances cannot open the same project
```

**Cause:**
Unity Editor is already running with the project open.

**Solution:**
The `make sync` command treats this as success since the running Unity instance will handle meta file synchronization automatically. No action is required.

## Proposed Design

### `sync` script (in [`Makefile`](../Makefile))

The `sync` script runs `Refresh.Run()` in [`Assets/Editor/Refresh.cs`](../Assets/Editor/Refresh.cs) via Unity CLI in headless mode.

**Features:**

- Executes `AssetDatabase.Refresh()` to force meta file generation
- Detects if Unity is already running and handles it gracefully
- Provides both verbose (`sync`) and quiet (`sync-quiet`) modes

**Environment variables:**

- `UNITY_VERSION`: Unity version to use
- `UNITY_PATH_MAC`: Path to Unity on macOS
- `UNITY_PATH_WIN`: Path to Unity on Windows
- `EXECUTE_METHOD_REFRESH`: Editor method to execute (default: `Refresh.Run`)

### [`Assets/Editor/Refresh.cs`](../Assets/Editor/Refresh.cs)

Simple Editor script that calls `AssetDatabase.Refresh()` to synchronize all assets and generate missing meta files.

**Features:**

- Can be invoked from Unity CLI with `-executeMethod Refresh.Run`
- Also accessible via Unity Editor menu: `Tools/Refresh Meta`
- Forces complete Asset Database refresh

## Use Cases

### Git hooks

Automatically sync meta files after git operations:

```sh
# .git/hooks/post-checkout
#!/bin/sh
make sync-quiet
```

### CI/CD pipelines

Ensure meta files are synchronized before building:

```sh
# .github/workflows/build.yml
- name: Sync Unity meta files
  run: make sync
```

### Local development

Quickly sync after manual file operations:

```sh
# After moving/copying assets outside Unity
make sync
```
