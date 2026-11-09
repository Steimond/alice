using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Archetype for something that hides among the decor and drops on Alice (the laundry sheet EN13, GDD section 9).
    /// It hangs like any other sheet; when she comes close it twitches, then drops and wraps whoever stands under it.
    /// The wrap is a grab: it cannot be parried, it squeezes on a rhythm, and Alice tears free by struggling. Missed
    /// or thrown off, it lies in a heap open to every blow, then creeps back up. A blow from outside its reach makes
    /// it drop harmlessly: that is how a wary player deals with it.
    /// </summary>
    public sealed class Ambusher : EnemyBase
    {
        public enum State
        {
            Hanging,
            Tremble,
            Drop,
            Hold,
            Heap,
            Rise,
            Dead
        }

        [SerializeField] private AmbusherSettings _settings;

        private IRestrainable _captive;
        private float _nextSqueezeAt;
        private float _armedAt;
        private bool _diedHanging;

        public AmbusherSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public override EnemySettings BaseSettings => _settings;
        public State CurrentState { get; private set; }

        // Cloth: Alice walks through it, and its touch does not hurt; only the wrap does.
        public override bool BlocksBodies => false;
        protected override bool ContactHurts => false;

        protected override string ArchetypeClipId
        {
            get
            {
                switch (CurrentState)
                {
                    case State.Tremble:
                        return "tremble";
                    case State.Drop:
                        return "drop";
                    case State.Hold:
                        return "wrap";
                    case State.Heap:
                        return "heap";
                    case State.Rise:
                        return "rise";
                    case State.Dead:
                        return _diedHanging && StateTime < _settings.Drop ? "drop" : "death";
                    default:
                        return "hang";
                }
            }
        }

        protected override float ArchetypeClipTime =>
            CurrentState == State.Dead && _diedHanging ? StateTime - _settings.Drop : StateTime;

        protected override float ArchetypeClipSpan
        {
            get
            {
                switch (CurrentState)
                {
                    case State.Tremble:
                        return _settings.Tremble;
                    case State.Drop:
                        return _settings.Drop;
                    case State.Rise:
                        return _settings.Rise;
                    default:
                        return 0f;
                }
            }
        }

        protected override void OnSpawn()
        {
            ReleaseCaptive();
            _armedAt = 0f;
            _diedHanging = false;
            Enter(State.Hanging);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ReleaseCaptive();
        }

        protected override void Tick()
        {
            switch (CurrentState)
            {
                case State.Hanging:
                    if (Time.time >= _armedAt && AliceWithin(_settings.TriggerRange))
                    {
                        Enter(State.Tremble);
                    }

                    break;
                case State.Tremble:
                    if (StateTime >= _settings.Tremble)
                    {
                        Enter(State.Drop);
                    }

                    break;
                case State.Drop:
                    if (StateTime >= _settings.Drop)
                    {
                        Land();
                    }

                    break;
                case State.Hold:
                    UpdateHold();
                    break;
                case State.Heap:
                    if (StateTime >= _settings.HeapTime)
                    {
                        Enter(State.Rise);
                    }

                    break;
                case State.Rise:
                    if (StateTime >= _settings.Rise)
                    {
                        _armedAt = Time.time + _settings.Rearm;
                        Enter(State.Hanging);
                    }

                    break;
            }
        }

        private bool AliceWithin(float range)
        {
            Transform alice = PlayerPresence.Current;
            if (alice == null)
            {
                return false;
            }

            Vector2 offset = (Vector2)alice.position - Body.position;
            return Mathf.Abs(offset.x) <= range && offset.y > -0.5f && offset.y < _settings.HangingBody.y;
        }

        /// <summary>Lands on the floor: wraps Alice when she stands under it, otherwise lies there.</summary>
        private void Land()
        {
            Transform alice = PlayerPresence.Current;
            if (alice != null && AliceWithin(_settings.WrapRange) && alice.TryGetComponent(out IRestrainable captive)
                && captive.TryRestrain(gameObject, _settings.PressesToEscape, Body.position))
            {
                _captive = captive;
                _nextSqueezeAt = Time.time + _settings.SqueezeInterval;
                Enter(State.Hold);
                return;
            }

            Enter(State.Heap);
            TryStun();
        }

        private void UpdateHold()
        {
            if (_captive == null || !_captive.IsRestrained)
            {
                // Torn free, or she fell: the sheet drops off her into a heap.
                _captive = null;
                Enter(State.Heap);
                return;
            }

            if (StateTime >= _settings.MaxHold)
            {
                ReleaseCaptive();
                Enter(State.Heap);
                return;
            }

            if (Time.time >= _nextSqueezeAt && _captive is Component component
                && component.TryGetComponent(out IDamageable target))
            {
                _nextSqueezeAt = Time.time + _settings.SqueezeInterval;
                // No throw: she stays inside; the zero-speed knockback keeps her own default push out of it.
                target.ReceiveHit(new HitInfo(_settings.SqueezeDamage, Facing, gameObject, HitKind.Other, 0f,
                    new Knockback(Vector2.zero, 0.01f)));
            }
        }

        private void ReleaseCaptive()
        {
            if (_captive != null)
            {
                _captive.Release(gameObject);
                _captive = null;
            }
        }

        protected override void OnHurt(HitInfo hit)
        {
            // Struck while it hangs, it drops at once; out of reach that wraps nobody.
            if (CurrentState == State.Hanging || CurrentState == State.Tremble)
            {
                Enter(State.Drop);
            }
        }

        // Only the heap on the floor reels: a hanging sheet just drops.
        protected override bool CanStunNow => CurrentState == State.Heap;

        protected override void OnStunEnded()
        {
            Enter(State.Rise);
        }

        protected override void OnKilled(HitInfo hit)
        {
            ReleaseCaptive();
            _diedHanging = CurrentState == State.Hanging || CurrentState == State.Tremble || CurrentState == State.Drop;
            Enter(State.Dead);
        }

        private void Enter(State state)
        {
            CurrentState = state;
            RestartStateClock();
            bool lying = state == State.Heap || state == State.Hold || state == State.Dead;
            Vector2 size = lying ? _settings.HeapBody : _settings.HangingBody;
            if (state == State.Hold)
            {
                // Wrapped around Alice it is as tall as she is, and her blows cannot reach it from inside.
                size = new Vector2(_settings.HeapBody.x * 0.5f, 3f);
            }

            BodyCollider.size = size;
            BodyCollider.offset = new Vector2(0f, size.y * 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            if (_settings == null)
            {
                return;
            }

            Vector3 position = transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(position + new Vector3(0f, _settings.HangingBody.y * 0.5f),
                new Vector3(_settings.TriggerRange * 2f, _settings.HangingBody.y));
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(position + new Vector3(0f, 1.5f), new Vector3(_settings.WrapRange * 2f, 3f));
        }
    }
}
