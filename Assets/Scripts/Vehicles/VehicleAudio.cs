using UnityEngine;

namespace Coastline
{
    /// <summary>Shared combustion loops mixed by speed, load and tyre contact.</summary>
    [RequireComponent(typeof(VehicleController))]
    public sealed class VehicleAudio : MonoBehaviour
    {
        public RaceSettings Settings;
        VehicleController vehicle;
        AudioSource idle, load, tyres, wind, nitro, impact;
        AudioClip impactClip;
        float rpm, lastImpact = -1;

        void Start()
        {
            vehicle = GetComponent<VehicleController>();
            idle = Source("engine-idle", true);
            load = Source("engine-load", true);
            tyres = Source("tyre-scrub", true);
            wind = Source("road-wind", true);
            nitro = Source("nitro-air", true);
            impact = Source(null, false);
            impactClip = Resources.Load<AudioClip>("Audio/impact");
        }

        AudioSource Source(string clip, bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = loop;
            source.volume = 0;
            source.playOnAwake = false;
            source.spatialBlend = vehicle.IsPlayer ? 0 : 1;
            source.minDistance = 4;
            source.maxDistance = 65;
            source.dopplerLevel = vehicle.IsPlayer ? 0 : .35f;
            source.rolloffMode = AudioRolloffMode.Linear;
            if (clip != null)
            {
                source.clip = Resources.Load<AudioClip>("Audio/" + clip);
                // Rivals should not run in phase and reinforce the same pulse.
                if (source.clip)
                {
                    source.time = Random.value * source.clip.length;
                    source.Play();
                }
            }
            return source;
        }

        void Update()
        {
            if (!vehicle || !idle)
                return;
            float throttle = vehicle.ControlEnabled ? Mathf.Abs(vehicle.Input.Throttle) : 0;
            rpm = Mathf.Lerp(rpm, vehicle.Drivetrain.NormalizedRpm, Time.deltaTime * 9);
            float engineVolume = Settings == null ? .85f : Settings.EngineVolume;
            float effectsVolume = Settings == null ? .8f : Settings.EffectsVolume;
            float distanceMix = vehicle.IsPlayer ? 1 : vehicle.ControlEnabled ? .38f : 0;
            float gain = Time.timeScale == 0 ? 0 : engineVolume * distanceMix;
            float shiftDip = vehicle.Drivetrain.IsShifting ? .42f : 1;
            idle.pitch = .85f + rpm * 1.6f;
            load.pitch = .65f + rpm * 1.25f;
            idle.volume = (.12f + (1 - rpm) * .08f) * gain;
            load.volume = (.08f + throttle * .19f) * Mathf.Lerp(.25f, 1, rpm) * gain * shiftDip;
            float fx = Time.timeScale == 0 ? 0 : effectsVolume * distanceMix;
            float scrub = vehicle.GroundedWheels > 0 && vehicle.Speed > 4 ? Mathf.Clamp01((vehicle.Slip - .15f) * 1.5f) : 0;
            tyres.volume = Mathf.MoveTowards(tyres.volume, scrub * .22f * fx, Time.unscaledDeltaTime * 2);
            tyres.pitch = .85f + Mathf.Clamp01(vehicle.Speed / 50) * .3f;
            wind.volume = Mathf.Clamp01(vehicle.Speed / 60) * .13f * fx;
            nitro.volume = Mathf.MoveTowards(nitro.volume, vehicle.Boosting ? .22f * fx : 0, Time.unscaledDeltaTime * 3);
        }

        public void PlayImpact(float strength)
        {
            if (!impact || Time.time - lastImpact < .12f)
                return;
            lastImpact = Time.time;
            impact.volume = (Settings == null ? .8f : Settings.EffectsVolume) * (vehicle.IsPlayer ? 1 : .4f);
            impact.pitch = Random.Range(.85f, 1.12f);
            if (impactClip)
                impact.PlayOneShot(impactClip, Mathf.Clamp01(strength) * .7f);
        }
    }
}
