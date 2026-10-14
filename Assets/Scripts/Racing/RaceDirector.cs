using System;
using System.Collections.Generic;
using UnityEngine;
namespace Coastline
{
    [Serializable]
    public sealed class RaceEntrant
    {
        public string Name; public Color Color; public VehicleController Vehicle; public AIDriver Driver; public LapProgress Progress = new LapProgress(); public float GridDistance, Lane, FinishTime; public int Recoveries;
    }
    public sealed class RaceDirector : MonoBehaviour
    {
        public RaceState State { get; private set; } = RaceState.Garage;
        public RaceSettings Settings
        {
            get; private set;
        }
        public CircuitBuilder Circuit
        {
            get; private set;
        }
        public List<RaceEntrant> Entrants { get; private set; } = new List<RaceEntrant>();
        public RaceEntrant Player
        {
            get; private set;
        }
        public float RaceTime
        {
            get; private set;
        }
        public float Countdown
        {
            get; private set;
        }
        public float BestSavedLap
        {
            get; private set;
        }
        public float Penalty
        {
            get; private set;
        }
        public string Notification
        {
            get; private set;
        }
        public bool DiagnosticMode;
        public Action ReturnToGarage;
        float toast, lastRecovery = -10;
        RaceAudio cues;
        public void Initialize(RaceSettings settings, CircuitBuilder circuit, VehicleTuning tuning)
        {
            Settings = settings;
            cues = gameObject.AddComponent<RaceAudio>();
            cues.Settings = settings;
            Circuit = circuit;
            var colors = new[] { new Color(1, .42f, .25f), new Color(.75f, .5f, 1), new Color(1, .82f, .23f), new Color(.3f, .66f, 1), new Color(.95f, .47f, .73f), new Color(.22f, .95f, .73f) };
            string[] names = { "EMBER", "VIOLET", "GOLD", "COBALT", "ROSE", "YOU" };
            int count = settings.Mode == RaceMode.TimeTrial ? 1 : 6;
            var vehicles = new GameObject("Vehicles / shared physics").transform;
            vehicles.SetParent(transform, false);
            for (int i = 0; i < count; i++)
            {
                bool player = i == count - 1;
                var vehicle = VehicleFactory.Create(player ? settings.Car : i % 3, colors[player ? 5 : i], player, circuit.Track, tuning, circuit.Materials, vehicles);
                vehicle.Weather = settings.Weather;
                vehicle.GetComponent<VehicleAudio>().Settings = settings;
                var entrant = new RaceEntrant { Name = names[player ? 5 : i], Color = colors[player ? 5 : i], Vehicle = vehicle, GridDistance = -8 - i * 7, Lane = count == 1 ? 0 : i % 2 == 0 ? -2.6f : 2.6f };
                vehicle.ResetAt(entrant.GridDistance, entrant.Lane);
                entrant.Progress.Reset(entrant.GridDistance, circuit.Track.Length);
                if (!player)
                {
                    entrant.Driver = vehicle.gameObject.AddComponent<AIDriver>();
                    entrant.Driver.Director = this;
                    entrant.Driver.Entrant = entrant;
                    entrant.Driver.Lane = entrant.Lane;
                    entrant.Driver.Pace = settings.Pace + (i - 2) * .012f;
                }
                else
                    Player = entrant;
                Entrants.Add(entrant);
            }
            string key = BestKey();
            BestSavedLap = PlayerPrefs.GetFloat(key, 0);
            AudioListener.volume = settings.Volume;
        }
        string BestKey() => "cr-v2-lap-" + Circuit.Track.Id + "-" + Settings.Car + "-" + Settings.Weather;
        public void StartRace()
        {
            Time.timeScale = 1;
            AudioListener.pause = false;
            RaceTime = Penalty = 0;
            lastRecovery = -10;
            Countdown = 3;
            toast = 0;
            State = RaceState.Countdown;
            cues.BeginCountdown();
            foreach (var e in Entrants)
            {
                e.FinishTime = 0;
                e.Recoveries = 0;
                e.Vehicle.ControlEnabled = false;
                e.Vehicle.ResetAt(e.GridDistance, e.Lane);
                e.Progress.Reset(e.GridDistance, Circuit.Track.Length);
                if (e.Driver)
                    e.Driver.Lane = e.Lane;
            }
            Circuit.RacingLine.SetActive(Settings.ShowRacingLine);
            Toast("FIND YOUR LINE. CHASE THE COASTLINE.");
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == RaceState.Racing)
                    Pause();
                else if (State == RaceState.Paused)
                    Resume();
            }
            if (Input.GetKeyDown(KeyCode.R) && (State == RaceState.Racing || State == RaceState.Results || State == RaceState.Paused))
                StartRace();
            if (Input.GetKeyDown(KeyCode.F) && State == RaceState.Racing)
                Recover(Player);
            if (Input.GetKeyDown(KeyCode.Return) && (State == RaceState.Garage || State == RaceState.Results))
                StartRace();
            if (toast > 0)
            {
                toast -= Time.unscaledDeltaTime;
                if (toast <= 0)
                    Notification = null;
            }
            if (State == RaceState.Countdown)
            {
                int tick = Mathf.CeilToInt(Countdown);
                Countdown -= Time.deltaTime;
                if (Mathf.CeilToInt(Countdown) != tick && Countdown > 0)
                    cues.Tick();
                if (Countdown <= 0)
                {
                    State = RaceState.Racing;
                    foreach (var e in Entrants)
                        e.Vehicle.ControlEnabled = true;
                    cues.Go();
                    Toast("GO!");
                }
                return;
            }
            if (State != RaceState.Racing)
                return;
            RaceTime += Time.deltaTime;
            if (!Player.Driver || !Player.Driver.enabled)
                Player.Vehicle.Input = ReadPlayerInput();
            foreach (var e in Entrants)
            {
                if (e.Progress.Finished)
                    continue;
                var p = e.Vehicle.Location;
                bool lap = e.Progress.Tick(p.Distance, p.Lateral, Circuit.Track.Length, Circuit.Track.HalfWidth, RaceTime, Time.deltaTime, Settings.Laps);
                if (lap)
                {
                    Debug.Log("LAP_COMPLETE driver=" + e.Name + " lap=" + e.Progress.CompletedLaps + " time=" + e.Progress.LastLap);
                    if (e == Player)
                    {
                        cues.Lap();
                        Toast("LAP " + e.Progress.CompletedLaps + " / " + Settings.Laps + "   •   " + FormatTime(e.Progress.LastLap));
                        if (!DiagnosticMode && (BestSavedLap == 0 || e.Progress.LastLap < BestSavedLap))
                        {
                            BestSavedLap = e.Progress.LastLap;
                            PlayerPrefs.SetFloat(BestKey(), BestSavedLap);
                            PlayerPrefs.Save();
                        }
                    }
                    if (e.Progress.Finished)
                    {
                        e.FinishTime = RaceTime;
                        if (e == Player)
                        {
                            State = RaceState.Results;
                            cues.Finish();
                            foreach (var car in Entrants)
                            {
                                car.Vehicle.ControlEnabled = false;
                                car.Vehicle.Input = DriveInput.Stopped;
                            }
                            Debug.Log("RACE_FINISHED position=" + Position(Player) + " time=" + RaceTime);
                        }
                    }
                }
            }
            if (Player.Vehicle.transform.position.y < -8)
                Recover(Player);
        }
        DriveInput ReadPlayerInput()
        {
            float throttle = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
            float steer = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            if (Settings.AssistedSteering && Mathf.Abs(steer) < .01f && Player.Vehicle.Speed > 8)
            {
                Vector3 local = Player.Vehicle.transform.InverseTransformDirection(Player.Vehicle.Body.linearVelocity.normalized);
                steer = Mathf.Clamp(local.x * .28f, -.12f, .12f);
            }
            return new DriveInput { Throttle = throttle, Steering = steer, Handbrake = Input.GetKey(KeyCode.Space), Boost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) };
        }
        public void Pause()
        {
            if (State != RaceState.Racing)
                return;
            State = RaceState.Paused;
            Time.timeScale = 0;
            AudioListener.pause = true;
        }
        public void Resume()
        {
            if (State != RaceState.Paused)
                return;
            State = RaceState.Racing;
            Time.timeScale = 1;
            AudioListener.pause = false;
        }
        public void Garage()
        {
            Time.timeScale = 1;
            AudioListener.pause = false;
            State = RaceState.Garage;
            foreach (var e in Entrants)
                e.Vehicle.ControlEnabled = false;
            ReturnToGarage?.Invoke();
        }
        public void Recover(RaceEntrant entrant)
        {
            if (entrant == null || State != RaceState.Racing)
                return;
            if (entrant == Player && RaceTime - lastRecovery < 3)
                return;
            float safe = Mathf.Min(entrant.Progress.AbsoluteDistance, entrant.Progress.CompletedLaps * Circuit.Track.Length + (entrant.Progress.NextSector - 1) * Circuit.Track.Length / LapProgress.SectorCount);
            safe = Mathf.Max(entrant.Progress.CompletedLaps * Circuit.Track.Length - 3, safe - 4);
            entrant.Vehicle.ResetAt(safe, entrant.Lane);
            entrant.Progress.Recover(safe, Circuit.Track.Length);
            entrant.Recoveries++;
            if (entrant == Player)
            {
                Penalty += 5;
                RaceTime += 5;
                lastRecovery = RaceTime;
                Toast("RECOVERED TO LAST SECTOR   •   +5 SECONDS");
            }
        }
        public int Position(RaceEntrant target)
        {
            int position = 1;
            foreach (var e in Entrants)
            {
                if (e == target)
                    continue;
                if (e.Progress.Finished)
                {
                    if (!target.Progress.Finished || e.FinishTime < target.FinishTime)
                        position++;
                }
                else if (!target.Progress.Finished && e.Progress.AbsoluteDistance > target.Progress.AbsoluteDistance)
                    position++;
            }
            return position;
        }
        public List<RaceEntrant> Standings()
        {
            var sorted = new List<RaceEntrant>(Entrants);
            sorted.Sort((a, b) => Position(a).CompareTo(Position(b)));
            return sorted;
        }
        public void Toast(string message)
        {
            Notification = message;
            toast = 4;
        }
        public static string FormatTime(float t) => string.Format("{0:00}:{1:00.00}", (int)t / 60, t % 60);
        void OnDestroy()
        {
            Time.timeScale = 1;
            AudioListener.pause = false;
        }
    }
}
