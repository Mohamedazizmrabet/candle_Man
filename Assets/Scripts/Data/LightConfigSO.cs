using UnityEngine;

namespace Wickfall.Data
{
    /// <summary>How wax turns into light (design doc 2.1: radius 1.2 to 6 units).</summary>
    [CreateAssetMenu(menuName = "Wickfall/Config/Light Config", fileName = "LightConfig")]
    public class LightConfigSO : ScriptableObject
    {
        [Header("Radius (units)")]
        [Tooltip("Light radius at 0 wax.")]
        [Min(0f)] public float minRadius = 1.2f;
        [Tooltip("Light radius at max wax.")]
        [Min(0f)] public float maxRadius = 6f;
        [Tooltip("Inner (full brightness) radius as a fraction of the outer radius.")]
        [Range(0f, 1f)] public float innerRadiusRatio = 0.25f;
        [Tooltip("Seconds to reach a new radius (hits, trimming the wick). 0 = instant.")]
        [Min(0f)] public float radiusSmoothTime = 0.15f;

        [Header("Intensity")]
        [Min(0f)] public float intensity = 1f;
        public Color color = new Color(1f, 0.78f, 0.45f);

        [Header("Flicker")]
        [Tooltip("Random radius change, as a fraction of the radius (0.05 = 5%).")]
        [Range(0f, 0.5f)] public float flickerAmount = 0.05f;
        [Min(0f)] public float flickerSpeed = 8f;

        [Header("Flame sprite scale")]
        [Tooltip("Scale of the flame child object at 0 wax.")]
        [Min(0f)] public float minFlameScale = 0.5f;
        [Tooltip("Scale of the flame child object at max wax.")]
        [Min(0f)] public float maxFlameScale = 1.2f;

        /// <summary>Radius for a wax value between 0 and 1, before dim and flicker.</summary>
        public float RadiusFor(float normalizedWax) => Mathf.Lerp(minRadius, maxRadius, Mathf.Clamp01(normalizedWax));
    }
}
