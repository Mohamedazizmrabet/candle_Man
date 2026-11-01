using UnityEngine;
using Wickfall.Combat;
using Wickfall.Core;
using Wickfall.Data;

namespace Wickfall.Enemies
{
    /// <summary>
    /// Enemy state machine: wander, alert, chase, attack, return (plus hurt and dead).
    /// Generic melee behaviour driven by an EnemyConfig; phase 1 uses it for the Dripling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyConfigSO config;

        [Header("Same object")]
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private Damageable damageable;

        [Header("Attack")]
        [Tooltip("Child rotated toward the player when a strike starts. The hitbox sits on +X.")]
        [SerializeField] private Transform attackPivot;
        [SerializeField] private Hitbox attackHitbox;
        [Tooltip("How long the hitbox stays on once the wind-up ends.")]
        [SerializeField, Min(0.01f)] private float attackActiveTime = 0.15f;
        [Tooltip("Pause after a strike before chasing again.")]
        [SerializeField, Min(0f)] private float attackRecovery = 0.4f;

        [Header("Hurt and death")]
        [SerializeField, Min(0f)] private float hurtDuration = 0.2f;
        [SerializeField, Min(0f)] private float knockbackSpeed = 5f;
        [SerializeField, Min(0f)] private float deathFadeTime = 0.5f;

        [Header("Visual (placeholder telegraphs)")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Color alertTint = new Color(1f, 0.9f, 0.4f);
        [SerializeField] private Color windupTint = new Color(1f, 0.4f, 0.3f);

        [Header("Debug")]
        [SerializeField] private bool showStateLabel = true;

        private readonly StateMachine machine = new StateMachine();
        private Rigidbody2D body;
        private Color baseColor = Color.white;
        private float nextAttackTime;

        public EnemyWanderState Wander { get; private set; }
        public EnemyAlertState Alert { get; private set; }
        public EnemyChaseState Chase { get; private set; }
        public EnemyAttackState Attack { get; private set; }
        public EnemyReturnState Return { get; private set; }
        public EnemyHurtState Hurt { get; private set; }
        public EnemyDeadState Dead { get; private set; }

        public EnemyConfigSO Config => config;
        public EnemyPerception Perception => perception;
        public Vector2 Home { get; private set; }
        public Vector2 Position => body.position;
        public float AttackActiveTime => attackActiveTime;
        public float AttackRecovery => attackRecovery;
        public float HurtDuration => hurtDuration;
        public float KnockbackSpeed => knockbackSpeed;
        public float DeathFadeTime => deathFadeTime;
        public bool AttackReady => Time.time >= nextAttackTime;
        public float DistanceFromHome => Vector2.Distance(body.position, Home);

        private void Reset()
        {
            perception = GetComponent<EnemyPerception>();
            damageable = GetComponent<Damageable>();
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            if (config == null || perception == null || damageable == null)
            {
                Debug.LogError($"[Wickfall] EnemyBrain on {name} needs an EnemyConfig, an EnemyPerception and a Damageable.", this);
                enabled = false;
                return;
            }

            body = GetComponent<Rigidbody2D>();
            Home = body.position;
            if (bodyRenderer != null) baseColor = bodyRenderer.color;
            damageable.SetMaxHealth(config.maxHealth);

            Wander = new EnemyWanderState(this);
            Alert = new EnemyAlertState(this);
            Chase = new EnemyChaseState(this);
            Attack = new EnemyAttackState(this);
            Return = new EnemyReturnState(this);
            Hurt = new EnemyHurtState(this);
            Dead = new EnemyDeadState(this);

            SetAttackHitbox(false);
        }

        private void OnEnable()
        {
            if (damageable == null) return;
            damageable.Damaged += OnDamaged;
            damageable.Died += OnDied;
        }

        private void OnDisable()
        {
            if (damageable == null) return;
            damageable.Damaged -= OnDamaged;
            damageable.Died -= OnDied;
        }

        private void Start() => machine.ChangeState(Wander);

        private void Update()
        {
            if (machine.Current != Dead) perception.Sense(Time.deltaTime);
            machine.Tick(Time.deltaTime);
        }

        private void FixedUpdate() => machine.FixedTick(Time.fixedDeltaTime);

        // ---------- Called by states ----------

        public void ChangeState(IState next) => machine.ChangeState(next);

        public void SetVelocity(Vector2 velocity) => body.linearVelocity = velocity;

        public void Stop() => body.linearVelocity = Vector2.zero;

        /// <summary>Moves toward a point. Returns true once there (and stops).</summary>
        public bool MoveTowards(Vector2 target, float speed)
        {
            Vector2 toTarget = target - body.position;
            if (toTarget.sqrMagnitude < 0.05f * 0.05f)
            {
                Stop();
                return true;
            }

            body.linearVelocity = toTarget.normalized * speed;
            return false;
        }

        public void AimAt(Vector2 target)
        {
            if (attackPivot == null) return;
            Vector2 dir = target - body.position;
            attackPivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        public void SetAttackHitbox(bool active)
        {
            if (attackHitbox == null) return;

            var go = attackHitbox.gameObject;
            if (go.activeSelf == active) return;

            if (active) attackHitbox.Damage = config.attackDamage;
            go.SetActive(active);
        }

        public void StartAttackCooldown() => nextAttackTime = Time.time + config.attackCooldown;

        public void SetTint(Color tint)
        {
            if (bodyRenderer != null) bodyRenderer.color = tint;
        }

        public void ShowAlertTint() => SetTint(alertTint);
        public void ShowWindupTint() => SetTint(windupTint);
        public void ClearTint() => SetTint(baseColor);

        /// <summary>Dead enemies stop colliding and stop being simulated while they fade.</summary>
        public void DisableBody()
        {
            Stop();
            SetAttackHitbox(false);
            foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;
            body.simulated = false;
        }

        // ---------- Events ----------

        private void OnDamaged(DamageInfo hit)
        {
            if (damageable.IsDead) return;
            Hurt.SetSource(hit.SourcePosition);
            ChangeState(Hurt);
        }

        private void OnDied(DamageInfo hit) => ChangeState(Dead);

        private void OnGUI()
        {
            if (!showStateLabel || Camera.main == null) return;

            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.2f);
            if (screen.z < 0f) return;
            GUI.Label(new Rect(screen.x - 40f, Screen.height - screen.y - 10f, 120f, 20f), machine.CurrentName.Replace("Enemy", "").Replace("State", ""));
        }
    }
}
