using UnityEngine;
using UnityEngine.InputSystem;

namespace Alice.Player
{
    /// <summary>Reads the project-wide Gameplay actions and exposes them as player commands.</summary>
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerCommandSource
    {
        private InputAction _move;
        private InputAction _jump;
        private InputAction _evade;

        public Vector2 Move { get; private set; }
        public bool JumpHeld { get; private set; }
        public float JumpPressedAt { get; private set; } = float.NegativeInfinity;
        public float EvadePressedAt { get; private set; } = float.NegativeInfinity;

        private void Awake()
        {
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError("Project-wide input actions are not assigned.", this);
                enabled = false;
                return;
            }

            _move = actions.FindAction("Gameplay/Move", true);
            _jump = actions.FindAction("Gameplay/Jump", true);
            _evade = actions.FindAction("Gameplay/Evade", true);
        }

        private void OnEnable()
        {
            _move?.actionMap.Enable();
        }

        private void Update()
        {
            Move = _move.ReadValue<Vector2>();
            JumpHeld = _jump.IsPressed();
            if (_jump.WasPressedThisFrame())
            {
                JumpPressedAt = Time.time;
            }

            if (_evade.WasPressedThisFrame())
            {
                EvadePressedAt = Time.time;
            }
        }
    }
}
