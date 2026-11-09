using UnityEngine;

namespace Wickfall.Core
{
    /// <summary>
    /// Player settings saved between sessions (PlayerPrefs) and applied when the game starts,
    /// so they work in every scene, not only after visiting the Settings screen.
    /// </summary>
    public static class GameSettings
    {
        private const string VolumeKey = "wickfall.volume";
        private const string FullscreenKey = "wickfall.fullscreen";

        /// <summary>Master volume, 0 to 1.</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
                AudioListener.volume = Mathf.Clamp01(value);
            }
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                ApplyFullscreen(value);
            }
        }

        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup()
        {
            AudioListener.volume = Volume;
            ApplyFullscreen(Fullscreen);
        }

        private static void ApplyFullscreen(bool fullscreen)
        {
            // In the editor the Game view decides; only builds switch window mode.
            if (Application.isEditor) return;
            Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
    }
}
