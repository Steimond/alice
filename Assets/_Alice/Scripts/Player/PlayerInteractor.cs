using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Interact used on the world: a door's latch, later levers and people (Alice.Core.IInteractable). Picking
    /// up the lost purse and sitting in a chair keep their own handling; this takes the press only when one of
    /// those is not possible here, and never while she is busy.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.2f)] private float _bufferTime = 0.12f;

        private readonly Collider2D[] _overlaps = new Collider2D[8];

        private PlayerMotor _motor;
        private PlayerSpoils _spoils;
        private PlayerRest _rest;
        private PlayerDamageReaction _reaction;
        private IPlayerCommandSource _commands;
        private ContactFilter2D _triggers;
        private float _consumedAt = float.NegativeInfinity;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _spoils = GetComponent<PlayerSpoils>();
            _rest = GetComponent<PlayerRest>();
            _reaction = GetComponent<PlayerDamageReaction>();
            _commands = GetComponent<IPlayerCommandSource>();
            _triggers = new ContactFilter2D { useTriggers = true };
        }

        private void FixedUpdate()
        {
            if (_commands == null
                || !InputBuffer.IsFresh(_commands.InteractPressedAt, _consumedAt, Time.time, _bufferTime))
            {
                return;
            }

            bool busy = !_motor.IsGrounded || _motor.IsLocked || _motor.IsSliding || _motor.RunOnly
                || (_reaction != null && _reaction.IsDefeated) || (_rest != null && _rest.IsResting)
                || (_spoils != null && _spoils.CanRecoverLoss);
            if (busy || !TryFind(out IInteractable target))
            {
                return;
            }

            _consumedAt = _commands.InteractPressedAt;
            target.Interact(gameObject);
        }

        private bool TryFind(out IInteractable target)
        {
            Bounds body = GetComponent<Collider2D>().bounds;
            int count = Physics2D.OverlapBox(body.center, body.size, 0f, _triggers, _overlaps);
            for (int index = 0; index < count; index++)
            {
                IInteractable candidate = _overlaps[index].GetComponentInParent<IInteractable>();
                if (candidate != null && candidate.CanInteract(_motor.Position))
                {
                    target = candidate;
                    return true;
                }
            }

            target = null;
            return false;
        }
    }
}
