using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Coastline;
public static class CoastlineChecks
{
    public static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
    [MenuItem("Coastline Rush/Validation/Run Track and Race Rules Checks")]
    public static void RunLogicChecks()
    {
        int passed = 0;
        for (int layout = 0; layout < 2; layout++)
        {
            var track = new TrackPath(layout);
            Assert(track.Length > 800 && track.Length < 1600, "Unexpected circuit length");
            Assert(Vector3.Distance(track.Position(0), track.Position(track.Length)) < .001f, "Circuit seam is not closed");
            Assert(Vector3.Dot(track.Direction(0), track.Direction(track.Length)) > .999f, "Circuit seam tangent mismatch");
            passed += 3;
            for (int i = 0; i < track.Count; i += 15)
            {
                var point = track.Position(track.Distances[i]);
                var nearest = track.Locate(point);
                Assert(nearest.Offset < .01f, "Projection moved off the sampled road");
                Assert(track.Distances[i + 1] > track.Distances[i], "Non-monotonic arc length");
            }
            passed += 2;
            var progress = new LapProgress();
            progress.Reset(-3, track.Length);
            float time = 0;
            for (float metres = -2; metres < track.Length * 3 + 3; metres += 1)
            {
                time += .02f;
                progress.Tick(Mathf.Repeat(metres, track.Length), 0, track.Length, track.HalfWidth, time, .02f, 3);
                if (progress.Finished)
                    break;
            }
            Assert(progress.CompletedLaps == 3 && progress.Finished, "Ordered sectors failed to complete three circuits");
            passed++;
            var shortcut = new LapProgress();
            shortcut.Reset(-3, track.Length);
            shortcut.Tick(0, 0, track.Length, track.HalfWidth, 1, .02f, 3);
            shortcut.Tick(track.Length * .5f, 0, track.Length, track.HalfWidth, 2, .02f, 3);
            shortcut.Tick(0, 0, track.Length, track.HalfWidth, 3, .02f, 3);
            Assert(shortcut.CompletedLaps == 0, "Teleport awarded a lap");
            passed++;
            var backward = new LapProgress();
            backward.Reset(200, track.Length);
            for (int i = 0; i < 180; i++)
                backward.Tick(199 - i, 0, track.Length, track.HalfWidth, i * .02f, .02f, 3);
            Assert(backward.WrongWay && backward.CompletedLaps == 0, "Reverse driving not detected");
            passed++;
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/rules-validation.txt", "PASS " + passed + " track and race checks\n");
        Debug.Log("COASTLINE_RULES_PASS checks=" + passed);
    }
    [MenuItem("Coastline Rush/Validation/Run Physics and Full Race Checks")]
    public static void RunPhysicsChecks()
    {
        RunLogicChecks();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.playModeStateChanged += RestartAfterStop;
        }
        else
            StartValidation();
    }
    [MenuItem("Coastline Rush/Validation/Run Highland Wet Checks")]
    public static void RunWetChecks()
    {
        SessionState.SetInt("Coastline.ValidationPreset", 1);
        RunPhysicsChecks();
    }
    static void RestartAfterStop(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;
        EditorApplication.playModeStateChanged -= RestartAfterStop;
        EditorApplication.delayCall += StartValidation;
    }
    static void StartValidation()
    {
        SessionState.SetBool("Coastline.Validate", true);
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Coastline.unity");
        EditorApplication.isPlaying = true;
    }
}
