using System.Collections;
using UnityEngine;
using Wickfall.Combat;
using Wickfall.Core;

namespace Wickfall.World
{
    /// <summary>
    /// A chest opened with Interact (E). A normal chest spills wax drops. A mimic bites instead: it opens its
    /// mouth, its bite hitbox hurts the player, and it keeps biting while he stays close, until it is killed
    /// (then it spills more wax than a normal chest). Mimics can also be hit before being opened.
    /// </summary>
    public class Chest : MonoBehaviour
    {
        [SerializeField] private PlayerRuntimeSO runtime;
        [SerializeField] private InputReaderSO input;
        [SerializeField] private bool isMimic;

        [Header("Look")]
        [Tooltip("3 frames: closed, opening, open.")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform prompt;

        [Header("Opening")]
        [SerializeField, Min(0f)] private float interactRadius = 2f;
        [SerializeField, Min(0.01f)] private float frameTime = 0.08f;

        [Header("Loot")]
        [SerializeField] private WaxPickup pickupPrefab;
        [SerializeField, Min(0)] private int drops = 3;
        [SerializeField] private Vector2Int waxPerDrop = new Vector2Int(5, 10);

        [Header("Mimic only")]
        [SerializeField] private Hitbox bite;
        [SerializeField] private Damageable damageable;
        [SerializeField, Min(0f)] private float biteInterval = 1.1f;
        [SerializeField, Min(0.01f)] private float biteActiveTime = 0.15f;

        private bool opened;
        private bool playerInRange;
        private Vector3 promptBase;
        private float nextBite;

        private void Awake()
        {
            if (prompt != null)
            {
                promptBase = prompt.localPosition;
                prompt.gameObject.SetActive(false);
            }
            if (bite != null) bite.gameObject.SetActive(false);
            if (frames != null && frames.Length > 0 && spriteRenderer != null) spriteRenderer.sprite = frames[0];
        }

        private void OnEnable()
        {
            if (input != null) input.InteractPressed += OnInteract;
            if (damageable != null) damageable.Died += OnKilled;
        }

        private void OnDisable()
        {
            if (input != null) input.InteractPressed -= OnInteract;
            if (damageable != null) damageable.Died -= OnKilled;
        }

        private void Update()
        {
            playerInRange = runtime != null && runtime.IsRegistered && runtime.IsAlive
                            && Vector2.Distance(runtime.Position, transform.position) <= interactRadius;

            if (prompt != null)
            {
                bool show = playerInRange && !opened;
                if (prompt.gameObject.activeSelf != show) prompt.gameObject.SetActive(show);
                prompt.localPosition = promptBase + Vector3.up * (Mathf.Sin(Time.time * 4f) * 0.08f);
            }

            // An open mimic keeps snapping at a player who stays close.
            if (isMimic && opened && playerInRange && Time.time >= nextBite) StartCoroutine(Bite());
        }

        private void OnInteract()
        {
            if (!playerInRange || opened) return;
            opened = true;
            StartCoroutine(Open());
        }

        private IEnumerator Open()
        {
            for (int i = 1; i < frames.Length; i++)
            {
                spriteRenderer.sprite = frames[i];
                yield return new WaitForSeconds(frameTime);
            }

            if (isMimic) StartCoroutine(Bite());
            else SpillWax(drops);
        }

        private IEnumerator Bite()
        {
            nextBite = Time.time + biteInterval;
            if (bite == null) yield break;
            bite.gameObject.SetActive(true);
            yield return new WaitForSeconds(biteActiveTime);
            bite.gameObject.SetActive(false);
        }

        private void OnKilled(DamageInfo hit)
        {
            SpillWax(drops * 2);
            Destroy(gameObject);
        }

        private void SpillWax(int count)
        {
            if (pickupPrefab == null) return;
            for (int i = 0; i < count; i++)
            {
                var direction = Quaternion.Euler(0f, 0f, Random.Range(-160f, -20f)) * Vector3.right; // toward the front
                var pickup = Instantiate(pickupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                pickup.Init(Random.Range(waxPerDrop.x, waxPerDrop.y + 1), direction * Random.Range(2f, 3.5f));
            }
        }
    }
}
