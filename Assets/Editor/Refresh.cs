#if UNITY_EDITOR
using UnityEditor;

public static class Refresh
{
  [MenuItem("Tools/Refresh Meta")]
  public static void Run()
  {
    AssetDatabase.Refresh();
  }
}
#endif
