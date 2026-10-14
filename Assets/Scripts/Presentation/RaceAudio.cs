using UnityEngine;

namespace Coastline
{
    /// <summary>Race and UI cues remain audible independently of engine volume.</summary>
    public sealed class RaceAudio : MonoBehaviour
    {
        public RaceSettings Settings;
        AudioSource source;
        AudioClip tick, go, lap, finish, click;
        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.ignoreListenerPause = true;
            tick = Resources.Load<AudioClip>("Audio/countdown");
            go = Resources.Load<AudioClip>("Audio/go");
            lap = Resources.Load<AudioClip>("Audio/lap");
            finish = Resources.Load<AudioClip>("Audio/finish");
            click = Resources.Load<AudioClip>("Audio/click");
        }
        void Play(AudioClip clip, float volume)
        {
            if (clip)
                source.PlayOneShot(clip, volume * (Settings == null ? .8f : Settings.EffectsVolume));
        }
        public void BeginCountdown()
        {
            source.Stop();
            Tick();
        }
        public void Tick() => Play(tick, .35f);
        public void Go() => Play(go, .45f);
        public void Lap() => Play(lap, .32f);
        public void Finish() => Play(finish, .45f);
        public void Click() => Play(click, .2f);
    }
}
