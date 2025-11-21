using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameObjectCacheController
{
  // シーン → GameObject名 → GameObject参照のキャッシュ
  private readonly Dictionary<string, GameObject> _gameObjectCache = new();

  // 現在のシーンID
  private AssetAccessor.Scene.Id? _currentSceneId = null;

  // AssetAccessor.GameObject.NotifySceneLoaded から呼ばれる
  public void OnSceneLoaded(AssetAccessor.Scene.Id sceneId)
  {
    // シーンが変わった場合、キャッシュをクリア
    if (_currentSceneId.HasValue && _currentSceneId.Value != sceneId)
    {
      _gameObjectCache.Clear();
    }

    _currentSceneId = sceneId;

    // 新しいシーンのGameObjectをキャッシュに読み込む
    RefreshGameObjectCache();
  }

  private void RefreshGameObjectCache()
  {
    _gameObjectCache.Clear();

    var activeScene = SceneManager.GetActiveScene();
    if (!activeScene.isLoaded)
    {
      return;
    }

    // ルートのGameObjectを取得
    var rootGameObjects = activeScene.GetRootGameObjects();
    foreach (var rootGo in rootGameObjects)
    {
      // 再帰的に全てのGameObjectをキャッシュ
      CacheGameObjectRecursive(rootGo);
    }
  }

  private void CacheGameObjectRecursive(GameObject go)
  {
    if (go == null) return;

    // GameObjectをキャッシュに追加
    _gameObjectCache[go.name] = go;

    // 子供のGameObjectも再帰的にキャッシュ
    var transform = go.transform;
    for (int i = 0; i < transform.childCount; i++)
    {
      var child = transform.GetChild(i);
      CacheGameObjectRecursive(child.gameObject);
    }
  }

  public GameObject GetGameObject(string name)
  {
    if (_gameObjectCache.TryGetValue(name, out var go))
    {
      // キャッシュにある場合、まだ有効か確認
      if (go != null)
      {
        return go;
      }
      // nullになっている場合はキャッシュから削除
      _ = _gameObjectCache.Remove(name);
    }

    // キャッシュミス → GameObject.Find でフォールバック
    go = GameObject.Find(name);
    if (go != null)
    {
      _gameObjectCache[name] = go;
    }

    return go;
  }

  public T GetComponent<T>(string gameObjectName) where T : Component
  {
    var go = GetGameObject(gameObjectName);
    return go == null ? null : go.GetComponent<T>();
  }
}
