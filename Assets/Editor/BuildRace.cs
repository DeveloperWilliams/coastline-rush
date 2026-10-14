using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using Coastline;

public static class BuildRace
{
    [MenuItem("Coastline Rush/Open Racing Scene")]
    public static void OpenRace() => EditorSceneManager.OpenScene("Assets/Coastline.unity");

    [MenuItem("Coastline Rush/Play Race")]
    public static void PlayRace()
    {
        if (EditorApplication.isPlaying)
            return;
        PrepareScene();
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Coastline Rush/Prepare Scene and Handling Asset")]
    public static void PrepareScene()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode first.");
        var tuning = AssetDatabase.LoadAssetAtPath<VehicleTuning>("Assets/Resources/ClubHandling.asset");
        if (!tuning)
        {
            tuning = VehicleTuning.Defaults();
            AssetDatabase.CreateAsset(tuning, "Assets/Resources/ClubHandling.asset");
        }
        // The runtime creates these materials, so retain the shader explicitly.
        if (!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/SurfaceReference.mat"))
        {
            var reference = new Material(Shader.Find("Standard")) { name = "Surface reference" };
            AssetDatabase.CreateAsset(reference, "Assets/Resources/SurfaceReference.mat");
        }
        OpenRace();
        var bootstrap = UnityEngine.Object.FindFirstObjectByType<CoastlineRace>();
        if (!bootstrap)
            bootstrap = new GameObject("Coastline Rush").AddComponent<CoastlineRace>();
        bootstrap.vehicleTuning = tuning;
        EditorUtility.SetDirty(bootstrap);
        EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
        EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
        PlayerSettings.companyName = "Open Road";
        PlayerSettings.productName = "Coastline Rush";
        PlayerSettings.bundleVersion = "0.3.0";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        EditorSettings.serializationMode = SerializationMode.ForceText;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Coastline.unity", true) };
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Coastline Rush/Build macOS Game")]
    public static void Build()
    {
        PrepareScene();
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Coastline.unity" },
            locationPathName = "Build/Coastline Rush.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Build failed: " + report.summary.result);
        System.IO.Directory.CreateDirectory("Logs");
        System.IO.File.WriteAllText("Logs/build-result.txt", "PASS macOS build / " + report.summary.totalSize + " bytes / " + report.summary.totalTime);
        Debug.Log("COASTLINE_BUILD_PASS " + report.summary.totalSize + " bytes");
    }
}
