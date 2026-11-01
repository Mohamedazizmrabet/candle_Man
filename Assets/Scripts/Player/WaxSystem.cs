using UnityEngine;
using Wickfall.Core.Events;
using Wickfall.Data;

namespace Wickfall.Player
{
    /// <summary>
    /// The only owner of the wax value (wax = life = light = risk). Handles passive loss, damage, gains and death.
    /// Everything else reads <see cref="Current"/> or listens to the WaxChanged channel.
    /// </summary>
    public class WaxSystem : MonoBehaviour
    {
        [SerializeField] private PlayerConfigSO config;

        [Header("Broadcasts")]
        [SerializeField] private WaxEventChannelSO waxChanged;
        [SerializeField] private VoidEventChannelSO playerExtinguished;

        [Header("Listens to")]
        [SerializeField] private FloatEventChannelSO waxGainRequested;
        [SerializeField] private Vector3EventChannelSO brazierRested;
        [SerializeField] private VoidEventChannelSO playerRespawned;

        public float Current { get; private set; }
        public float Max => config.maxWax;
        public float Normalized => Max > 0f ? Current / Max : 0f;
        public bool IsExtinguished { get; private set; }

        /// <summary>Multiplier on passive loss. DimController sets it while the wick is trimmed.</summary>
        public float LossMultiplier { get; set; } = 1f;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"[Wickfall] WaxSystem on {name} has no PlayerConfig assigned.", this);
                enabled = false;
                return;
            }

            Current = Mathf.Clamp(config.startWax, 0f, Max);
        }

        private void OnEnable()
        {
            if (waxGainRequested != null) waxGainRequested.OnRaised += Gain;
            if (brazierRested != null) brazierRested.OnRaised += OnBrazierRested;
            if (playerRespawned != null) playerRespawned.OnRaised += Refill;
        }

        private void OnDisable()
        {
            if (waxGainRequested != null) waxGainRequested.OnRaised -= Gain;
            if (brazierRested != null) brazierRested.OnRaised -= OnBrazierRested;
            if (playerRespawned != null) playerRespawned.OnRaised -= Refill;
        }

        private void Start() => Broadcast();

        private void Update()
        {
            if (IsExtinguished) return;

            float loss = config.passiveLossPerSecond * LossMultiplier * Time.deltaTime;
            if (loss > 0f) SetWax(Current - loss);
        }

        /// <summary>Removes wax from a hit or hazard. Ignored once extinguished.</summary>
        public void TakeDamage(float amount)
        {
            if (IsExtinguished || amount <= 0f) return;
            SetWax(Current - amount);
        }

        /// <summary>Pays a wax cost (attacks). Refuses a cost that would put the flame out.</summary>
        public bool TrySpend(float amount)
        {
            if (IsExtinguished || amount >= Current) return false;
            SetWax(Current - amount);
            return true;
        }

        public void Gain(float amount)
        {
            if (IsExtinguished || amount <= 0f) return;
            SetWax(Current + amount);
        }

        /// <summary>Back to max wax and relit (brazier rest or respawn).</summary>
        public void Refill()
        {
            IsExtinguished = false;
            SetWax(Max);
        }

        private void OnBrazierRested(Vector3 _) => Refill();

        private void SetWax(float value)
        {
            Current = Mathf.Clamp(value, 0f, Max);
            Broadcast();

            if (Current <= 0f && !IsExtinguished)
            {
                IsExtinguished = true;
                if (playerExtinguished != null) playerExtinguished.Raise();
            }
        }

        private void Broadcast()
        {
            if (waxChanged != null) waxChanged.Raise(new WaxChange(Current, Max));
        }
    }
}
