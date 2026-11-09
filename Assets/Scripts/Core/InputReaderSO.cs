using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Wickfall.Core
{
    /// <summary>
    /// Wraps the "Gameplay" map of Wickfall_Input so the player never touches the Input System directly.
    /// Keys are bound by physical position, so WASD positions are Z Q S D on an AZERTY keyboard.
    /// While <see cref="Paused"/> is set, gameplay input reads as nothing; only Pause still comes through.
    /// </summary>
    [CreateAssetMenu(menuName = "Wickfall/Input/Input Reader", fileName = "InputReader")]
    public class InputReaderSO : ScriptableObject
    {
        [SerializeField] private InputActionAsset inputActions;

        public event Action AttackPressed;
        public event Action DashPressed;
        public event Action InteractPressed;
        public event Action PausePressed;

        private InputActionMap gameplay;
        private InputAction move;
        private InputAction dim;
        private InputAction attack;
        private InputAction dash;
        private InputAction interact;
        private InputAction pause;

        /// <summary>Set by the pause menu. Movement, attacks, dash, dim and interact are ignored meanwhile.</summary>
        [NonSerialized] public bool Paused;

        public Vector2 Move => move != null && !Paused ? move.ReadValue<Vector2>() : Vector2.zero;
        public bool DimHeld => dim != null && !Paused && dim.IsPressed();

        public void EnableGameplay()
        {
            if (gameplay == null) Bind();
            gameplay.Enable();
        }

        public void DisableGameplay() => gameplay?.Disable();

        private void Bind()
        {
            gameplay = inputActions.FindActionMap("Gameplay", throwIfNotFound: true);
            move = gameplay.FindAction("Move", throwIfNotFound: true);
            dim = gameplay.FindAction("Dim", throwIfNotFound: true);
            attack = gameplay.FindAction("Attack", throwIfNotFound: true);
            dash = gameplay.FindAction("Dash", throwIfNotFound: true);
            interact = gameplay.FindAction("Interact", throwIfNotFound: true);
            pause = gameplay.FindAction("Pause", throwIfNotFound: false);

            attack.performed += OnAttack;
            dash.performed += OnDash;
            interact.performed += OnInteract;
            if (pause != null) pause.performed += OnPause;
        }

        private void Unbind()
        {
            if (gameplay == null) return;

            attack.performed -= OnAttack;
            dash.performed -= OnDash;
            interact.performed -= OnInteract;
            if (pause != null) pause.performed -= OnPause;
            gameplay.Disable();
            gameplay = null;
        }

        private void OnAttack(InputAction.CallbackContext _)
        {
            if (!Paused) AttackPressed?.Invoke();
        }

        private void OnDash(InputAction.CallbackContext _)
        {
            if (!Paused) DashPressed?.Invoke();
        }

        private void OnInteract(InputAction.CallbackContext _)
        {
            if (!Paused) InteractPressed?.Invoke();
        }

        private void OnPause(InputAction.CallbackContext _) => PausePressed?.Invoke();

        private void OnDisable()
        {
            Paused = false;
            Unbind();
        }
    }
}
