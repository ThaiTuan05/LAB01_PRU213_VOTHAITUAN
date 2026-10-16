using System.Collections.Generic;
using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>Sound effects used by the game.</summary>
    public enum Sfx
    {
        Laser,
        Pickup,
        Explosion,
        Hit,
        Click,
        PowerUp,
        Shield,
        EnemyShoot,
    }

    /// <summary>
    /// Plays short sound effects. The clips are synthesized from code when the game
    /// starts (simple waveforms + noise), so the project needs no audio files.
    /// The mute setting is saved in PlayerPrefs and applied through AudioListener.volume.
    ///
    /// Usage: <c>SoundManager.Play(Sfx.Laser);</c>
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private const string MuteKey = "SpaceExplorer.Muted";
        private const int SampleRate = 44100;

        private static SoundManager instance;

        private AudioSource source;
        private Dictionary<Sfx, AudioClip> clips;

        /// <summary>True when all game audio is muted.</summary>
        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MuteKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MuteKey, value ? 1 : 0);
                PlayerPrefs.Save();
                AudioListener.volume = value ? 0f : 1f;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplySavedMute() => AudioListener.volume = Muted ? 0f : 1f;

        /// <summary>Plays a sound effect once.</summary>
        public static void Play(Sfx sfx, float volume = 1f)
        {
            var manager = GetOrCreate();
            manager.source.PlayOneShot(manager.clips[sfx], volume);
        }

        private static SoundManager GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            var go = new GameObject("SoundManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SoundManager>();
            instance.source = go.AddComponent<AudioSource>();
            instance.source.playOnAwake = false;
            instance.clips = new Dictionary<Sfx, AudioClip>
            {
                { Sfx.Laser, Synth("Laser", 0.14f, LaserWave) },
                { Sfx.Pickup, Synth("Pickup", 0.25f, PickupWave) },
                { Sfx.Explosion, Synth("Explosion", 0.45f, ExplosionWave) },
                { Sfx.Hit, Synth("Hit", 0.35f, HitWave) },
                { Sfx.Click, Synth("Click", 0.05f, ClickWave) },
                { Sfx.PowerUp, Synth("PowerUp", 0.3f, PowerUpWave) },
                { Sfx.Shield, Synth("Shield", 0.25f, ShieldWave) },
                { Sfx.EnemyShoot, Synth("EnemyShoot", 0.16f, EnemyShootWave) },
            };
            return instance;
        }

        // ---------------------------------------------------------------- synthesis

        private delegate float Wave(float t, float progress);

        private static AudioClip Synth(string name, float duration, Wave wave)
        {
            int count = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = Mathf.Clamp(wave(i / (float)SampleRate, i / (float)count), -1f, 1f);
            }

            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static readonly System.Random Noise = new System.Random(7);
        private static float lowPassed;

        private static float WhiteNoise() => (float)(Noise.NextDouble() * 2.0 - 1.0);

        /// <summary>Fast falling "pew": frequency sweeps 1400 Hz -> 300 Hz.</summary>
        private static float LaserWave(float t, float p)
        {
            float freq = Mathf.Lerp(1400f, 300f, p);
            float phase = t * freq * (1f - p * 0.5f);
            return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * phase)) * 0.18f * (1f - p) * (1f - p);
        }

        /// <summary>Two rising chime notes.</summary>
        private static float PickupWave(float t, float p)
        {
            float freq = p < 0.35f ? 988f : 1319f;
            return Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f * (1f - p);
        }

        /// <summary>Low-passed noise burst with a long decay.</summary>
        private static float ExplosionWave(float t, float p)
        {
            lowPassed = Mathf.Lerp(lowPassed, WhiteNoise(), 0.12f);
            return lowPassed * 1.6f * Mathf.Pow(1f - p, 2.5f);
        }

        /// <summary>Heavy descending buzz + noise for taking damage.</summary>
        private static float HitWave(float t, float p)
        {
            float freq = Mathf.Lerp(260f, 60f, p);
            float tone = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.25f;
            return (tone + WhiteNoise() * 0.15f) * (1f - p);
        }

        /// <summary>Very short UI tick.</summary>
        private static float ClickWave(float t, float p) => Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.25f * (1f - p);

        /// <summary>Rising three-note arpeggio for collecting a power-up.</summary>
        private static float PowerUpWave(float t, float p)
        {
            float freq = p < 0.33f ? 523f : p < 0.66f ? 659f : 880f;
            return Mathf.Sin(2f * Mathf.PI * freq * t) * 0.3f * (1f - p * 0.5f);
        }

        /// <summary>Short metallic ping when the shield absorbs a hit.</summary>
        private static float ShieldWave(float t, float p)
        {
            float tone = Mathf.Sin(2f * Mathf.PI * 700f * t) + Mathf.Sin(2f * Mathf.PI * 1050f * t) * 0.6f;
            return tone * 0.18f * Mathf.Pow(1f - p, 2f);
        }

        /// <summary>Descending "angry" zap used by enemy shots.</summary>
        private static float EnemyShootWave(float t, float p)
        {
            float freq = Mathf.Lerp(720f, 180f, p);
            return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.16f * (1f - p);
        }
    }
}
