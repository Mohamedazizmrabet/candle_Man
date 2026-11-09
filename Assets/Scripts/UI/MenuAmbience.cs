using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wickfall.UI
{
    /// <summary>
    /// Brings the still menu picture to life: candle-light flicker on the background
    /// and embers drifting up from the bottom.
    /// </summary>
    public class MenuAmbience : MonoBehaviour
    {
        [Header("Firelight")]
        [SerializeField] private Graphic background;
        [SerializeField] private Color warmTint = new Color(1f, 0.93f, 0.85f);
        [Tooltip("How much the light dips (0.1 = up to 10% darker).")]
        [SerializeField, Range(0f, 0.3f)] private float flickerDepth = 0.08f;
        [SerializeField, Min(0f)] private float flickerSpeed = 3f;

        [Header("Embers")]
        [Tooltip("Layer the embers are drawn in (full-screen, behind the logo and menu).")]
        [SerializeField] private RectTransform emberLayer;
        [SerializeField, Min(0)] private int emberCount = 28;
        [SerializeField] private Vector2 emberSize = new Vector2(3f, 6f);
        [SerializeField] private Vector2 emberSpeed = new Vector2(18f, 45f);
        [SerializeField] private Vector2 emberLife = new Vector2(3f, 7f);
        [SerializeField] private Color emberColor = new Color(1f, 0.6f, 0.2f, 0.9f);

        private class Ember
        {
            public RectTransform Rect;
            public Image Image;
            public float Age;
            public float Life;
            public float Speed;
            public float Sway;
            public float Phase;
        }

        private readonly List<Ember> embers = new List<Ember>();

        private void Start()
        {
            if (emberLayer == null) return;

            for (int i = 0; i < emberCount; i++)
            {
                var go = new GameObject("Ember", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(emberLayer, false);
                var ember = new Ember { Rect = (RectTransform)go.transform, Image = go.GetComponent<Image>() };
                ember.Image.raycastTarget = false;
                Respawn(ember, startAnywhere: true);
                embers.Add(ember);
            }
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            if (background != null)
            {
                // Two noise speeds mixed: slow breathing plus quick candle twitches.
                float noise = Mathf.PerlinNoise(t * flickerSpeed, 0.3f) * 0.7f + Mathf.PerlinNoise(t * flickerSpeed * 3.1f, 7.7f) * 0.3f;
                float light = 1f - flickerDepth * noise;
                background.color = new Color(warmTint.r * light, warmTint.g * light, warmTint.b * light, 1f);
            }

            foreach (var ember in embers)
            {
                ember.Age += dt;
                if (ember.Age >= ember.Life)
                {
                    Respawn(ember, startAnywhere: false);
                    continue;
                }

                float x = Mathf.Sin(ember.Phase + ember.Age * 1.7f) * ember.Sway * dt;
                ember.Rect.anchoredPosition += new Vector2(x, ember.Speed * dt);

                // Fade in quickly, glow, fade out.
                float k = ember.Age / ember.Life;
                float alpha = Mathf.Min(k * 5f, 1f) * (1f - k);
                var c = emberColor;
                c.a *= alpha;
                ember.Image.color = c;
            }
        }

        private void Respawn(Ember ember, bool startAnywhere)
        {
            var area = emberLayer.rect;
            if (area.width <= 0f) area = new Rect(0f, 0f, 1376f, 768f); // layout not computed yet on the first frame
            float size = Random.Range(emberSize.x, emberSize.y);
            ember.Rect.anchorMin = ember.Rect.anchorMax = new Vector2(0f, 0f);
            ember.Rect.sizeDelta = new Vector2(size, size);
            float y = startAnywhere ? Random.Range(0f, area.height * 0.8f) : Random.Range(-10f, area.height * 0.15f);
            ember.Rect.anchoredPosition = new Vector2(Random.Range(0f, area.width), y);
            ember.Life = Random.Range(emberLife.x, emberLife.y);
            ember.Age = startAnywhere ? Random.Range(0f, ember.Life) : 0f;
            ember.Speed = Random.Range(emberSpeed.x, emberSpeed.y);
            ember.Sway = Random.Range(8f, 25f);
            ember.Phase = Random.Range(0f, Mathf.PI * 2f);
            ember.Image.color = new Color(0f, 0f, 0f, 0f);
        }
    }
}
