using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wickfall.Core;

namespace Wickfall.UI
{
    /// <summary>
    /// Main menu: New Game, Continue, Settings, Quit. The glowing frame follows the selected entry
    /// (keyboard Z/S or arrows, gamepad, or mouse hover). Enter or a click confirms.
    /// New Game and Continue fade to black, load their scene, and fade back in.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Entries (top to bottom)")]
        [SerializeField] private MainMenuButton newGame;
        [SerializeField] private MainMenuButton continueGame;
        [SerializeField] private MainMenuButton settings;
        [SerializeField] private MainMenuButton quit;

        [Header("Highlight frame")]
        [SerializeField] private RectTransform highlight;
        [SerializeField] private Image highlightImage;
        [Tooltip("Gentle breathing of the frame's glow. 0 = still, like the reference image.")]
        [SerializeField, Range(0f, 0.3f)] private float pulseAmount = 0.06f;
        [SerializeField, Min(0f)] private float pulseSpeed = 2f;

        [Header("Scenes (index in File > Build Profiles > Scene List)")]
        [Tooltip("Loaded by New Game. 0 is this menu, 1 is the first game scene.")]
        [SerializeField, Min(0)] private int newGameScene = 1;
        [Tooltip("Loaded by Continue. Will come from the save file once saving exists.")]
        [SerializeField, Min(0)] private int continueScene = 1;

        [Header("Transition")]
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.6f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 0.8f;

        private MainMenuButton selected;

        private void Awake()
        {
            newGame.Button.onClick.AddListener(OnNewGame);
            continueGame.Button.onClick.AddListener(OnContinue);
            settings.Button.onClick.AddListener(OnSettings);
            quit.Button.onClick.AddListener(OnQuit);
        }

        private void Start() => newGame.Button.Select();

        private void Update()
        {
            // Clicking empty space clears the UI selection; take it back so the keyboard keeps working.
            var eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.currentSelectedGameObject == null && selected != null)
            {
                selected.Button.Select();
            }

            if (highlightImage != null && pulseAmount > 0f)
            {
                float glow = 1f - pulseAmount * 0.5f * (1f + Mathf.Sin(Time.unscaledTime * pulseSpeed));
                highlightImage.color = new Color(glow, glow, glow, 1f);
            }
        }

        /// <summary>Moves the frame onto the entry. Each entry's rect is exactly the frame's size.</summary>
        public void OnButtonSelected(MainMenuButton button)
        {
            selected = button;
            if (highlight == null) return;
            highlight.anchorMin = button.Rect.anchorMin;
            highlight.anchorMax = button.Rect.anchorMax;
        }

        private void OnNewGame() => SceneFader.FadeToScene(newGameScene, fadeOutSeconds, fadeInSeconds);

        private void OnContinue() => SceneFader.FadeToScene(continueScene, fadeOutSeconds, fadeInSeconds);

        // Settings screen comes later.
        private void OnSettings() => Debug.Log("[Wickfall] Settings: not built yet.");

        private void OnQuit()
        {
            if (SceneFader.IsFading) return;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
