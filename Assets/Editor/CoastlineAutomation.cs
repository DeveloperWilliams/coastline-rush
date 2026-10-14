using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// A small local editor interface for repeatable validation and builds.
// Only these fixed project operations are accepted; no arbitrary code execution.
[InitializeOnLoad]
public static class CoastlineAutomation
{
    static double next;
    static CoastlineAutomation()
    {
        EditorApplication.update += Poll;
    }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        next = EditorApplication.timeSinceStartup + .5;
        const string request = "Temp/coastline-command.txt";
        if (!File.Exists(request))
            return;
        string command = File.ReadAllText(request).Trim();
        File.Delete(request);
        try
        {
            switch (command)
            {
                case "stop":
                    EditorApplication.isPlaying = false;
                    break;
                case "play":
                    if (!EditorApplication.isPlaying)
                    {
                        EditorSceneManager.OpenScene("Assets/Coastline.unity");
                        EditorApplication.isPlaying = true;
                    }
                    break;
                case "build":
                    if (EditorApplication.isPlaying)
                        throw new InvalidOperationException("Stop Play mode before building.");
                    BuildRace.Build();
                    break;
                case "capture":
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException("Capture needs Play mode.");
                    Directory.CreateDirectory("Logs");
                    ScreenCapture.CaptureScreenshot("Logs/garage.png");
                    break;
                case "assets":
                    InspectAssets();
                    break;
                case "validate":
                    CoastlineChecks.RunPhysicsChecks();
                    break;
                case "validate-wet":
                    CoastlineChecks.RunWetChecks();
                    break;
                case "prepare":
                    BuildRace.PrepareScene();
                    break;
                case "rules":
                    CoastlineChecks.RunLogicChecks();
                    break;
                case "refresh":
                    AssetDatabase.Refresh();
                    break;
                default:
                    throw new InvalidOperationException("Unknown project operation: " + command);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/coastline-command-result.txt", DateTime.UtcNow.ToString("o") + " " + command + " OK");
        }
        catch (Exception e) { File.WriteAllText("Logs/coastline-command-result.txt", command + " ERROR " + e); Debug.LogException(e); }
    }
    static void InspectAssets()
    {
        var report = new System.Text.StringBuilder();
        foreach (string folder in new[] { "KenneyCars", "KenneyRacing", "KenneyNature" })
            foreach (var model in Resources.LoadAll<GameObject>(folder))
            {
                report.AppendLine(folder + "/" + model.name);
                var renderers = model.GetComponentsInChildren<Renderer>();
                Bounds b = new Bounds();
                bool first = true;
                foreach (var r in renderers)
                {
                    if (first)
                    {
                        b = r.bounds;
                        first = false;
                    }
                    else
                        b.Encapsulate(r.bounds);
                    report.AppendLine("  " + r.name + " bounds=" + r.bounds + " materials=" + string.Join(",", Array.ConvertAll(r.sharedMaterials, m => m ? m.name : "missing")));
                }
                report.AppendLine("  TOTAL " + b);
            }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/assets-inspection.txt", report.ToString());
    }
}
