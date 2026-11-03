using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Wickfall.Core
{
    /// <summary>
    /// Fades the screen to black, loads a scene, then fades back in. Creates itself on first use and survives
    /// the scene change (DontDestroyOnLoad), so no setup is needed: call <see cref="FadeToScene"/>.
    /// While fading, a full-screen overlay blocks clicks.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        private static SceneFader instance;

        private Image overlay;

        public static bool IsFading { get; private set; }

        /// <summary>Fades out, loads the scene at this Build Profiles index, fades in. Ignored if already fading.</summary>
        public static void FadeToScene(int buildIndex, float fadeOutSeconds, float fadeInSeconds)
        {
            if (IsFading) return;

            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"[Wickfall] No scene at build index {buildIndex}. Add it in File > Build Profiles > Scene List.");
                return;
            }

            if (instance == null) instance = Create();
            instance.StartCoroutine(instance.Run(buildIndex, fadeOutSeconds, fadeInSeconds));
        }

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
            fader.SetAlpha(0f);
            return fader;
        }

        private IEnumerator Run(int buildIndex, float fadeOutSeconds, float fadeInSeconds)
        {
            IsFading = true;
            overlay.raycastTarget = true;

            yield return Fade(0f, 1f, fadeOutSeconds);

            var load = SceneManager.LoadSceneAsync(buildIndex);
            while (!load.isDone) yield return null;
            // One extra frame so the new scene's Start methods run (camera snaps to the player) before revealing it.
            yield return null;

            yield return Fade(1f, 0f, fadeInSeconds);

            overlay.raycastTarget = false;
            IsFading = false;
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
