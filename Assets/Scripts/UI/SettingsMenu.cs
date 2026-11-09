using UnityEngine;
using UnityEngine.UI;
using Wickfall.Core;

namespace Wickfall.UI
{
    /// <summary>
    /// Settings screen: master volume and fullscreen. Values are applied right away and saved (GameSettings).
    /// Up/down moves between rows, left/right changes the volume, Enter toggles fullscreen.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private Slider volume;
        [SerializeField] private Text volumeValue;
        [SerializeField] private Toggle fullscreen;
        [SerializeField] private Button back;

        /// <summary>The row selected when the screen opens.</summary>
        public Selectable FirstRow => volume;
        public Button Back => back;

        private void Awake()
        {
            volume.minValue = 0f;
            volume.maxValue = 10f;
            volume.wholeNumbers = true; // 10% steps, so left/right feels crisp
            volume.SetValueWithoutNotify(Mathf.Round(GameSettings.Volume * 10f));
            fullscreen.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            ShowVolume(volume.value);

            volume.onValueChanged.AddListener(OnVolumeChanged);
            fullscreen.onValueChanged.AddListener(OnFullscreenChanged);
        }

        private void OnVolumeChanged(float steps)
        {
            GameSettings.Volume = steps / 10f;
            ShowVolume(steps);
        }

        private void OnFullscreenChanged(bool on) => GameSettings.Fullscreen = on;

        private void ShowVolume(float steps)
        {
            if (volumeValue != null) volumeValue.text = $"{steps * 10f:0}%";
        }

        private void OnDisable() => GameSettings.Save();
    }
}
