using System.Collections.Generic;
using UnityEngine;

public class AudioCacheController
{
  // シーン → そのシーンで使う SE のセット
  private readonly Dictionary<AssetAccessor.Scene.Id, HashSet<AssetAccessor.Audio.SE.Id>> _sceneSEs;

  // SE ID → 実際の AudioClip キャッシュ
  private readonly Dictionary<AssetAccessor.Audio.SE.Id, AudioClip> _seCache = new();

  // 今のシーン（必要なら）
  private AssetAccessor.Scene.Id _currentScene;

  public AudioCacheController(Dictionary<AssetAccessor.Scene.Id, HashSet<AssetAccessor.Audio.SE.Id>> sceneMap, AssetAccessor.Scene.Id initialScene)
  {
    _sceneSEs = sceneMap;
    LoadSceneAssets(initialScene);
  }

  public AudioCacheController()
  {
  }

  // Assets.Audio.NotifySceneLoaded から呼ばれる
  public void LoadSceneAssets(AssetAccessor.Scene.Id sceneId)
  {
    if (/* scene not changed */ _currentScene == sceneId) return;

    _currentScene = sceneId;

    // 例: そのシーンで必要な SE だけプリロード
    if (_sceneSEs.TryGetValue(sceneId, out var SEs))
    {
      foreach (var seId in SEs)
      {
        if (!_seCache.ContainsKey(seId))
        {
          var clip = AssetAccessor.Audio.SE.Load(seId);
          CacheSE(seId, clip);
        }
      }
    }

    // 逆に「今使わない SE はアンロード」などもここでできる
  }

  public void CacheSE(AssetAccessor.Audio.SE.Id id, AudioClip clip)
  {
    _seCache[id] = clip;
  }

  public AudioClip GetSE(AssetAccessor.Audio.SE.Id id)
  {
    return _seCache.TryGetValue(id, out var clip) ? clip : null;
  }
}

