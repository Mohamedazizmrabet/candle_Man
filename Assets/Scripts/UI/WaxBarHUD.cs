using UnityEngine;
using UnityEngine.UI;
using Wickfall.Core.Events;

namespace Wickfall.UI
{
    /// <summary>
    /// Simple wax bar. Listens to WaxChanged only, so it works with no reference to the player.
    /// The fill scales horizontally (pivot on the left), and a pale trail shows recent damage.
    /// </summary>
    public class WaxBarHUD : MonoBehaviour
    {
        [SerializeField] private WaxEventChannelSO waxChanged;

        [Header("Bar parts (pivot X must be 0)")]
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [Tooltip("Optional. Shrinks after a short delay so hits stay readable.")]
        [SerializeField] private RectTransform trail;

        [Header("Look")]
        [SerializeField] private Gradient fillColor = DefaultGradient();
        [SerializeField, Min(0f)] private float trailDelay = 0.4f;
        [SerializeField, Min(0f)] private float trailSpeed = 0.6f;

        // A drop bigger than this (as a fraction of max) counts as a hit and restarts the trail delay.
        private const float HitThreshold = 0.01f;

        private float target = 1f;
        private float trailValue = 1f;
        private float trailTimer;

        private void OnEnable()
        {
            if (waxChanged != null) waxChanged.OnRaised += OnWaxChanged;
        }

        private void OnDisable()
        {
            if (waxChanged != null) waxChanged.OnRaised -= OnWaxChanged;
        }

        private void OnWaxChanged(WaxChange wax)
        {
            float value = wax.Normalized;
            if (target - value > HitThreshold) trailTimer = trailDelay;
            target = value;

            SetWidth(fill, target);
            if (fillImage != null) fillImage.color = fillColor.Evaluate(target);
        }

        private void Update()
        {
            if (trail == null) return;

            if (trailTimer > 0f)
            {
                trailTimer -= Time.deltaTime;
            }
            else
            {
                trailValue = Mathf.MoveTowards(trailValue, target, trailSpeed * Time.deltaTime);
            }

            // The trail never sits below the bar (gains snap it up).
            trailValue = Mathf.Max(trailValue, target);
            SetWidth(trail, trailValue);
        }

        private static void SetWidth(RectTransform bar, float normalized)
        {
            if (bar == null) return;
            var scale = bar.localScale;
            scale.x = Mathf.Clamp01(normalized);
            bar.localScale = scale;
        }

        private static Gradient DefaultGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.75f, 0.15f, 0.1f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.3f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.5f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
