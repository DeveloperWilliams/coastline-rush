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
            var plan = new RacingPlan(track);
            var wetSpeeds = plan.BuildSpeedProfile(51, 7.2f, 8);
            bool outside = false, inside = false;
            for (int i = 0; i < track.Count; i++)
            {
                float d = track.Length * i / track.Count;
                float offset = plan.Lane(d);
                Assert(Mathf.Abs(offset) <= track.HalfWidth - 1.7f, "Guide leaves safe road bounds");
                Assert(!float.IsNaN(plan.PreviewSpeed[i]) && plan.PreviewSpeed[i] > 5, "Invalid braking profile");
                Assert(wetSpeeds[i] <= plan.PreviewSpeed[i] + .01f, "Wet guidance recommends a faster corner speed");
                Assert(plan.PreviewSpeed[i] * plan.PreviewSpeed[i] <= plan.PreviewSpeed[(i + 1) % track.Count] * plan.PreviewSpeed[(i + 1) % track.Count] + 16 * track.Length / track.Count + .01f, "Braking envelope discontinuity");
                outside |= offset < -.5f;
                inside |= offset > .5f;
            }
            Assert(outside && inside, "Guide does not use both sides of the road");
            Assert(Vector3.Distance(plan.Position(0), plan.Position(track.Length)) < .001f, "Guide seam is not closed");
            passed += 6;
            var recovered = new LapProgress();
            recovered.Reset(-3, track.Length);
            recovered.Tick(0, 0, track.Length, track.HalfWidth, 1, .02f, 2);
            recovered.Recover(0, track.Length);
            DriveLap(recovered, track, 0, 1, 2);
            Assert(recovered.CompletedLaps == 1 && !recovered.LastLapEligible && recovered.BestLap == 0, "Recovered lap awarded a record");
            DriveLap(recovered, track, track.Length, 70, 2);
            Assert(recovered.CompletedLaps == 2 && recovered.LastLapEligible && recovered.BestLap > 0, "Clean lap after recovery is ineligible");
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
        var gearbox = new AutomaticGearbox();
        gearbox.Tick(400, 1, 15, .02f);
        Assert(gearbox.Gear == 2 && gearbox.IsShifting && gearbox.TorqueFactor < .5f, "Upshift or torque interruption failed");
        for (int i = 0; i < 60; i++)
            gearbox.Tick(400, 1, 15, .02f);
        Assert(gearbox.Gear == 2 && !gearbox.IsShifting, "Gear oscillates or shift never settles");
        for (int i = 0; i < 100; i++)
            gearbox.Tick(0, 0, 3, .02f);
        Assert(gearbox.Gear == 1 && gearbox.EngineRpm >= 1100 && gearbox.EngineRpm < 1150, "Downshift or idle RPM failed");
        gearbox.Reset();
        Assert(gearbox.Gear == 1 && !gearbox.IsShifting, "Gearbox restart failed");
        gearbox.Tick(400, 1, 15, .02f);
        for (int i = 0; i < 60; i++)
            gearbox.Tick(0, 0, 0, .02f);
        Assert(gearbox.Gear == 1, "Stopped vehicle remains in a higher gear");
        passed += 5;
        var traffic = new[] { new TrafficSample { Distance = 8, Lateral = 3, Speed = 20 } };
        Assert(!TrafficSafety.IsLaneClear(3, 25, traffic) && TrafficSafety.IsLaneClear(-3, 25, traffic), "Occupied passing lane accepted");
        traffic[0] = new TrafficSample { Distance = -20, Lateral = -3, Speed = 40 };
        Assert(!TrafficSafety.IsLaneClear(-3, 20, traffic), "Faster car approaching from rear ignored");
        traffic[0] = new TrafficSample { Distance = -65, Lateral = -3, Speed = 40 };
        Assert(TrafficSafety.IsLaneClear(-3, 20, traffic), "Distant rear car blocks a safe pass");
        passed += 3;
        var a = new RaceEntrant { Name = "A", FinishTime = 60, Penalty = 5 };
        var b = new RaceEntrant { Name = "B", FinishTime = 62 };
        var comparisonTrack = new TrackPath();
        a.Progress.Reset(-3, comparisonTrack.Length);
        b.Progress.Reset(-3, comparisonTrack.Length);
        a.Progress.Tick(0, 0, comparisonTrack.Length, comparisonTrack.HalfWidth, 1, .02f, 1);
        b.Progress.Tick(0, 0, comparisonTrack.Length, comparisonTrack.HalfWidth, 1, .02f, 1);
        DriveLap(a.Progress, comparisonTrack, 0, 1, 1);
        DriveLap(b.Progress, comparisonTrack, 0, 1, 1);
        Assert(RaceClassification.Compare(a, b) > 0 && a.AdjustedTime == 65 && b.AdjustedTime == 62, "Penalties do not adjust final order");
        a.Penalty = 0;
        Assert(RaceClassification.Compare(a, b) < 0, "Raw finish order failed");
        b.Progress.Reset(-3, comparisonTrack.Length);
        Assert(RaceClassification.Compare(a, b) < 0, "Unfinished car ranks above finisher");
        passed += 3;
        var template = VehicleTuning.Defaults();
        var club = VehicleTuning.ForModel(template, 1);
        var nova = VehicleTuning.ForModel(template, 2);
        Assert(club.tyreGrip > template.tyreGrip && club.topSpeed < template.topSpeed && nova.topSpeed > template.topSpeed, "Car profiles are not distinct");
        Assert(template.mass == 1200 && template.torquePerWheel == 730, "Variant modified shared baseline");
        UnityEngine.Object.DestroyImmediate(club);
        UnityEngine.Object.DestroyImmediate(nova);
        UnityEngine.Object.DestroyImmediate(template);
        passed += 2;
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/rules-validation.txt", "PASS " + passed + " track and race checks\n");
        Debug.Log("COASTLINE_RULES_PASS checks=" + passed);
    }
    static void DriveLap(LapProgress progress, TrackPath track, float from, float time, int laps)
    {
        for (float metres = from + 1; metres <= from + track.Length + 2; metres += 1)
        {
            time += .02f;
            progress.Tick(Mathf.Repeat(metres, track.Length), 0, track.Length, track.HalfWidth, time, .02f, laps);
        }
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
    [MenuItem("Coastline Rush/Validation/Run Nova R Solo Checks")]
    public static void RunNovaSoloChecks()
    {
        SessionState.SetInt("Coastline.ValidationPreset", 2);
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
