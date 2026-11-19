#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Builds an APK for testing on an Android phone. Menu: Umwami > Build Android APK.</summary>
public static class UmwamiAndroidBuild
{
    const string Output = "Builds/Android/UmwamiAscendAR.apk";

    [MenuItem("Umwami/Build Android APK")]
    public static void Build() => Build(false);

    /// <summary>Builds, installs and starts the app on the phone connected by USB (USB debugging must be on).</summary>
    [MenuItem("Umwami/Build and Run on Phone")]
    public static void BuildAndRun() => Build(true);

    static void Build(bool run)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Umwami", "Stop Play mode first, then build again.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Make sure the Title and Story scenes exist and lead the build list.
        UmwamiSceneBuilder.Build();

        PlayerSettings.companyName = "Strix Legacy";
        PlayerSettings.productName = "Umwami's Ascend";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.strixlegacy.umwamiascendar");
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        EditorUserBuildSettings.buildAppBundle = false;
        AssetDatabase.SaveAssets();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("Umwami: could not switch to Android. Check that the Android module is installed for this Unity version in Unity Hub.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Title.unity", "Assets/Scenes/Story.unity" },
            locationPathName = Output,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = run ? BuildOptions.AutoRunPlayer : BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        var s = report.summary;
        if (s.result == BuildResult.Succeeded)
        {
            Debug.Log($"Umwami: APK built at {Path.GetFullPath(Output)} ({s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalMinutes:0.0} min).");
            if (!run) EditorUtility.RevealInFinder(Output);
        }
        else
        {
            Debug.LogError($"Umwami: Android build {s.result} with {s.totalErrors} error(s). See the Console above for details.");
        }
    }
}
#endif
