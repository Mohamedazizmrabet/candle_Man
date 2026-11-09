using UnityEngine;
using Wickfall.Combat;
using Wickfall.Core;
using Wickfall.Core.Events;
using Wickfall.Data;

namespace Wickfall.Player
{
    /// <summary>
    /// Candleman's state machine: idle, walk, dim, attack, dash, hurt, extinguished.
    /// Owns movement, facing, cooldowns, invulnerability and respawning. States live in Player/States.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // How long an attack or dash press is remembered, so presses during another action are not lost.
        private const float InputBufferTime = 0.15f;

        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimMoveX = Animator.StringToHash("MoveX");
        private static readonly int AnimMoveY = Animator.StringToHash("MoveY");

        [SerializeField] private PlayerConfigSO config;
        [SerializeField] private InputReaderSO input;
        [SerializeField] private PlayerRuntimeSO runtime;

        [Header("Same object")]
        [SerializeField] private WaxSystem waxSystem;
        [SerializeField] private DimController dimController;

        [Header("Visual child")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer bodyRenderer;

        [Header("Attack")]
        [Tooltip("Child that rotates so its +X axis points where Candleman faces. The hitbox sits on +X.")]
        [SerializeField] private Transform attackPivot;
        [Tooltip("Turned on during the active part of the attack.")]
        [SerializeField] private Hitbox attackHitbox;

        [Header("Broadcasts")]
        [SerializeField] private VoidEventChannelSO playerRespawned;

        [Header("Listens to")]
        [SerializeField] private VoidEventChannelSO playerExtinguished;
        [SerializeField] private Vector3EventChannelSO brazierRested;

        [Header("Debug")]
        [SerializeField] private bool showStateLabel = true;

        private readonly StateMachine machine = new StateMachine();
        private Rigidbody2D body;
        private Vector3 respawnPoint;
        private float attackBuffer;
        private float dashBuffer;
        private float attackCooldown;
        private float dashCooldown;
        private float invulnerableTimer;

        public PlayerIdleState Idle { get; private set; }
        public PlayerWalkState Walk { get; private set; }
        public PlayerDimState Dim { get; private set; }
        public PlayerAttackState Attack { get; private set; }
        public PlayerDashState Dash { get; private set; }
        public PlayerHurtState Hurt { get; private set; }
        public PlayerExtinguishedState Extinguished { get; private set; }

        public PlayerConfigSO Config => config;
        public WaxSystem Wax => waxSystem;
        public DimController DimControl => dimController;

        /// <summary>Movement input snapped to 4 directions (zero when no input).</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>Last direction faced, always one of up, down, left, right.</summary>
        public Vector2 Facing { get; private set; } = Vector2.down;

        public bool IsDimmed => dimController != null && dimController.IsDimmed;
        public bool IsInvulnerable => invulnerableTimer > 0f || machine.Current == Dash || machine.Current == Extinguished;

        /// <summary>Damage of one swing right now (halved while the wick is trimmed).</summary>
        public float AttackDamage => config.attackDamage * (dimController != null ? dimController.DamageMultiplier : 1f);

        public string CurrentStateName => machine.CurrentName;

        /// <summary>Direction of the current (or last) swing, set when it starts.</summary>
        public Vector2 AttackDirection { get; private set; } = Vector2.down;

        /// <summary>0 to 1 through the current swing, or -1 when not attacking. Drives the sword animation.</summary>
        public float AttackProgress => machine.Current == Attack ? Attack.Progress : -1f;

        private void Reset()
        {
            waxSystem = GetComponent<WaxSystem>();
            dimController = GetComponent<DimController>();
            animator = GetComponentInChildren<Animator>();
            bodyRenderer = animator != null ? animator.GetComponent<SpriteRenderer>() : null;
        }

        private void Awake()
        {
            if (config == null || input == null || waxSystem == null)
            {
                Debug.LogError($"[Wickfall] PlayerController on {name} needs a PlayerConfig, an InputReader and a WaxSystem.", this);
                enabled = false;
                return;
            }

            body = GetComponent<Rigidbody2D>();
            respawnPoint = transform.position;

            Idle = new PlayerIdleState(this);
            Walk = new PlayerWalkState(this);
            Dim = new PlayerDimState(this);
            Attack = new PlayerAttackState(this);
            Dash = new PlayerDashState(this);
            Hurt = new PlayerHurtState(this);
            Extinguished = new PlayerExtinguishedState(this);

            if (runtime != null)
            {
                runtime.ResetValues();
                runtime.IsRegistered = true;
                runtime.IsAlive = true;
                runtime.Position = body.position;
            }

            SetAttackHitbox(false);
        }

        private void OnEnable()
        {
            input.EnableGameplay();
            input.AttackPressed += OnAttackPressed;
            input.DashPressed += OnDashPressed;
            if (playerExtinguished != null) playerExtinguished.OnRaised += OnExtinguished;
            if (brazierRested != null) brazierRested.OnRaised += OnBrazierRested;
        }

        private void OnDisable()
        {
            input.AttackPressed -= OnAttackPressed;
            input.DashPressed -= OnDashPressed;
            if (playerExtinguished != null) playerExtinguished.OnRaised -= OnExtinguished;
            if (brazierRested != null) brazierRested.OnRaised -= OnBrazierRested;
            if (runtime != null) runtime.IsRegistered = false;
        }

        private void Start() => machine.ChangeState(Idle);

        private void Update()
        {
            float dt = Time.deltaTime;
            attackBuffer -= dt;
            dashBuffer -= dt;
            attackCooldown -= dt;
            dashCooldown -= dt;
            invulnerableTimer -= dt;

            MoveInput = SnapToFourDirections(input.Move);
            if (MoveInput != Vector2.zero) Facing = MoveInput;

            machine.Tick(dt);
            UpdateAnimator();
            UpdateBlink();
        }

        private void FixedUpdate() => machine.FixedTick(Time.fixedDeltaTime);

        private void LateUpdate()
        {
            if (runtime != null) runtime.Position = body.position;
        }

        // ---------- Called by states ----------

        public void ChangeState(IState next) => machine.ChangeState(next);

        public void SetVelocity(Vector2 velocity) => body.linearVelocity = velocity;

        public void Stop() => body.linearVelocity = Vector2.zero;

        /// <summary>Goes back to idle, walk or dim depending on input and the Dim key.</summary>
        public void ReturnToLocomotion()
        {
            if (IsDimmed) ChangeState(Dim);
            else if (MoveInput != Vector2.zero) ChangeState(Walk);
            else ChangeState(Idle);
        }

        /// <summary>Starts a buffered dash or attack if allowed. Returns true if the state changed.</summary>
        public bool TryStartAction()
        {
            if (dashBuffer > 0f && dashCooldown <= 0f)
            {
                dashBuffer = 0f;
                ChangeState(Dash);
                return true;
            }

            if (attackBuffer > 0f && attackCooldown <= 0f)
            {
                attackBuffer = 0f;
                // No attack if its wax cost would put the flame out.
                if (waxSystem.TrySpend(config.attackWaxCost))
                {
                    ChangeState(Attack);
                    return true;
                }
            }

            return false;
        }

        public void StartAttackCooldown() => attackCooldown = config.attackCooldown;

        public void StartDashCooldown() => dashCooldown = config.dashCooldown;

        public void SetCanDim(bool canDim)
        {
            if (dimController != null) dimController.CanDim = canDim;
        }

        /// <summary>Turns the attack pivot toward the facing direction. Called when a swing starts.</summary>
        public void AimAttack()
        {
            AttackDirection = Facing;
            if (attackPivot == null) return;
            float angle = Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg;
            attackPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetAttackHitbox(bool active)
        {
            if (attackHitbox == null) return;

            var go = attackHitbox.gameObject;
            if (go.activeSelf == active) return;

            if (active) attackHitbox.Damage = AttackDamage;
            go.SetActive(active);
        }

        public void SetAlive(bool alive)
        {
            if (runtime != null) runtime.IsAlive = alive;
        }

        /// <summary>Placeholder until the death animation exists: greys out the body.</summary>
        public void SetBodyTint(Color tint)
        {
            if (bodyRenderer != null) bodyRenderer.color = tint;
        }

        /// <summary>Moves to the last brazier (or the start point) and tells WaxSystem to relight.</summary>
        public void Respawn()
        {
            body.position = respawnPoint;
            transform.position = respawnPoint;
            Stop();
            invulnerableTimer = config.invulnerabilityDuration;
            if (playerRespawned != null) playerRespawned.Raise();
            ChangeState(Idle);
        }

        // ---------- Called by combat (PlayerHurtbox) ----------

        /// <summary>
        /// A hit from an enemy or hazard. Removes wax, knocks back and starts invulnerability.
        /// Returns false if the hit was ignored (dashing, invulnerable or extinguished).
        /// </summary>
        public bool ApplyHit(float waxDamage, Vector2 sourcePosition)
        {
            if (IsInvulnerable) return false;

            waxSystem.TakeDamage(waxDamage);
            if (waxSystem.IsExtinguished) return true; // PlayerExtinguished already switched the state.

            invulnerableTimer = config.invulnerabilityDuration;
            Hurt.SetSource(sourcePosition);
            ChangeState(Hurt);
            return true;
        }

        // ---------- Events ----------

        private void OnAttackPressed() => attackBuffer = InputBufferTime;

        private void OnDashPressed() => dashBuffer = InputBufferTime;

        private void OnExtinguished() => ChangeState(Extinguished);

        private void OnBrazierRested(Vector3 point) => respawnPoint = point;

        // ---------- Helpers ----------

        private static Vector2 SnapToFourDirections(Vector2 raw)
        {
            if (raw.sqrMagnitude < 0.04f) return Vector2.zero; // dead zone for sticks
            return Mathf.Abs(raw.x) > Mathf.Abs(raw.y)
                ? new Vector2(Mathf.Sign(raw.x), 0f)
                : new Vector2(0f, Mathf.Sign(raw.y));
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;
            animator.SetBool(AnimIsMoving, body.linearVelocity.sqrMagnitude > 0.01f && machine.Current != Hurt);
            animator.SetFloat(AnimMoveX, Facing.x);
            animator.SetFloat(AnimMoveY, Facing.y);
        }

        private void UpdateBlink()
        {
            if (bodyRenderer == null) return;
            bool blinking = invulnerableTimer > 0f && machine.Current != Extinguished;
            bodyRenderer.enabled = !blinking || Mathf.Repeat(Time.time * 12f, 1f) > 0.5f;
        }

        private void OnGUI()
        {
            if (!showStateLabel) return;
            GUI.Label(new Rect(12f, 40f, 300f, 20f), $"State: {CurrentStateName}  Wax: {waxSystem.Current:0.0}");
        }
    }
}
