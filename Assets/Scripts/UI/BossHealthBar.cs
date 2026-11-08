using UnityEngine;
using UnityEngine.UI;
using Wickfall.Core.Events;

namespace Wickfall.UI
{
    /// <summary>
    /// Boss health bar at the bottom of the screen. Listens to BossStatusChanged only: fades in when the boss
    /// engages, shrinks with a pale trail on hits (like the wax bar), and fades out when the fight ends.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private BossStatusEventChannelSO bossStatusChanged;

        [Header("Parts (fill and trail pivot X must be 0)")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text nameLabel;
        [SerializeField] private RectTransform fill;
        [SerializeField] private RectTransform trail;

        [Header("Feel")]
        [SerializeField, Min(0f)] private float fadeSpeed = 3f;
        [SerializeField, Min(0f)] private float trailDelay = 0.5f;
        [SerializeField, Min(0f)] private float trailSpeed = 0.5f;

        private float target = 1f;
        private float trailValue = 1f;
        private float trailTimer;
        private bool visible;

        private void Awake()
        {
            if (group != null) group.alpha = 0f;
        }

        private void OnEnable()
        {
            if (bossStatusChanged != null) bossStatusChanged.OnRaised += OnStatus;
        }

        private void OnDisable()
        {
            if (bossStatusChanged != null) bossStatusChanged.OnRaised -= OnStatus;
        }

        private void OnStatus(BossStatus status)
        {
            if (nameLabel != null) nameLabel.text = status.Name;

            float value = status.Normalized;
            if (!visible && status.Engaged)
            {
                // A new fight: start full, no trail.
                trailValue = value;
                trailTimer = 0f;
            }
            else if (target - value > 0.001f)
            {
                trailTimer = trailDelay;
            }

            target = value;
            visible = status.Engaged;
            SetWidth(fill, target);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (group != null) group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, fadeSpeed * dt);

            if (trailTimer > 0f) trailTimer -= dt;
            else trailValue = Mathf.MoveTowards(trailValue, target, trailSpeed * dt);
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
    }
}
