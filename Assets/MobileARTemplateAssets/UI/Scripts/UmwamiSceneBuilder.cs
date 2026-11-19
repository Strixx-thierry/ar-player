#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Builds the Title and Story scenes in Assets/Scenes. Runs once by itself when the scenes
/// are missing, and again from the menu: Umwami > Rebuild UI Scenes.
/// </summary>
public static class UmwamiSceneBuilder
{
    const string UI = "Assets/MobileARTemplateAssets/UI/";
    const string TitlePath = "Assets/Scenes/Title.unity";
    const string StoryPath = "Assets/Scenes/Story.unity";
    const string SamplePath = "Assets/Scenes/SampleScene.unity";

    [InitializeOnLoadMethod]
    static void BuildWhenMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(TitlePath) || !File.Exists(StoryPath)) Build();
        };
    }

    [MenuItem("Umwami/Rebuild UI Scenes")]
    public static void Build()
    {
        try
        {
            var titleUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UI + "Title.uxml");
            var storyUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UI + "Story.uxml");
            if (titleUxml == null || storyUxml == null)
            {
                Debug.LogWarning("Umwami: UI files are not imported yet. Use Umwami > Rebuild UI Scenes to try again.");
                return;
            }

            var panel = Panel();
            BuildTitle(panel, titleUxml);
            BuildStory(panel, storyUxml);

            // Title first, then Story, then whatever was already in the list.
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(TitlePath, true),
                new EditorBuildSettingsScene(StoryPath, true),
            };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != TitlePath && s.path != StoryPath) scenes.Add(s);
            EditorBuildSettings.scenes = scenes.ToArray();

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            AssetDatabase.SaveAssets();
            Debug.Log("Umwami: built Assets/Scenes/Title.unity and Assets/Scenes/Story.unity.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Umwami: could not build the UI scenes. " + e);
        }
    }

    static PanelSettings Panel()
    {
        const string path = UI + "UmwamiPanel.asset";
        var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
        if (panel == null)
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panel, path);
        }
        panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UI + "UmwamiTheme.tss");
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(844, 390);
        panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panel.match = 1f;
        EditorUtility.SetDirty(panel);
        return panel;
    }

    static void BuildTitle(PanelSettings panel, VisualTreeAsset uxml)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        var c = cam.GetComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color32(0x22, 0x25, 0x2D, 255);
        SceneManager.MoveGameObjectToScene(cam, scene);

        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(events, scene);

        var ui = new GameObject("Title UI", typeof(UIDocument), typeof(TitleScreen));
        var doc = ui.GetComponent<UIDocument>();
        doc.panelSettings = panel;
        doc.visualTreeAsset = uxml;
        SceneManager.MoveGameObjectToScene(ui, scene);

        EditorSceneManager.SaveScene(scene, TitlePath);
        EditorSceneManager.CloseScene(scene, true);
    }

    static void BuildStory(PanelSettings panel, VisualTreeAsset uxml)
    {
        // Start from the template scene, which already has the AR session and camera rig.
        if (!File.Exists(StoryPath))
        {
            if (!AssetDatabase.CopyAsset(SamplePath, StoryPath))
                throw new IOException("Could not copy " + SamplePath);
        }

        var scene = EditorSceneManager.OpenScene(StoryPath, OpenSceneMode.Additive);
        GameObject ours = null;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<StoryScreen>() != null) ours = root;

            // Switch off the template's own menu canvas.
            if (root.GetComponent<Canvas>() != null)
            {
                root.SetActive(false);
                if (!root.name.EndsWith("(template, off)")) root.name += " (template, off)";
            }

            // Stop the template from dropping sample shapes when the screen is tapped.
            foreach (var b in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (b == null) continue;
                string type = b.GetType().Name;
                if (type == "ARInteractorSpawnTrigger" || type == "ObjectSpawner") b.enabled = false;
            }
        }

        if (ours == null)
        {
            ours = new GameObject("Story UI", typeof(UIDocument), typeof(StoryScreen));
            SceneManager.MoveGameObjectToScene(ours, scene);
        }
        var doc = ours.GetComponent<UIDocument>();
        doc.panelSettings = panel;
        doc.visualTreeAsset = uxml;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);
    }
}
#endif
