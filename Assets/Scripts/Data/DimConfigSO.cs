using UnityEngine;

namespace Wickfall.Data
{
    /// <summary>
    /// "Trim the wick" multipliers (design doc 2.1): radius -65%, wax loss -75%,
    /// detection -60%, damage dealt -50%. A poverty mode, not invisibility.
    /// </summary>
    [CreateAssetMenu(menuName = "Wickfall/Config/Dim Config", fileName = "DimConfig")]
    public class DimConfigSO : ScriptableObject
    {
        [Range(0f, 1f)] public float radiusMultiplier = 0.35f;
        [Range(0f, 1f)] public float waxLossMultiplier = 0.25f;
        [Range(0f, 1f)] public float detectionMultiplier = 0.4f;
        [Range(0f, 1f)] public float damageMultiplier = 0.5f;

        [Tooltip("Not in the doc: sneaking is slower than walking.")]
        [Range(0f, 1f)] public float moveSpeedMultiplier = 0.5f;

        [Tooltip("Not in the doc: how small the flame sprite gets while trimmed.")]
        [Range(0f, 1f)] public float flameScaleMultiplier = 0.6f;
    }
}
