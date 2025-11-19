# DesignDoc: unity-mvvm

## Feature slices

### Editor, CI (`Makefile`, `Assets/Editor/`)

- [Sync meta (`make sync` command)](./DesignDoc_sync_meta.md)
- [Format and Lint (`make fmt`, `make lint` command)](./DesignDoc_format_lint.md)
- [Code generation (`make gen` command)](./DesignDoc_code_generation.md)

### In Build (`Assets/App`, `Assets/Resources`)

#### Structure

TODO: ViewModel スクリプトと UI 用スクリプトの分け方、配置をドキュメント化

- `Assets/App/`
  - `_Library/`: Internal use scripts
    - `<Library>.cs`
  - `_Prefabs/`: Internal (App) use prefabs
    - `<Prefab>.prefab`
    - `<PrefabScript>.cs`: Internal (Prefab) use script
    - `<PrefabImage>.png`: Internal (Prefab) use and **static** attached image
  - `<SceneName>/`
    - `<SceneName>.unity`: Scene
    - `<SceneScript>.cs`: Internal (Scene) use script
    - `_Prefabs/`: Internal (Scene) use prefabs
  - `(<GroupName>)/`
    - `<SceneName>/`
      - `<SceneName>.unity`: Scene
      - `<SceneScript>.cs`: Internal (Scene) use script
      - `_Prefabs/`: Internal (Scene) use prefabs
    - `<GroupScript>.cs`: Internal (SceneGroup) use script
- `Assets/Resources/`: **Dynamicly** access resources
  - `Audio/`: Audio Assets
  - `Image/`: Images
  - `Data/`: Other data
- `Assets/Generated/`: Generated code

The folder structure is inspired by [Next.js App Router Project Structure](https://nextjs.org/docs/app/getting-started/project-structure),  
designed to achieve **clear separation of responsibilities** between UI presentation, logic, and data state.
