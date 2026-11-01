using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Wickfall.Core
{
    /// <summary>
    /// Wraps the "Gameplay" map of Wickfall_Input so the player never touches the Input System directly.
    /// Keys are bound by physical position, so WASD positions are Z Q S D on an AZERTY keyboard.
    /// </summary>
    [CreateAssetMenu(menuName = "Wickfall/Input/Input Reader", fileName = "InputReader")]
    public class InputReaderSO : ScriptableObject
    {
        [SerializeField] private InputActionAsset inputActions;

        public event Action AttackPressed;
        public event Action DashPressed;
        public event Action InteractPressed;

        private InputActionMap gameplay;
        private InputAction move;
        private InputAction dim;
        private InputAction attack;
        private InputAction dash;
        private InputAction interact;

        public Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;
        public bool DimHeld => dim != null && dim.IsPressed();

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

            attack.performed += OnAttack;
            dash.performed += OnDash;
            interact.performed += OnInteract;
        }

        private void Unbind()
        {
            if (gameplay == null) return;

            attack.performed -= OnAttack;
            dash.performed -= OnDash;
            interact.performed -= OnInteract;
            gameplay.Disable();
            gameplay = null;
        }

        private void OnAttack(InputAction.CallbackContext _) => AttackPressed?.Invoke();
        private void OnDash(InputAction.CallbackContext _) => DashPressed?.Invoke();
        private void OnInteract(InputAction.CallbackContext _) => InteractPressed?.Invoke();

        private void OnDisable() => Unbind();
    }
}
