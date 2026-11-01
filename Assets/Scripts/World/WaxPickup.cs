using UnityEngine;
using Wickfall.Core;
using Wickfall.Core.Events;

namespace Wickfall.World
{
    /// <summary>
    /// A drop of wax on the ground. Drifts to the player when close, and on touch raises WaxGainRequested
    /// (WaxSystem listens). It never references the player: position comes from PlayerRuntime.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WaxPickup : MonoBehaviour
    {
        [SerializeField] private PlayerRuntimeSO runtime;

        [Header("Broadcasts")]
        [SerializeField] private FloatEventChannelSO waxGainRequested;

        [Header("Amount")]
        [Tooltip("Used when placed by hand. WaxDropper overrides it.")]
        [SerializeField, Min(0f)] private float amount = 8f;
        [Tooltip("Visual scale for the smallest (4) and largest (15) drop.")]
        [SerializeField] private Vector2 scaleRange = new Vector2(0.35f, 0.7f);

        [Header("Feel")]
        [Tooltip("Can't be collected right after spawning, so drops visibly pop out first.")]
        [SerializeField, Min(0f)] private float pickupDelay = 0.3f;
        [Tooltip("Starts drifting to the player within this distance. 0 = off.")]
        [SerializeField, Min(0f)] private float magnetRadius = 2f;
        [SerializeField, Min(0f)] private float magnetSpeed = 6f;
        [SerializeField, Min(0f)] private float bobHeight = 0.08f;
        [SerializeField, Min(0f)] private float bobSpeed = 3f;
        [Tooltip("Child holding the sprite (bobs and scales). Empty = this object is only scaled.")]
        [SerializeField] private Transform visual;

        private float age;
        private float bobOffset;
        private Vector3 visualBase;
        private Vector2 scatterVelocity;

        public float Amount => amount;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void Awake()
        {
            bobOffset = Random.value * Mathf.PI * 2f;
            if (visual != null) visualBase = visual.localPosition;
            ApplyScale();
        }

        /// <summary>Called by WaxDropper right after spawning.</summary>
        public void Init(float waxAmount, Vector2 scatter)
        {
            amount = waxAmount;
            scatterVelocity = scatter;
            ApplyScale();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            age += dt;

            // Short pop away from the body, slowing to a stop.
            if (scatterVelocity.sqrMagnitude > 0.0001f)
            {
                transform.position += (Vector3)(scatterVelocity * dt);
                scatterVelocity = Vector2.MoveTowards(scatterVelocity, Vector2.zero, 12f * dt);
            }

            if (age >= pickupDelay && PlayerCanCollect())
            {
                Vector2 toPlayer = runtime.Position - (Vector2)transform.position;
                if (magnetRadius > 0f && toPlayer.sqrMagnitude <= magnetRadius * magnetRadius)
                {
                    transform.position = Vector2.MoveTowards(transform.position, runtime.Position, magnetSpeed * dt);
                }
            }

            if (visual != null)
            {
                visual.localPosition = visualBase + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight);
            }
        }

        private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);

        private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        private void TryCollect(Collider2D other)
        {
            if (age < pickupDelay || other.isTrigger || !other.CompareTag("Player")) return;
            // WaxSystem ignores gains while extinguished, so keep the drop for later.
            if (!PlayerCanCollect()) return;

            if (waxGainRequested != null) waxGainRequested.Raise(amount);
            Destroy(gameObject);
        }

        private bool PlayerCanCollect() => runtime == null || (runtime.IsRegistered && runtime.IsAlive);

        private void ApplyScale()
        {
            float t = Mathf.InverseLerp(4f, 15f, amount);
            float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, t);
            var target = visual != null ? visual : transform;
            target.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
