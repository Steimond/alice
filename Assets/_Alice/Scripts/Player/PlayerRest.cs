using System;
using Alice.Combat;
using Alice.Core;
using Alice.Save;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Resting in an armchair (GDD section 4). Interact inside a rest spot sits Alice down; once she is
    /// seated her clarity and salts come back, ordinary enemies return to their posts, the chair becomes the
    /// place she wakes after a defeat and the game saves. She may then change her technique, and gets up on
    /// Jump, Evade or Interact. A hit throws her out of the chair, and before she is seated cancels the rest.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor), typeof(Health))]
    public sealed class PlayerRest : MonoBehaviour, ISaveParticipant
    {
        public enum RestStage
        {
            None,
            Sitting,
            Resting,
            Rising
        }

        [SerializeField] private PlayerCombatSettings _settings;

        private readonly Collider2D[] _overlaps = new Collider2D[8];

        private PlayerMotor _motor;
        private Health _health;
        private PlayerDamageReaction _reaction;
        private PlayerAttack _attack;
        private PlayerSalts _salts;
        private PlayerSpoils _spoils;
        private IPlayerCommandSource _commands;
        private ContactFilter2D _triggers;
        private float _consumedInteractAt = float.NegativeInfinity;
        private float _stageStartedAt;

        public PlayerCombatSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public RestStage Stage { get; private set; }
        public float StageTime => Time.time - _stageStartedAt;
        public bool IsResting => Stage != RestStage.None;
        public IRestSpot Spot { get; private set; }

        /// <summary>The rest she last sat at; a defeat wakes her there and a save names it.</summary>
        public string LastRestId { get; private set; } = string.Empty;

        /// <summary>Raised once she is seated and the rest has taken effect.</summary>
        public event Action Rested;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _health = GetComponent<Health>();
            _reaction = GetComponent<PlayerDamageReaction>();
            _attack = GetComponent<PlayerAttack>();
            _salts = GetComponent<PlayerSalts>();
            _spoils = GetComponent<PlayerSpoils>();
            _commands = GetComponent<IPlayerCommandSource>();
            _triggers = new ContactFilter2D { useTriggers = true };
            if (_settings == null)
            {
                Debug.LogError("PlayerRest needs combat settings.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
        }

        private void FixedUpdate()
        {
            float now = Time.time;
            switch (Stage)
            {
                case RestStage.None:
                    if (_commands != null
                        && InputBuffer.IsFresh(_commands.InteractPressedAt, _consumedInteractAt, now, _settings.AttackBufferTime)
                        && CanSit() && TryFindSpot(out IRestSpot spot))
                    {
                        _consumedInteractAt = _commands.InteractPressedAt;
                        SitDown(spot);
                    }

                    break;
                case RestStage.Sitting:
                    Hold();
                    if (StageTime >= _settings.RestSitTime)
                    {
                        TakeRest();
                    }

                    break;
                case RestStage.Resting:
                    Hold();
                    if (WantsToGetUp())
                    {
                        _consumedInteractAt = _commands.InteractPressedAt;
                        Enter(RestStage.Rising);
                    }

                    break;
                case RestStage.Rising:
                    Hold();
                    if (StageTime >= _settings.RestRiseTime)
                    {
                        Enter(RestStage.None);
                    }

                    break;
            }
        }

        public void Capture(SaveData data)
        {
            data.RestId = LastRestId;
        }

        /// <summary>A loaded game starts in the chair it names, which is also where a defeat wakes her.</summary>
        public void Restore(SaveData data)
        {
            IRestSpot spot = RestSpots.Find(data.RestId);
            if (spot == null)
            {
                return;
            }

            LastRestId = spot.RestId;
            _motor.Teleport(spot.SeatPosition);
            _motor.Face(spot.Facing);
            if (_reaction != null)
            {
                _reaction.SetWakePoint(spot.SeatPosition, spot.Facing);
            }
        }

        private bool CanSit()
        {
            return _motor.IsGrounded && !_motor.IsSliding
                && (_reaction == null || !_reaction.IsDefeated)
                && (_attack == null || !_attack.IsAttacking)
                && (_salts == null || !_salts.IsUsing)
                && (_spoils == null || !_spoils.CanRecoverLoss);
        }

        private bool TryFindSpot(out IRestSpot spot)
        {
            Bounds body = GetComponent<Collider2D>().bounds;
            int count = Physics2D.OverlapBox(body.center, body.size, 0f, _triggers, _overlaps);
            for (int index = 0; index < count; index++)
            {
                if (_overlaps[index].TryGetComponent(out spot))
                {
                    return true;
                }
            }

            spot = null;
            return false;
        }

        private void SitDown(IRestSpot spot)
        {
            Spot = spot;
            _motor.Teleport(spot.SeatPosition);
            _motor.Face(spot.Facing);
            Enter(RestStage.Sitting);
            Hold();
        }

        private void TakeRest()
        {
            _health.Refill();
            WorldReset.Raise();
            LastRestId = Spot.RestId;
            if (_reaction != null)
            {
                _reaction.SetWakePoint(Spot.SeatPosition, Spot.Facing);
            }

            Enter(RestStage.Resting);
            Rested?.Invoke();
            Autosave.Request();
        }

        private bool WantsToGetUp()
        {
            if (_commands == null)
            {
                return false;
            }

            // Only presses made after she settled: the one that sat her down does not stand her up.
            return _commands.JumpPressedAt > _stageStartedAt || _commands.EvadePressedAt > _stageStartedAt
                || _commands.InteractPressedAt > _stageStartedAt;
        }

        /// <summary>A blow throws her out of the chair; before she is seated it also cancels the rest.</summary>
        private void OnDamaged(HitInfo hit)
        {
            if (Stage != RestStage.None)
            {
                Enter(RestStage.None);
            }
        }

        private void Hold()
        {
            _motor.Lock(Time.fixedDeltaTime * 2f);
        }

        private void Enter(RestStage stage)
        {
            Stage = stage;
            _stageStartedAt = Time.time;
        }
    }
}
