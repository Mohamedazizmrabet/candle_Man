using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Wickfall.Core;

namespace Wickfall.UI
{
    /// <summary>
    /// Main menu flow. Opens with a fade from black.
    /// New Game and Continue fade out, load level 1 and fade back in. Settings swaps to the settings panel
    /// (Back or Escape returns). Keyboard Z/S or arrows, gamepad, or mouse; Enter or click confirms.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Main entries")]
        [SerializeField] private Button newGame;
        [SerializeField] private Button continueGame;
        [SerializeField] private Button settings;
        [SerializeField] private Button quit;
        [SerializeField] private CanvasGroup mainGroup;

        [Header("Settings panel")]
        [SerializeField] private SettingsMenu settingsMenu;
        [SerializeField] private CanvasGroup settingsGroup;
        [SerializeField, Min(0f)] private float panelSwapSeconds = 0.2f;

        [Header("Intro")]
        [SerializeField, Min(0f)] private float fadeFromBlackSeconds = 1f;

        [Header("Level 1")]
        [Tooltip("Scene loaded by New Game and Continue (by name, must be in File > Build Profiles > Scene List).")]
        [SerializeField] private string levelOneScene = "Level_01";
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.6f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 0.8f;

        private GameObject lastSelected;
        private bool inSettings;
        private Coroutine swap;

        private void Awake()
        {
            newGame.onClick.AddListener(LoadLevelOne);
            // No save system yet: Continue starts level 1 too.
            continueGame.onClick.AddListener(LoadLevelOne);
            settings.onClick.AddListener(OpenSettings);
            quit.onClick.AddListener(Quit);
            settingsMenu.Back.onClick.AddListener(CloseSettings);
        }

        private void Start()
        {
            SetGroup(settingsGroup, 0f, false);
            SetGroup(mainGroup, 1f, true);

            Select(newGame);
            SceneFader.FadeFromBlack(fadeFromBlackSeconds);
        }

        private void Update()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;

            // Clicking empty space clears the selection; take it back so the keyboard keeps working.
            if (eventSystem.currentSelectedGameObject != null) lastSelected = eventSystem.currentSelectedGameObject;
            else if (lastSelected != null && lastSelected.activeInHierarchy) eventSystem.SetSelectedGameObject(lastSelected);

            if (inSettings && CancelPressed(eventSystem)) CloseSettings();
        }

        // ---------- Actions ----------

        private void LoadLevelOne()
        {
            // No double clicks while fading.
            if (SceneFader.FadeToScene(levelOneScene, fadeOutSeconds, fadeInSeconds)) SetGroup(mainGroup, mainGroup.alpha, false);
        }

        private void OpenSettings()
        {
            if (inSettings || SceneFader.IsFading) return;
            inSettings = true;
            Swap(mainGroup, settingsGroup, settingsMenu.FirstRow);
        }

        private void CloseSettings()
        {
            if (!inSettings) return;
            inSettings = false;
            Swap(settingsGroup, mainGroup, settings);
        }

        private void Quit()
        {
            if (SceneFader.IsFading) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- Animation ----------

        private void Swap(CanvasGroup from, CanvasGroup to, Selectable select)
        {
            if (swap != null) StopCoroutine(swap);
            swap = StartCoroutine(SwapRoutine(from, to, select));
        }

        private IEnumerator SwapRoutine(CanvasGroup from, CanvasGroup to, Selectable select)
        {
            SetGroup(from, from.alpha, false);
            yield return FadeGroup(from, from.alpha, 0f, panelSwapSeconds);
            SetGroup(to, 0f, true);
            Select(select);
            yield return FadeGroup(to, 0f, 1f, panelSwapSeconds);
            swap = null;
        }

        private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            group.alpha = to;
        }

        // ---------- Helpers ----------

        private static void SetGroup(CanvasGroup group, float alpha, bool interactive)
        {
            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        private void Select(Selectable selectable)
        {
            if (selectable == null) return;
            selectable.Select();
            lastSelected = selectable.gameObject;
            var entry = selectable.GetComponent<MenuEntry>();
            if (entry != null) entry.MoveHighlightHere();
        }

        private static bool CancelPressed(EventSystem eventSystem)
        {
            var module = eventSystem.currentInputModule as InputSystemUIInputModule;
            var cancel = module != null && module.cancel != null ? module.cancel.action : null;
            return cancel != null && cancel.WasPressedThisFrame();
        }
    }
}
