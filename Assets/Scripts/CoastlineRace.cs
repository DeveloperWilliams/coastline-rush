using UnityEngine;
using Coastline;

/// <summary>Scene entry point. Builds art and wires independent gameplay systems.</summary>
public sealed class CoastlineRace : MonoBehaviour
{
    public VehicleTuning vehicleTuning;
    public RaceSettings Settings
    {
        get; private set;
    }
    public RaceDirector Director
    {
        get; private set;
    }
    GameObject world; RaceHUD hud; VehicleTuning runtimeTuning;
    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        Time.fixedDeltaTime = .02f;
        Time.maximumDeltaTime = .1f;
        Settings = new RaceSettings();
        try
        {
            string saved = PlayerPrefs.GetString("cr-v2-settings", "");
            if (!string.IsNullOrEmpty(saved))
                JsonUtility.FromJsonOverwrite(saved, Settings);
        }
        catch (System.Exception) { Settings = new RaceSettings(); }
#if UNITY_EDITOR
        if (UnityEditor.SessionState.GetBool("Coastline.Validate", false))
        {
            UnityEditor.SessionState.SetString("Coastline.SettingsBeforeValidation", JsonUtility.ToJson(Settings));
            Settings.Laps = 3;
            Settings.Mode = RaceMode.Circuit;
            Settings.Difficulty = Difficulty.Club;
            Settings.Track = 0;
            Settings.Car = 0;
            Settings.Weather = RoadWeather.Dry;
            if (UnityEditor.SessionState.GetInt("Coastline.ValidationPreset", 0) == 1)
            {
                Settings.Track = 1;
                Settings.Car = 1;
                Settings.Weather = RoadWeather.Wet;
            }
        }
#endif
        Settings.Track = Mathf.Clamp(Settings.Track, 0, 1);
        Settings.Car = Mathf.Clamp(Settings.Car, 0, 2);
        Settings.Laps = Mathf.Clamp(Settings.Laps, 1, 5);
        Settings.Volume = Mathf.Clamp01(Settings.Volume);
        Settings.EngineVolume = Mathf.Clamp01(Settings.EngineVolume);
        Settings.EffectsVolume = Mathf.Clamp01(Settings.EffectsVolume);
        runtimeTuning = vehicleTuning ? Instantiate(vehicleTuning) : VehicleTuning.Defaults();
        hud = gameObject.AddComponent<RaceHUD>();
        hud.Bootstrap = this;
        BuildWorld();
#if UNITY_EDITOR
  RaceDiagnostics.Begin(this);
#endif
    }
    public void BuildWorld()
    {
        if (world)
        {
            world.SetActive(false);
            Destroy(world);
        }
        Time.timeScale = 1;
        AudioListener.pause = false;
        world = new GameObject("Coastline / generated world");
        world.transform.SetParent(transform, false);
        var track = new TrackPath(Settings.Track);
        var circuit = world.AddComponent<CircuitBuilder>();
        circuit.Build(track, Settings.Weather == RoadWeather.Wet);
        Director = world.AddComponent<RaceDirector>();
        Director.Initialize(Settings, circuit, runtimeTuning);
        Director.ReturnToGarage = () => SaveSettings();
        var cameraObject = new GameObject("Player camera");
        cameraObject.transform.SetParent(world.transform, false);
        var camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.farClipPlane = 1200;
        camera.nearClipPlane = .15f;
        camera.allowHDR = true;
        cameraObject.AddComponent<AudioListener>();
        var chase = cameraObject.AddComponent<ChaseCamera>();
        chase.Director = Director;
        chase.Target = Director.Player.Vehicle;
        chase.Snap();
        hud.Director = Director;
        SaveSettings();
    }
    public void SaveSettings()
    {
        PlayerPrefs.SetString("cr-v2-settings", JsonUtility.ToJson(Settings));
        PlayerPrefs.Save();
        AudioListener.volume = Settings.Volume;
    }
    void OnDestroy()
    {
        if (runtimeTuning)
            Destroy(runtimeTuning);
        Time.timeScale = 1;
        AudioListener.pause = false;
    }
}
