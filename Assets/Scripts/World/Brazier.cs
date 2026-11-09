using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Core;
using Wickfall.Core.Events;

namespace Wickfall.World
{
    /// <summary>
    /// Checkpoint (design doc 2.4). Stand next to it and press Interact (E): it lights up, refills the wax
    /// and becomes the respawn point. Resting again refills again. Talks to the player only through
    /// PlayerRuntime (where he is) and the BrazierRested channel (WaxSystem and PlayerController listen).
    /// </summary>
    public class Brazier : MonoBehaviour
    {
        private static readonly int LitHash = Animator.StringToHash("Lit");

        [SerializeField] private PlayerRuntimeSO runtime;
        [SerializeField] private InputReaderSO input;

        [Header("Broadcasts")]
        [SerializeField] private Vector3EventChannelSO brazierRested;

        [Header("Rest")]
        [Tooltip("How close (from the player's feet) to be able to rest.")]
        [SerializeField, Min(0f)] private float interactRadius = 2.2f;
        [Tooltip("Where the player reappears, relative to the brazier (in front of it).")]
        [SerializeField] private Vector2 respawnOffset = new Vector2(0f, -1.5f);

        [Header("Parts")]
        [SerializeField] private Animator animator;
        [SerializeField] private Light2D glow;
        [Tooltip("The floating E shown when the player is close enough.")]
        [SerializeField] private Transform prompt;

        [Header("Glow when lit")]
        [SerializeField, Min(0f)] private float glowIntensity = 1.1f;
        [SerializeField, Min(0f)] private float glowFlicker = 0.12f;

        private bool lit;
        private bool playerInRange;
        private Vector3 promptBase;
        private float seed;

        public bool IsLit => lit;

        private void Awake()
        {
            seed = Random.value * 100f;
            if (prompt != null)
            {
                promptBase = prompt.localPosition;
                prompt.gameObject.SetActive(false);
            }
            if (glow != null) glow.intensity = 0f;
        }

        private void OnEnable()
        {
            if (input != null) input.InteractPressed += OnInteract;
        }

        private void OnDisable()
        {
            if (input != null) input.InteractPressed -= OnInteract;
        }

        private void Update()
        {
            playerInRange = runtime != null && runtime.IsRegistered && runtime.IsAlive
                            && Vector2.Distance(runtime.Position, transform.position) <= interactRadius;

            if (prompt != null)
            {
                if (prompt.gameObject.activeSelf != playerInRange) prompt.gameObject.SetActive(playerInRange);
                prompt.localPosition = promptBase + Vector3.up * (Mathf.Sin(Time.time * 4f) * 0.08f);
            }

            if (glow != null && lit)
            {
                float noise = Mathf.PerlinNoise(Time.time * 6f, seed) * 2f - 1f;
                glow.intensity = glowIntensity * (1f + noise * glowFlicker);
            }
        }

        private void OnInteract()
        {
            if (playerInRange) Rest();
        }

        /// <summary>Lights the brazier (if needed), refills wax and saves this as the respawn point.</summary>
        public void Rest()
        {
            if (!lit)
            {
                lit = true;
                if (animator != null) animator.SetBool(LitHash, true);
            }

            if (brazierRested != null) brazierRested.Raise(transform.position + (Vector3)respawnOffset);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
            Gizmos.DrawWireCube(transform.position + (Vector3)respawnOffset, Vector3.one * 0.4f);
        }
    }
}
