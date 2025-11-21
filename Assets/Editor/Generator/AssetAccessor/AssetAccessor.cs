#if UNITY_EDITOR
using System.IO;

public static partial class AssetAccessorGenerator
{
  public static void Generate(string dir)
  {

    SceneAccessor.Generate(
      Path.Combine(dir, "Scene.cs") /* output file path */
    );
    AudioAccessor.Generate(
      "Assets/Resources/Audio/SE" /* SE asset directory */,
      Path.Combine(dir, "Audio.cs") /* output file path */
    );
    GameObjectAccessor.Generate(
      Path.Combine(dir, "GameObject/") /* output directory */
    );
  }
}
#endif
