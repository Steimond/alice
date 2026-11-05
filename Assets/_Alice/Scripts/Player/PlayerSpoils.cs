using System;
using Alice.Combat;
using Alice.Core;
using Alice.Save;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// What Alice takes from the enemies she beats: Queen's decrees, the currency, and confidence for her
    /// special techniques (GDD sections 4 and 10). An execution gives more confidence than a plain kill.
    /// When she falls, the decrees she carries stay at her last safe footing as the one lost purse; Interact next
    /// to it gives them back, and falling again first loses the old purse for good.
    /// </summary>
    public sealed class PlayerSpoils : MonoBehaviour, IDecreeLoss, ISaveParticipant
    {
        [SerializeField] private PlayerCombatSettings _settings;

        private Health _health;
        private PlayerRespawn _respawn;
        private PlayerDamageReaction _reaction;
        private PlayerMotor _motor;
        private IPlayerCommandSource _commands;
        private float _consumedInteractAt = float.NegativeInfinity;

        public PlayerCombatSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public int Decrees { get; private set; }
        public ConfidencePool Confidence { get; private set; }

        public bool HasLoss { get; private set; }
        public int LossAmount { get; private set; }
        public string LossRoomId { get; private set; } = string.Empty;
        public Vector2 LossPosition { get; private set; }

        public event Action LossChanged;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _respawn = GetComponent<PlayerRespawn>();
            _reaction = GetComponent<PlayerDamageReaction>();
            _motor = GetComponent<PlayerMotor>();
            _commands = GetComponent<IPlayerCommandSource>();
            if (_settings == null)
            {
                Debug.LogError("PlayerSpoils needs combat settings.", this);
                enabled = false;
                return;
            }

            Confidence = new ConfidencePool(_settings.ConfidenceMax);
        }

        private void OnEnable()
        {
            EnemyDefeats.Happened += OnEnemyDefeated;
            if (_health != null)
            {
                _health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            EnemyDefeats.Happened -= OnEnemyDefeated;
            if (_health != null)
            {
                _health.Died -= OnDied;
            }
        }

        /// <summary>
        /// The purse lies in this room within reach of her feet and she is free to bend for it: Interact now picks
        /// it up (and does not sit her down in a chair that happens to be near).
        /// </summary>
        public bool CanRecoverLoss =>
            HasLoss && LossRoomId == RoomPresence.CurrentId
            && Vector2.Distance(transform.position, LossPosition) <= _settings.LossPickupRadius
            && (_reaction == null || !_reaction.IsDefeated)
            && (_motor == null || (_motor.IsGrounded && !_motor.IsLocked && !_motor.IsSliding));

        private void FixedUpdate()
        {
            if (_commands != null && CanRecoverLoss
                && InputBuffer.IsFresh(_commands.InteractPressedAt, _consumedInteractAt, Time.time, _settings.AttackBufferTime))
            {
                _consumedInteractAt = _commands.InteractPressedAt;
                RecoverLoss();
            }
        }

        /// <summary>Gives the whole purse back; nothing happens while she lies defeated.</summary>
        public void RecoverLoss()
        {
            if (!HasLoss || (_reaction != null && _reaction.IsDefeated))
            {
                return;
            }

            Decrees += LossAmount;
            SetLoss(false, 0, string.Empty, Vector2.zero);
            Autosave.Request();
        }

        public void Capture(SaveData data)
        {
            data.Decrees = Decrees;
            data.Confidence = Confidence.Current;
            data.Loss = new DecreeLossData
            {
                Exists = HasLoss,
                Amount = LossAmount,
                RoomId = LossRoomId,
                X = LossPosition.x,
                Y = LossPosition.y
            };
        }

        public void Restore(SaveData data)
        {
            Decrees = Mathf.Max(0, data.Decrees);
            Confidence.Set(data.Confidence);
            DecreeLossData loss = data.Loss;
            SetLoss(loss.Exists && loss.Amount > 0, loss.Amount, loss.RoomId, new Vector2(loss.X, loss.Y));
        }

        private void OnEnemyDefeated(EnemyDefeat defeat)
        {
            if (defeat.KillingBlow.Source != gameObject)
            {
                return;
            }

            Decrees += defeat.Decrees;
            float factor = defeat.KillingBlow.Kind == HitKind.Execution ? _settings.ExecutionConfidenceFactor : 1f;
            Confidence.Gain(defeat.Confidence * factor);
            // Spoils are kept the moment they are won, as in other soulslikes: quitting never rolls them back.
            Autosave.Request();
        }

        /// <summary>
        /// The old purse is gone for good; what she carried now lies at her last safe footing, so a fall into
        /// a pit or onto spikes leaves it where it can be reached.
        /// </summary>
        private void OnDied(HitInfo hit)
        {
            Vector2 where = _respawn != null ? _respawn.SafePosition : (Vector2)transform.position;
            SetLoss(Decrees > 0, Decrees, RoomPresence.CurrentId, where);
            Decrees = 0;
        }

        private void SetLoss(bool exists, int amount, string roomId, Vector2 position)
        {
            HasLoss = exists;
            LossAmount = exists ? amount : 0;
            LossRoomId = exists ? roomId ?? string.Empty : string.Empty;
            LossPosition = exists ? position : Vector2.zero;
            LossChanged?.Invoke();
        }
    }
}
