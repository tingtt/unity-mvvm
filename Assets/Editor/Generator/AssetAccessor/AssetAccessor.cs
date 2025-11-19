#if UNITY_EDITOR
public static partial class AssetAccessorGenerator
{
  public static void Generate(string dir)
  {

    SceneAccessor.Generate(
      $"{dir}/Scene.cs" /* output file path */
    );
    AudioAccessor.Generate(
      "Assets/Resources/Audio/SE" /* SE asset directory */,
      $"{dir}/Audio.cs" /* output file path */
    );
  }
}
#endif
