namespace Coastline
{
    public enum RaceState
    {
        Garage, Countdown, Racing, Paused, Results
    }
    public enum RaceMode
    {
        Circuit, TimeTrial
    }
    public enum Difficulty
    {
        Rookie, Club, Expert
    }
    public enum RoadWeather
    {
        Dry, Wet
    }
    [System.Serializable]
    public sealed class RaceSettings
    {
        public int Track, Car, Laps = 3;
        public Difficulty Difficulty = Difficulty.Club;
        public RaceMode Mode = RaceMode.Circuit;
        public RoadWeather Weather = RoadWeather.Dry;
        public float Volume = .65f, EngineVolume = .85f, EffectsVolume = .8f;
        public bool AssistedSteering = true, ShowRacingLine = true;
        public float Pace => Difficulty == Difficulty.Rookie ? .78f : Difficulty == Difficulty.Expert ? 1.03f : .9f;
    }
}
