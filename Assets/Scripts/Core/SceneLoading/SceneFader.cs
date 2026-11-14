using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Wickfall.Core
{
    /// <summary>
    /// Fades the screen to black, loads a scene, then fades back in. Creates itself on first use and survives
    /// the scene change (DontDestroyOnLoad), so no setup is needed. While fading, a full-screen overlay blocks clicks.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        private static SceneFader instance;

        private Image overlay;
        private Coroutine activeFade;
        private bool revealing;

        public static bool IsFading { get; private set; }

        /// <summary>
        /// Fades out, loads the scene by name (it must be in the Build Profiles scene list), fades in.
        /// Returns false, and does nothing, if already fading or the scene is not in the build.
        /// </summary>
        public static bool FadeToScene(string sceneName, float fadeOutSeconds, float fadeInSeconds)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Wickfall] Scene '{sceneName}' is not in File > Build Profiles > Scene List.");
                return false;
            }

            var fader = Instance;
            // If a player confirms during the menu's opening fade, let the scene change take over.
            if (IsFading)
            {
                if (!fader.revealing) return false;
                if (fader.activeFade != null) fader.StopCoroutine(fader.activeFade);
                fader.revealing = false;
                IsFading = false;
            }

            fader.activeFade = fader.StartCoroutine(fader.Run(sceneName, fadeOutSeconds, fadeInSeconds));
            return true;
        }

        /// <summary>Loads a scene without a transition, cancelling the menu's opening fade if needed.</summary>
        public static bool LoadSceneImmediately(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Wickfall] Scene '{sceneName}' is not in File > Build Profiles > Scene List.");
                return false;
            }

            var fader = Instance;
            if (fader.activeFade != null) fader.StopCoroutine(fader.activeFade);
            fader.activeFade = null;
            fader.revealing = false;
            IsFading = false;
            fader.overlay.raycastTarget = false;
            fader.SetAlpha(0f);
            SceneManager.LoadScene(sceneName);
            return true;
        }

        /// <summary>Starts fully black and fades in. Used when a scene opens on its own (the main menu at launch).</summary>
        public static void FadeFromBlack(float seconds)
        {
            if (IsFading) return;
            var fader = Instance;
            fader.activeFade = fader.StartCoroutine(fader.Reveal(seconds));
        }

        private static SceneFader Instance => instance != null ? instance : instance = Create();

        private static SceneFader Create()
        {
            var go = new GameObject("SceneFader", typeof(Canvas), typeof(GraphicRaycaster));
            DontDestroyOnLoad(go);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // above every other UI

            var overlayGo = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlayGo.transform.SetParent(go.transform, false);
            var rect = (RectTransform)overlayGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var fader = go.AddComponent<SceneFader>();
            fader.overlay = overlayGo.GetComponent<Image>();
            fader.overlay.raycastTarget = false;
            fader.SetAlpha(0f);
            return fader;
        }

        private IEnumerator Run(string sceneName, float fadeOutSeconds, float fadeInSeconds)
        {
            IsFading = true;
            revealing = false;
            overlay.raycastTarget = true;

            yield return Fade(overlay.color.a, 1f, fadeOutSeconds);

            var load = SceneManager.LoadSceneAsync(sceneName);
            while (!load.isDone) yield return null;
            // One extra frame so the new scene's Start methods run (camera snaps to the player) before revealing it.
            yield return null;

            yield return Fade(1f, 0f, fadeInSeconds);

            overlay.raycastTarget = false;
            IsFading = false;
            activeFade = null;
        }

        private IEnumerator Reveal(float seconds)
        {
            IsFading = true;
            revealing = true;
            SetAlpha(1f);
            yield return null;
            yield return Fade(1f, 0f, seconds);
            revealing = false;
            IsFading = false;
            activeFade = null;
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            // Unscaled time, so fades still work if the game is paused (timeScale 0).
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds)));
                yield return null;
            }
            SetAlpha(to);
        }

        private void SetAlpha(float alpha) => overlay.color = new Color(0f, 0f, 0f, alpha);

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                IsFading = false;
            }
        }
    }
}
