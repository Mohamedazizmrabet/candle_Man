using UnityEngine;
using Wickfall.Combat;
using Wickfall.Data;
using Wickfall.World;

namespace Wickfall.Enemies
{
    /// <summary>
    /// Spawns a WaxPickup worth waxDropMin..waxDropMax (4 to 15) when the Damageable on this object dies.
    /// More wax means more light, and more light means more enemies notice you.
    /// </summary>
    public class WaxDropper : MonoBehaviour
    {
        [SerializeField] private EnemyConfigSO config;
        [SerializeField] private Damageable damageable;
        [SerializeField] private WaxPickup pickupPrefab;
        [Tooltip("Speed of the little pop when the drop appears.")]
        [SerializeField, Min(0f)] private float scatterSpeed = 2.5f;

        private void Reset() => damageable = GetComponent<Damageable>();

        private void OnEnable()
        {
            if (damageable != null) damageable.Died += OnDied;
        }

        private void OnDisable()
        {
            if (damageable != null) damageable.Died -= OnDied;
        }

        private void OnDied(DamageInfo hit)
        {
            if (config == null || pickupPrefab == null)
            {
                Debug.LogWarning($"[Wickfall] WaxDropper on {name} needs a config and a pickup prefab.", this);
                return;
            }

            // Inclusive max: Random.Range with ints excludes the upper bound.
            int amount = Random.Range(config.waxDropMin, config.waxDropMax + 1);
            Vector2 away = ((Vector2)transform.position - hit.SourcePosition).normalized;
            if (away == Vector2.zero) away = Random.insideUnitCircle.normalized;

            var pickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
            pickup.Init(amount, away * scatterSpeed);
        }
    }
}
