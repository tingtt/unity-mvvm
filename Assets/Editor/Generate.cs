#if UNITY_EDITOR
using UnityEditor;

public static class Generate
{
  [MenuItem("Tools/Generate")]
  public static void Run()
  {
    AssetAccessorGenerator.Generate("Assets/Generated/AssetAccessor/");
  }
}
#endif
