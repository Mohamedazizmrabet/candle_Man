using UnityEngine;
using Wickfall.Core;
using Wickfall.Data;

namespace Wickfall.Player
{
    /// <summary>
    /// "Trim the wick": while the Dim key is held, applies the DimConfig multipliers to wax loss and light,
    /// and publishes the detection multiplier for enemies. A poverty mode, not invisibility.
    /// </summary>
    public class DimController : MonoBehaviour
    {
        [SerializeField] private DimConfigSO config;
        [SerializeField] private InputReaderSO input;
        [SerializeField] private PlayerRuntimeSO runtime;

        [Header("Same object")]
        [SerializeField] private WaxSystem waxSystem;
        [SerializeField] private LightController lightController;

        public bool IsDimmed { get; private set; }

        /// <summary>PlayerController turns this off in states that cannot trim the wick (dash, extinguished).</summary>
        public bool CanDim { get; set; } = true;

        public float DamageMultiplier => IsDimmed ? config.damageMultiplier : 1f;
        public float MoveSpeedMultiplier => IsDimmed ? config.moveSpeedMultiplier : 1f;

        private void Reset()
        {
            waxSystem = GetComponent<WaxSystem>();
            lightController = GetComponent<LightController>();
        }

        private void Awake()
        {
            if (config == null || input == null || waxSystem == null || lightController == null)
            {
                Debug.LogError($"[Wickfall] DimController on {name} is missing a reference (config, input, wax system or light controller).", this);
                enabled = false;
            }
        }

        private void OnEnable() => input?.EnableGameplay();

        private void Start() => Apply(false);

        private void Update()
        {
            bool wanted = CanDim && input.DimHeld && !waxSystem.IsExtinguished;
            if (wanted != IsDimmed) Apply(wanted);
        }

        private void OnDisable()
        {
            if (IsDimmed) Apply(false);
        }

        private void Apply(bool dimmed)
        {
            IsDimmed = dimmed;
            waxSystem.LossMultiplier = dimmed ? config.waxLossMultiplier : 1f;
            lightController.RadiusMultiplier = dimmed ? config.radiusMultiplier : 1f;
            lightController.FlameScaleMultiplier = dimmed ? config.flameScaleMultiplier : 1f;

            if (runtime != null)
            {
                runtime.IsDimmed = dimmed;
                runtime.DetectionMultiplier = dimmed ? config.detectionMultiplier : 1f;
            }
        }
    }
}
