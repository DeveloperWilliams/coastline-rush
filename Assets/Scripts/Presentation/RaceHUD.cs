using System.Collections.Generic;
using UnityEngine;
namespace Coastline
{
    public sealed class RaceHUD : MonoBehaviour
    {
        public CoastlineRace Bootstrap;
        public RaceDirector Director;
        Texture2D pixel; readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>(); GUIStyle button, buttonCaption;
        readonly Color mint = new Color(.35f, .98f, .79f), muted = new Color(.65f, .74f, .75f), ink = new Color(.025f, .065f, .09f, .93f);
        float vw, vh; bool telemetry, credits;
        Font regular, semibold, bold;
        void Awake()
        {
            regular = Resources.Load<Font>("Fonts/JosefinSans-regular");
            semibold = Resources.Load<Font>("Fonts/JosefinSans-semibold");
            bold = Resources.Load<Font>("Fonts/JosefinSans-bold");
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                telemetry = !telemetry;
        }
        void OnDestroy()
        {
            if (pixel)
                Destroy(pixel);
        }
        GUIStyle Style(int size)
        {
            if (styles.TryGetValue(size, out var style))
                return style;
            style = new GUIStyle(GUI.skin.label) { font = size >= 24 ? bold : size >= 16 ? semibold : regular, fontSize = size, fontStyle = FontStyle.Normal, wordWrap = false };
            style.normal.textColor = Color.white;
            styles[size] = style;
            return style;
        }
        void Text(float x, float y, float w, string text, int size = 18, Color? color = null)
        {
            var style = Style(size);
            style.normal.textColor = color ?? Color.white;
            GUI.Label(new Rect(x, y, w, size * 1.65f), text, style);
        }
        void Panel(float x, float y, float w, float h, Color? color = null)
        {
            GUI.color = color ?? ink;
            GUI.DrawTexture(new Rect(x, y, w, h), pixel);
            GUI.color = Color.white;
        }
        bool Button(float x, float y, float w, string text, bool primary = false)
        {
            if (button == null)
            {
                button = new GUIStyle(GUI.skin.button) { font = semibold, fontSize = 18, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter };
                button.normal.background = pixel;
                button.hover.background = pixel;
                button.active.background = pixel;
                buttonCaption = new GUIStyle(GUI.skin.label)
                {
                    font = semibold, fontSize = 18, fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter, wordWrap = false
                };
            }
            Color bg = primary ? mint : new Color(.15f, .23f, .26f);
            button.normal.textColor = primary ? new Color(.025f, .13f, .15f) : Color.white;
            button.hover.textColor = button.normal.textColor;
            button.active.textColor = button.normal.textColor;
            GUI.backgroundColor = bg;
            var rect = new Rect(x, y, w, 48);
            bool clicked = GUI.Button(rect, GUIContent.none, button);
            GUI.backgroundColor = Color.white;
            // Keep captions on the same label rendering path as the rest of the HUD.
            // Captions remain independent of native button style state.
            if (semibold) semibold.RequestCharactersInTexture(text, 18, FontStyle.Normal);
            buttonCaption.normal.textColor = button.normal.textColor;
            GUI.Label(rect, text, buttonCaption);
            if (clicked && Director)
                Director.GetComponent<RaceAudio>()?.Click();
            return clicked;
        }
        void OnGUI()
        {
            if (!Director || !pixel)
                return;
            float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            vw = Screen.width / scale;
            vh = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            if (Director.State == RaceState.Garage)
            {
                Garage();
                return;
            }
            DrawRace();
            if (Director.State == RaceState.Countdown)
            {
                float count = Director.Countdown;
                Panel(vw / 2 - 110, vh * .35f, 220, 150, new Color(.025f, .065f, .09f, .8f));
                Text(vw / 2 - 62, vh * .35f + 25, 170, Mathf.Max(1, Mathf.CeilToInt(count)).ToString(), 80, mint);
                Text(vw / 2 - 190, vh * .35f + 165, 430, "LIGHTS OUT. FIND YOUR LINE.", 18);
            }
            if (Director.State == RaceState.Paused)
                Pause();
            if (Director.State == RaceState.Results || Director.State == RaceState.Finishing)
                Results();
        }
        void Garage()
        {
            Panel(0, 0, vw, vh, new Color(.012f, .035f, .05f, .2f));
            Panel(0, 0, 675, vh, new Color(.025f, .065f, .09f, .95f));
            Text(56, 40, 590, "OPEN ROAD  /  RACING CLUB", 13, mint);
            Text(52, 89, 620, "COASTLINE", 66);
            Text(52, 165, 620, "RUSH.", 86, mint);
            Text(58, 283, 550, "Golden roads. Real rivalries.", 24);
            Text(58, 325, 575, "Choose your car. Find the apex. Chase the coast.", 16, muted);
            var s = Bootstrap.Settings;
            Text(58, 385, 200, "YOUR CAR", 12, muted);
            Text(58, 408, 340, VehicleFactory.Names[s.Car], 26);
            if (Button(470, 402, 140, "CHANGE →"))
            {
                s.Car = (s.Car + 1) % 3;
                Bootstrap.BuildWorld();
                return;
            }
            Text(58, 463, 200, "CIRCUIT", 12, muted);
            Text(58, 487, 400, s.Track == 0 ? "PALM COAST" : "HIGHLAND LOOP", 25);
            if (Button(470, 481, 140, "CHANGE →"))
            {
                s.Track = 1 - s.Track;
                Bootstrap.BuildWorld();
                return;
            }
            Text(58, 543, 200, "EVENT", 12, muted);
            if (Button(58, 569, 262, s.Mode == RaceMode.Circuit ? "CIRCUIT / 6 DRIVERS" : "TIME TRIAL / SOLO"))
            {
                s.Mode = s.Mode == RaceMode.Circuit ? RaceMode.TimeTrial : RaceMode.Circuit;
                Bootstrap.BuildWorld();
                return;
            }
            if (Button(338, 569, 156, s.Difficulty.ToString().ToUpper()))
            {
                s.Difficulty = (Difficulty)(((int)s.Difficulty + 1) % 3);
                foreach (var e in Director.Entrants)
                    if (e.Driver)
                        e.Driver.Pace = s.Pace;
                Bootstrap.SaveSettings();
            }
            if (Button(510, 569, 100, (s.Laps + (s.Laps == 1 ? " LAP" : " LAPS"))))
            {
                s.Laps = s.Laps == 1 ? 3 : s.Laps == 3 ? 5 : 1;
                Bootstrap.SaveSettings();
            }
            if (Button(58, 632, 170, s.Weather == RoadWeather.Dry ? "DRY / SUNSET" : "WET / OVERCAST"))
            {
                s.Weather = s.Weather == RoadWeather.Dry ? RoadWeather.Wet : RoadWeather.Dry;
                Bootstrap.BuildWorld();
                return;
            }
            if (Button(244, 632, 170, "LINE: " + (s.ShowRacingLine ? "ON" : "OFF")))
            {
                s.ShowRacingLine = !s.ShowRacingLine;
                Director.Circuit.RacingLine.SetActive(false);
                Bootstrap.SaveSettings();
            }
            if (Button(430, 632, 180, "ASSIST: " + (s.AssistedSteering ? "ON" : "OFF")))
            {
                s.AssistedSteering = !s.AssistedSteering;
                Bootstrap.SaveSettings();
            }
            if (Button(58, 706, 552, "START RACE  →", true))
            {
                Bootstrap.SaveSettings();
                Director.StartRace();
                return;
            }
            Text(58, 785, 600, "ENTER  Start    •    WASD / ARROWS  Drive    •    C  Camera", 14, muted);
            Text(58, 846, 600, "GUIDE: GREEN Accelerate   AMBER Lift   RED Brake", 13, muted);
            Text(58, 814, 600, "←→ Car   ↑↓ Circuit   M Event   V Weather   L Laps", 13, muted);
            Text(vw - 405, 45, 365, "01 / COASTAL MOTORSPORT", 14, Color.white);
            Panel(vw - 370, vh - 255, 315, 192, new Color(.025f, .065f, .09f, .84f));
            Text(vw - 346, vh - 235, 265, VehicleFactory.Names[s.Car], 24, mint);
            Text(vw - 346, vh - 190, 280, s.Car == 1 ? "CLUB / STABLE / HIGH GRIP" : s.Car == 2 ? "PRO / FAST / LOW ASSIST" : "SPORT / BALANCED / AWD", 14);
            Text(vw - 346, vh - 160, 270, "SPEED LIMIT " + Mathf.RoundToInt(Director.Player.Vehicle.Tuning.topSpeed * 3.6f) + " KM/H  /  5 GEARS", 13, muted);
            Text(vw - 346, vh - 128, 270, (Director.Circuit.Track.Length / 1000).ToString("0.00") + " KM     •     " + (s.Laps + (s.Laps == 1 ? " LAP" : " LAPS")), 16);
            if (Button(vw - 370, vh - 55, 315, credits ? "CLOSE CREDITS" : "CREDITS / LICENSES"))
                credits = !credits;
            if (credits)
            {
                Panel(vw / 2 - 250, vh / 2 - 115, 570, 265);
                Text(vw / 2 - 222, vh / 2 - 94, 540, "BUILT FOR THE OPEN ROAD", 24, mint);
                Text(vw / 2 - 222, vh / 2 - 44, 535, "Code: MIT license  •  Artwork: Kenney, CC0", 17);
                Text(vw / 2 - 222, vh / 2 - 5, 535, "Car Kit 3.1 / Racing Kit 1.0 / Nature Kit 1.0", 16, muted);
                Text(vw / 2 - 222, vh / 2 + 40, 535, "Josefin Sans: SIL OFL 1.1 • Original procedural audio.", 16, muted);
                if (Button(vw / 2 - 222, vh / 2 + 89, 510, "BACK TO GARAGE"))
                    credits = false;
            }
        }
        void DrawRace()
        {
            var p = Director.Player;
            var car = p.Vehicle;
            Panel(28, 25, 280, 68);
            Text(46, 37, 260, "COASTLINE / RUSH", 23);
            Text(46, 72, 280, Director.Circuit.Track.Name + "  •  " + Director.Settings.Weather.ToString().ToUpper(), 11, mint);
            Panel(vw - 415, 25, 387, 100);
            Text(vw - 392, 38, 140, "POSITION", 12, muted);
            Text(vw - 392, 60, 130, Director.Position(p) + " / " + Director.Entrants.Count, 30, mint);
            Text(vw - 244, 38, 190, "LAP", 12, muted);
            Text(vw - 244, 60, 190, Mathf.Min(p.Progress.CompletedLaps + 1, Director.Settings.Laps) + " / " + Director.Settings.Laps, 30);
            Text(vw / 2 - 90, 30, 260, RaceDirector.FormatTime(Director.PlayerTime), 30);
            Text(vw / 2 - 98, 73, 295, "BEST  " + (Director.BestSavedLap > 0 ? RaceDirector.FormatTime(Director.BestSavedLap) : "--:--.--"), 13, muted);
            Panel(28, 124, 225, Director.Entrants.Count * 34 + 42, new Color(.025f, .065f, .09f, .72f));
            Text(46, 138, 190, "LIVE CLASSIFICATION", 11, muted);
            int row = 0;
            foreach (var e in Director.Standings())
            {
                Text(46, 168 + row * 34, 34, (row + 1).ToString(), 17, e == p ? mint : Color.white);
                Panel(80, 178 + row * 34, 6, 6, e.Color);
                Text(97, 168 + row * 34, 155, e.Name, 17, e == p ? mint : Color.white);
                row++;
            }
            Panel(28, vh - 168, 305, 140);
            Text(49, vh - 152, 90, "SPEED", 11, muted);
            Text(135, vh - 152, 170, (car.EngineRpm / 1000).ToString("0.0") + "K RPM", 11, car.Drivetrain.IsShifting ? mint : muted);
            Text(46, vh - 135, 160, Mathf.RoundToInt(car.SpeedKph).ToString(), 62);
            Text(216, vh - 108, 110, "KM/H", 16, muted);
            Text(220, vh - 143, 100, "G " + (car.ForwardSpeed < -.5f ? "R" : car.Gear.ToString()), 26, mint);
            Panel(50, vh - 66, 255, 6, new Color(.2f, .3f, .33f));
            Panel(50, vh - 66, 255 * car.Nitro, 6, mint);
            Text(50, vh - 50, 280, car.Boosting ? "NITRO ACTIVE" : "SHIFT / NITRO", 11, mint);
            MiniMap(vw - 255, vh - 253, 225);
            Panel(365, vh - 74, vw - 655, 46, new Color(.025f, .065f, .09f, .72f));
            Text(383, vh - 62, vw - 680, "SPACE  Handbrake   SHIFT  Nitro   C  Camera   Q  Look back   F  Recover   ESC  Pause", 14, muted);
            if (!string.IsNullOrEmpty(Director.Notification))
            {
                Panel(vw / 2 - 300, 142, 600, 44, new Color(.025f, .065f, .09f, .8f));
                Text(vw / 2 - 282, 152, 570, Director.Notification, 16, mint);
            }
            if (Director.State == RaceState.Racing)
            {
                float recommended = Director.Circuit.Guide.SuggestedSpeed(car.Location.Distance + 8);
                if (Director.Settings.ShowRacingLine && car.Speed > recommended + 3)
                    Text(vw / 2 - 115, 195, 350, "BRAKE  •  " + Mathf.RoundToInt(recommended * 3.6f) + " KM/H", 22, new Color(1, .5f, .3f));
                if (!p.Progress.RecordEligible && !p.Progress.LapInvalid)
                    Text(vw / 2 - 190, 232, 470, "RECOVERED LAP — RECORD INELIGIBLE", 13, muted);
            }
            if (p.Progress.WrongWay)
            {
                Panel(vw / 2 - 170, 205, 340, 48);
                Text(vw / 2 - 148, 213, 340, "WRONG WAY", 24, new Color(1, .45f, .3f));
            }
            else if (p.Progress.LapInvalid)
                Text(vw / 2 - 205, 211, 480, "LAP INVALID — VISIT ALL SECTORS", 16, new Color(1, .6f, .3f));
            if (telemetry)
            {
                Panel(28, 430, 330, 208);
                Text(48, 448, 300, "HANDLING TELEMETRY", 16, mint);
                Text(48, 480, 300, "Ground contacts: " + car.GroundedWheels + " / 4", 15);
                Text(48, 507, 300, "Side slip: " + car.Slip.ToString("0.00") + "   Steering: " + car.SteeringAngle.ToString("0.0") + "°", 15);
                Text(48, 534, 300, "Surface: " + (car.OnGrass ? "grass" : "tarmac") + "   Grip: " + Director.Settings.Weather, 15);
                Text(48, 561, 300, "Sector: " + p.Progress.NextSector + " / 12", 15);
                Text(48, 598, 300, "H  Hide telemetry", 12, muted);
            }
        }
        void MiniMap(float x, float y, float size)
        {
            Panel(x, y, size, size, new Color(.025f, .065f, .09f, .82f));
            var track = Director.Circuit.Track;
            float factor = size / 425f;
            for (int i = 0; i < track.Count; i += 3)
            {
                Vector3 p = track.Points[i];
                Panel(x + size / 2 + p.x * factor, y + size / 2 - (p.z - 25) * factor, 2.5f, 2.5f, new Color(.62f, .72f, .72f));
            }
            foreach (var e in Director.Entrants)
            {
                Vector3 p = e.Vehicle.transform.position;
                float s = e == Director.Player ? 7 : 4;
                Panel(x + size / 2 + p.x * factor - s / 2, y + size / 2 - (p.z - 25) * factor - s / 2, s, s, e.Color);
            }
            Text(x + 14, y + size - 30, size - 20, "CIRCUIT / " + (track.Length / 1000).ToString("0.00") + " KM", 10, muted);
        }
        void Pause()
        {
            Panel(0, 0, vw, vh, new Color(.005f, .02f, .03f, .65f));
            float x = vw / 2 - 230, y = vh / 2 - 270;
            Panel(x, y, 460, 550);
            Text(x + 34, y + 28, 400, "TAKE A BREATHER.", 32, mint);
            Text(x + 34, y + 86, 400, "G Garage  •  R Restart  •  F11 Screen mode", 13, muted);
            if (Button(x + 34, y + 135, 392, "RESUME  →", true))
                Director.Resume();
            if (Button(x + 34, y + 196, 392, "RESTART RACE"))
                Director.StartRace();
            if (Button(x + 34, y + 257, 392, "RETURN TO GARAGE"))
                Director.Garage();
            Text(x + 34, y + 325, 392, "MASTER VOLUME", 12, muted);
            float volume = GUI.HorizontalSlider(new Rect(x + 34, y + 357, 392, 24), Bootstrap.Settings.Volume, 0, 1);
            if (Mathf.Abs(volume - Bootstrap.Settings.Volume) > .001f)
            {
                Bootstrap.Settings.Volume = volume;
                Bootstrap.SaveSettings();
            }
            VolumeSlider(x + 34, y + 399, "ENGINE", ref Bootstrap.Settings.EngineVolume);
            VolumeSlider(x + 34, y + 465, "EFFECTS / RACE CUES", ref Bootstrap.Settings.EffectsVolume);
        }
        void VolumeSlider(float x, float y, string label, ref float value)
        {
            Text(x, y, 392, label, 12, muted);
            float updated = GUI.HorizontalSlider(new Rect(x, y + 29, 392, 24), value, 0, 1);
            if (Mathf.Abs(updated - value) > .001f)
            {
                value = updated;
                Bootstrap.SaveSettings();
            }
        }
        void Results()
        {
            Panel(0, 0, vw, vh, new Color(.005f, .02f, .03f, .55f));
            float x = vw / 2 - 320, y = vh / 2 - 310;
            Panel(x, y, 640, 650);
            int position = Director.Position(Director.Player);
            Text(x + 40, y + 24, 570, "CHEQUERED FLAG", 12, mint);
            Text(x + 36, y + 64, 590, Director.State == RaceState.Finishing ? "RACE COMPLETE." : Director.Settings.Mode == RaceMode.TimeTrial ? "TIME TRIAL COMPLETE." : position == 1 ? "COASTLINE CHAMPION." : "FINISH LINE.", 37);
            Text(x + 40, y + 128, 560, "POSITION " + position + " / " + Director.Entrants.Count + "    •    " + RaceDirector.FormatTime(Director.PlayerTime), 25, mint);
            Text(x + 40, y + 173, 560, Director.State == RaceState.Finishing
                ? "RIVALS FINISHING  /  " + Mathf.CeilToInt(Director.FinishSecondsRemaining) + "s  •  POSITION PROVISIONAL"
                : "BEST CLEAN LAP  " + (Director.Player.Progress.BestLap > 0 ? RaceDirector.FormatTime(Director.Player.Progress.BestLap) : "—")
                    + "   •   PENALTY +" + Director.Penalty.ToString("0") + "s", 16, muted);
            int row = 0;
            var standings = Director.Standings();
            foreach (var e in standings)
            {
                Panel(x + 40, y + 229 + row * 43, 560, 1, new Color(.25f, .35f, .38f));
                Text(x + 44, y + 238 + row * 43, 60, (row + 1).ToString(), 18);
                Text(x + 95, y + 238 + row * 43, 250, e.Name, 18, e.Color);
                Text(x + 365, y + 238 + row * 43, 235, e.Progress.Finished
                    ? (row == 0 ? RaceDirector.FormatTime(e.AdjustedTime) : "+" + (e.AdjustedTime - standings[0].AdjustedTime).ToString("0.00") + "s")
                        + (e.Penalty > 0 ? " (+" + e.Penalty.ToString("0") + ")" : "")
                    : Director.State == RaceState.Results ? "DNF / LAP " + (e.Progress.CompletedLaps + 1) : "LAP " + (e.Progress.CompletedLaps + 1), 17, muted);
                row++;
            }
            if (Director.State == RaceState.Finishing)
                return;
            if (Button(x + 40, y + 515, 270, "RACE AGAIN  →", true))
                Director.StartRace();
            if (Button(x + 330, y + 515, 270, "GARAGE"))
                Director.Garage();
            Text(x + 40, y + 589, 580, "ENTER  Race again    •    Explore another circuit in the garage", 13, muted);
        }
    }
}
