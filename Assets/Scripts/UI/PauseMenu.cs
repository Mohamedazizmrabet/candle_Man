using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wickfall.Core;

namespace Wickfall.UI
{
    /// <summary>
    /// Escape (or gamepad Start) pauses the game: time stops, gameplay input is ignored, and the menu offers
    /// Resume, Main Menu and Quit. Escape again resumes.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private InputReaderSO input;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button resume;
        [SerializeField] private Button mainMenu;
        [SerializeField] private Button quit;

        [Header("Back to the main menu")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.6f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 0.8f;

        private GameObject lastSelected;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            resume.onClick.AddListener(Resume);
            mainMenu.onClick.AddListener(BackToMainMenu);
            quit.onClick.AddListener(Quit);
            Show(false);
        }

        private void OnEnable()
        {
            if (input != null) input.PausePressed += Toggle;
        }

        private void OnDisable()
        {
            if (input != null) input.PausePressed -= Toggle;
        }

        private void OnDestroy()
        {
            // Never leave the game frozen when this scene unloads.
            if (IsPaused) SetPaused(false);
        }

        private void Update()
        {
            if (!IsPaused) return;

            // Clicking empty space clears the selection; take it back so the keyboard keeps working.
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;
            if (eventSystem.currentSelectedGameObject != null) lastSelected = eventSystem.currentSelectedGameObject;
            else if (lastSelected != null) eventSystem.SetSelectedGameObject(lastSelected);
        }

        private void Toggle()
        {
            if (SceneFader.IsFading) return;
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            SetPaused(true);
            resume.Select();
            lastSelected = resume.gameObject;
        }

        public void Resume() => SetPaused(false);

        private void BackToMainMenu()
        {
            if (SceneFader.IsFading) return;
            SetPaused(false);
            SceneFader.FadeToScene(mainMenuScene, fadeOutSeconds, fadeInSeconds);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (input != null) input.Paused = paused;
            Show(paused);
            if (!paused && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void Show(bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
