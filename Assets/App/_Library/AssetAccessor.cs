using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class AssetAccessor
{
  public static partial class Scene
  {
    private static void Load(Id id)
    {
      SceneManager.LoadScene(Path(id));
      Audio.NotifySceneLoaded(id);
    }
  }

  public static partial class Audio
  {
    public static AudioCacheController CacheController { get; private set; }

    public static void InitializeLoader(AudioCacheController controller)
    {
      CacheController = controller;
    }

    internal static void NotifySceneLoaded(Scene.Id sceneId)
    {
      CacheController?.LoadSceneAssets(sceneId);
    }

    public static partial class SE
    {
      public static AudioClip Load(Id id)
      {
        var clip = CacheController?.GetSE(id);
        if (clip != null) return clip;

        var path = Path(id);
        clip = Resources.Load<AudioClip>(path);
        if (clip == null)
        {
          throw new System.Exception($"[Assets.Audio.SE] AudioClip not found at {path}");
        }

        if (CacheController == null)
        {
          throw new System.Exception("[Assets.Audio.SE] CacheController is not initialized.");
        }

        CacheController?.CacheSE(id, clip);
        return clip;
      }
    }
  }
}
