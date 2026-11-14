using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wickfall.Core
{
    /// <summary>Creates a dark, candlelit dungeon score and keeps it playing across scene changes.</summary>
    public sealed class GameMusic : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const int TrackSeconds = 20;
        private static GameMusic instance;
        private AudioSource source;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            AttachAudioListener();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
        }

        public static void EnsurePlaying()
        {
            if (instance != null) return;

            var existing = FindFirstObjectByType<GameMusic>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            var musicObject = new GameObject("Game Music");
            instance = musicObject.AddComponent<GameMusic>();
            instance.Play();
        }

        private void Play()
        {
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.clip = CreateAmbientClip();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0.28f;
            source.spatialBlend = 0f;
            source.Play();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachAudioListener();

        private static void AttachAudioListener()
        {
            var camera = Camera.main;
            if (camera != null && camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();
        }

        private static AudioClip CreateAmbientClip()
        {
            int length = SampleRate * TrackSeconds;
            var samples = new float[length];
            // Slow C-minor harmony, low organ-like tones, and a sparse bell motif for a dark
            // fantasy dungeon feel. No imported audio asset is needed.
            double[][] chords =
            {
                new[] { 65.41, 130.81, 155.56, 196.00 }, // C minor
                new[] { 58.27, 116.54, 146.83, 174.61 }, // Bb
                new[] { 49.00, 98.00, 130.81, 155.56 },   // G minor
                new[] { 58.27, 116.54, 146.83, 174.61 }, // Bb
            };
            double[] melody = { 392.00, 0, 311.13, 0, 349.23, 0, 293.66, 0, 261.63, 0 };

            for (int i = 0; i < length; i++)
            {
                double time = (double)i / SampleRate;
                int chord = (int)(time / 5.0) % chords.Length;
                double beat = time % 5.0;
                double pad = 0;
                foreach (double note in chords[chord])
                {
                    double phase = time * note * 2.0 * System.Math.PI;
                    double vibrato = System.Math.Sin(time * 2.0 * System.Math.PI * 0.12) * 0.003;
                    pad += System.Math.Sin(phase * (1.0 + vibrato)) + 0.16 * System.Math.Sin(phase * 2.0);
                }

                int melodyIndex = (int)(time / 2.0) % melody.Length;
                double melodyNote = melody[melodyIndex];
                double noteAge = time % 2.0;
                double melodyEnvelope = melodyNote == 0 ? 0 : System.Math.Exp(-noteAge * 1.5);
                double phaseLead = time * melodyNote * 2.0 * System.Math.PI;
                double bell = melodyNote == 0 ? 0 :
                    (System.Math.Sin(phaseLead) + 0.3 * System.Math.Sin(phaseLead * 2.01)) * melodyEnvelope;
                double swell = 0.8 + 0.2 * System.Math.Sin(time * 2.0 * System.Math.PI / 10.0);
                double edgeFade = System.Math.Min(1.0, System.Math.Min(time / 0.25, (TrackSeconds - time) / 0.25));
                samples[i] = (float)((pad * 0.055 + bell * 0.07) * swell * edgeFade);
            }

            var clip = AudioClip.Create("Wickfall Ambient", length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
