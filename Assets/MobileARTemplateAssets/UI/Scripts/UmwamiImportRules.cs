#if UNITY_EDITOR
using System.IO;
using UnityEditor;

/// <summary>Imports the story characters with the Legacy animation type, so the director can play clips by name.</summary>
public class UmwamiImportRules : AssetPostprocessor
{
    static readonly string[] Cast =
        { "Kigwa", "Nkuba", "Gasani", "BabyKigwa", "Nyampundu", "Companion", "MubariMan", "MubariWoman" };

    static bool IsCast(string path) =>
        path.StartsWith("Assets/Resources/") && path.EndsWith(".fbx") &&
        System.Array.IndexOf(Cast, Path.GetFileNameWithoutExtension(path)) >= 0;

    void OnPreprocessModel()
    {
        if (!IsCast(assetPath)) return;
        var m = (ModelImporter)assetImporter;
        m.animationType = ModelImporterAnimationType.Legacy;
        m.importAnimation = true;
    }

    // Covers characters that were imported before this script existed.
    [InitializeOnLoadMethod]
    static void FixExisting()
    {
        EditorApplication.delayCall += () =>
        {
            foreach (var name in Cast)
            {
                string path = "Assets/Resources/" + name + ".fbx";
                if (AssetImporter.GetAtPath(path) is ModelImporter m && m.animationType != ModelImporterAnimationType.Legacy)
                    m.SaveAndReimport();
            }
        };
    }
}
#endif
