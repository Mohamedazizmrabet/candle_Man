using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Core;
using Wickfall.Core.Events;
using Wickfall.Data;

namespace Wickfall.Player
{
    /// <summary>
    /// Turns wax into the candle's Light2D radius, intensity and color, with a slight flicker,
    /// and scales the separate flame sprite. Writes the radius to PlayerRuntime for enemy perception.
    /// </summary>
    public class LightController : MonoBehaviour
    {
        [SerializeField] private LightConfigSO config;
        [SerializeField] private PlayerRuntimeSO runtime;

        [Header("Parts")]
        [Tooltip("Point Light 2D on its own child at the candle (not under Flame, so flame scaling never touches it).")]
        [SerializeField] private Light2D candleLight;
        [Tooltip("The flame sprite child. Scaled with wax, hidden when extinguished.")]
        [SerializeField] private Transform flame;

        [Header("Listens to")]
        [SerializeField] private WaxEventChannelSO waxChanged;

        /// <summary>Set by DimController while the wick is trimmed.</summary>
        public float RadiusMultiplier { get; set; } = 1f;

        /// <summary>Set by DimController while the wick is trimmed.</summary>
        public float FlameScaleMultiplier { get; set; } = 1f;

        /// <summary>Current radius in units, before flicker.</summary>
        public float CurrentRadius => radius;

        private float normalizedWax = 1f;
        private bool lit = true;
        private float radius;
        private float radiusVelocity;
        private float noiseSeed;

        private float TargetRadius => lit ? config.RadiusFor(normalizedWax) * RadiusMultiplier : 0f;

        private void Awake()
        {
            if (config == null || candleLight == null)
            {
                Debug.LogError($"[Wickfall] LightController on {name} needs a LightConfig and a Candle Light.", this);
                enabled = false;
                return;
            }

            candleLight.lightType = Light2D.LightType.Point;
            noiseSeed = Random.value * 100f;
            radius = TargetRadius;
        }

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
            normalizedWax = wax.Normalized;
            lit = wax.Current > 0f;
        }

        private void LateUpdate()
        {
            radius = config.radiusSmoothTime > 0f
                ? Mathf.SmoothDamp(radius, TargetRadius, ref radiusVelocity, config.radiusSmoothTime)
                : TargetRadius;

            if (runtime != null) runtime.LightRadius = radius;

            // Perlin noise in -1..1, so the flicker wanders instead of jittering every frame.
            float noise = Mathf.PerlinNoise(Time.time * config.flickerSpeed, noiseSeed) * 2f - 1f;
            float outer = radius * (1f + noise * config.flickerAmount);

            candleLight.enabled = outer > 0.01f;
            candleLight.pointLightOuterRadius = outer;
            candleLight.pointLightInnerRadius = outer * config.innerRadiusRatio;
            candleLight.intensity = config.intensity * (1f + noise * config.flickerAmount * 0.5f);
            candleLight.color = config.color;

            if (flame != null)
            {
                if (flame.gameObject.activeSelf != lit) flame.gameObject.SetActive(lit);

                float scale = Mathf.Lerp(config.minFlameScale, config.maxFlameScale, normalizedWax) * FlameScaleMultiplier;
                flame.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (candleLight == null) return;
            Gizmos.color = new Color(1f, 0.7f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(candleLight.transform.position, Application.isPlaying ? radius : config != null ? config.maxRadius : 0f);
        }
    }
}
