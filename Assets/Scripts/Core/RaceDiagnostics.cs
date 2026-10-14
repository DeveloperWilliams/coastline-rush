#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Coastline
{
    /// <summary>Exercises the real PhysX car, controls and race director in Play mode.</summary>
    public sealed class RaceDiagnostics : MonoBehaviour
    {
        [Serializable]
        sealed class Report
        {
            public bool passed;
            public string circuit, weather, error, completedUtc;
            public float accelerationKph, brakingKph, steeringDegrees, boostRemaining, raceSeconds;
            public int playerRecoveries, completedLaps;
            public List<string> checks = new List<string>();
        }

        readonly Report report = new Report();
        RaceDirector race;
        AIDriver autopilot;
        CoastlineRace bootstrap;

        public static void Begin(CoastlineRace owner)
        {
            if (!SessionState.GetBool("Coastline.Validate", false)) return;
            SessionState.SetBool("Coastline.Validate", false);
            var check = owner.gameObject.AddComponent<RaceDiagnostics>();
            check.bootstrap = owner;
            check.StartCoroutine(check.Guarded());
        }

        IEnumerator Guarded()
        {
            var routine = Execute();
            while (true)
            {
                bool next = false;
                Exception failure = null;
                try { next = routine.MoveNext(); }
                catch (Exception exception) { failure = exception; }
                if (failure != null)
                {
                    report.error = failure.ToString();
                    Finish(false);
                    yield break;
                }
                if (!next) break;
                yield return routine.Current;
            }
            Finish(true);
        }

        void Check(bool passed, string name)
        {
            if (!passed) throw new InvalidOperationException(name);
            report.checks.Add(name);
            Debug.Log("PHYSICS_CHECK_PASS " + name);
        }

        IEnumerator Execute()
        {
            yield return new WaitForSecondsRealtime(.8f);
            race = bootstrap.Director;
            report.circuit = race.Circuit.Track.Name;
            report.weather = race.Settings.Weather.ToString();
            race.DiagnosticMode = true;
            Check(Resources.Load<Font>("Fonts/JosefinSans-regular") && Resources.Load<Font>("Fonts/JosefinSans-bold"),
                "Josefin Sans fonts load in the game");
            foreach (string sound in new[] { "engine-idle", "engine-load", "tyre-scrub", "road-wind", "nitro-air", "impact", "countdown", "go", "lap", "finish", "click" })
                if (!Resources.Load<AudioClip>("Audio/" + sound))
                    throw new InvalidOperationException("Missing audio: " + sound);
            Check(true, "All eleven original sound assets load");
            foreach (var entrant in race.Entrants)
                if (entrant.Driver) entrant.Driver.enabled = false;

            var car = race.Player.Vehicle;
            car.ResetAt(35, 0);
            car.ControlEnabled = false;
            Time.timeScale = 3;
            yield return new WaitForSeconds(1.5f);
            Check(car.GroundedWheels == 4, "Four suspension contacts on tarmac");
            Check(Vector3.Dot(car.transform.up, Vector3.up) > .95f, "Chassis settles upright");

            car.ControlEnabled = true;
            car.Input = new DriveInput { Throttle = 1 };
            yield return new WaitForSeconds(4);
            report.accelerationKph = car.SpeedKph;
            Check(car.SpeedKph > 50, "Torque accelerates past 50 km/h in four seconds");
            car.Input = new DriveInput { Brake = 1 };
            yield return new WaitForSeconds(3);
            report.brakingKph = car.SpeedKph;
            Check(car.SpeedKph < 8, "Brakes reduce speed below 8 km/h");

            car.ResetAt(35, 0);
            Quaternion before = car.transform.rotation;
            car.Input = new DriveInput { Throttle = 1, Steering = .4f };
            yield return new WaitForSeconds(2.5f);
            report.steeringDegrees = Quaternion.Angle(before, car.transform.rotation);
            Check(report.steeringDegrees > 8, "Wheel steering turns the chassis");

            car.ResetAt(35, 0);
            car.Input = new DriveInput { Throttle = 1, Boost = true };
            yield return new WaitForSeconds(2);
            report.boostRemaining = car.Nitro;
            Check(car.Nitro < .8f && car.SpeedKph > 20, "Grounded nitro applies force and drains charge");

            car.ResetAt(35, 0);
            int contacts = car.CollisionCount;
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Validation barrier";
            obstacle.transform.position = race.Circuit.Track.Position(49) + Vector3.up;
            obstacle.transform.rotation = race.Circuit.Track.Rotation(49);
            obstacle.transform.localScale = new Vector3(15, 2, .8f);
            car.Input = new DriveInput { Throttle = 1 };
            yield return new WaitForSeconds(4);
            Check(car.CollisionCount > contacts, "Solid barrier generates a vehicle collision");
            Check(car.Location.Distance < 55, "Vehicle cannot drive through a solid barrier");
            Destroy(obstacle);
            yield return null;

            race.StartRace();
            Time.timeScale = 3;
            yield return new WaitForSeconds(4.3f);
            race.Pause();
            Vector3 paused = car.Body.position;
            float timer = race.RaceTime;
            yield return new WaitForSecondsRealtime(.3f);
            Check(Vector3.Distance(paused, car.Body.position) < .001f && Mathf.Abs(timer - race.RaceTime) < .001f,
                "Pause freezes timer and PhysX together");
            race.Resume();
            race.StartRace();
            Check(car.Nitro > .999f && race.RaceTime == 0 && race.Player.Progress.CompletedLaps == 0,
                "Restart resets charge, timer and lap state");

            foreach (var entrant in race.Entrants)
                if (entrant.Driver) entrant.Driver.enabled = true;
            autopilot = car.gameObject.AddComponent<AIDriver>();
            autopilot.Director = race;
            autopilot.Entrant = race.Player;
            autopilot.Pace = .9f;
            autopilot.Lane = race.Player.Lane;
            race.Player.Driver = autopilot;
            Time.timeScale = 3;
            float started = Time.realtimeSinceStartup;
            while (race.State != RaceState.Results && Time.realtimeSinceStartup - started < 125)
            {
                yield return new WaitForSecondsRealtime(.5f);
                if (!car || float.IsNaN(car.Body.position.x)) throw new InvalidOperationException("Physics became invalid");
            }
            report.completedLaps = race.Player.Progress.CompletedLaps;
            report.playerRecoveries = race.Player.Recoveries;
            report.raceSeconds = race.RaceTime;
            Check(race.State == RaceState.Results && report.completedLaps == 3,
                "Physical AI drives all three laps and reaches results");
            Check(report.playerRecoveries == 0, "Full-race driver completes without recovery teleports");
            Time.timeScale = 1;
            ScreenCapture.CaptureScreenshot(Path.Combine(Directory.GetCurrentDirectory(), "Logs/advanced-race.png"));
            yield return new WaitForSecondsRealtime(.5f);
        }

        void Finish(bool passed)
        {
            report.passed = passed;
            report.completedUtc = DateTime.UtcNow.ToString("o");
            Time.timeScale = 1;
            AudioListener.pause = false;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/physics-validation.json", JsonUtility.ToJson(report, true));
            Debug.Log(passed ? "COASTLINE_PHYSICS_PASS" : "COASTLINE_PHYSICS_FAIL " + report.error);
            if (autopilot)
            {
                autopilot.enabled = false;
                if (race) race.Player.Driver = null;
                Destroy(autopilot);
            }
            if (race)
            {
                race.Garage();
                foreach (var entrant in race.Entrants)
                    if (entrant.Driver) entrant.Driver.enabled = true;
            }
            string saved = SessionState.GetString("Coastline.SettingsBeforeValidation", "");
            if (!string.IsNullOrEmpty(saved))
            {
                JsonUtility.FromJsonOverwrite(saved, bootstrap.Settings);
                bootstrap.SaveSettings();
            }
            SessionState.SetInt("Coastline.ValidationPreset", 0);
            bootstrap.BuildWorld();
            Destroy(this);
        }
    }
}
#endif
