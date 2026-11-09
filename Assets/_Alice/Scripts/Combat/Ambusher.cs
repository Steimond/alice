using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Archetype for something that hides among the decor and drops on Alice (the laundry sheet EN13, GDD section 9).
    /// It is cloth, not a fighter: one blow ends it (owner's decision, 9 November 2026). Cut while it hangs, its
    /// lower half falls bleeding and the upper half stays on the line. When Alice comes close it twitches, the one
    /// tell a watchful player gets; whoever steps under it is caught for certain (owner: otherwise players just run
    /// past it), so it never lies missed in a heap. The grab cannot be parried and squeezes on a rhythm until she
    /// struggles free, and then she tears it apart and it is dead.
    /// </summary>
    public sealed class Ambusher : EnemyBase
    {
        public enum State
        {
            Hanging,
            Tremble,
            Drop,
            Hold,
            Dead
        }

        [SerializeField] private AmbusherSettings _settings;

        private IRestrainable _captive;
        private float _nextSqueezeAt;
        private string _deathClip = "death";

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
                    case State.Dead:
                        return _deathClip;
                    default:
                        return "hang";
                }
            }
        }

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
                    default:
                        return 0f;
                }
            }
        }

        protected override void OnSpawn()
        {
            ReleaseCaptive();
            _deathClip = "death";
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
                    if (!TryGrab() && AliceWithin(_settings.TriggerRange))
                    {
                        Enter(State.Tremble);
                    }

                    break;
                case State.Tremble:
                    if (TryGrab())
                    {
                        break;
                    }

                    // It twitches while she lingers near and calms down when she goes; the twitch repeats.
                    if (!AliceWithin(_settings.TriggerRange))
                    {
                        Enter(State.Hanging);
                    }
                    else if (StateTime >= _settings.Tremble)
                    {
                        Enter(State.Tremble);
                    }

                    break;
                case State.Drop:
                    if (StateTime >= _settings.Drop)
                    {
                        Enter(State.Hold);
                    }

                    break;
                case State.Hold:
                    UpdateHold();
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

        /// <summary>
        /// Alice under it is caught the moment she is there: the hold starts as the cloth begins to fall, so no run
        /// carries her out from under it.
        /// </summary>
        private bool TryGrab()
        {
            Transform alice = PlayerPresence.Current;
            if (alice == null || !AliceWithin(_settings.WrapRange) || !alice.TryGetComponent(out IRestrainable captive)
                || !captive.TryRestrain(gameObject, _settings.PressesToEscape, Body.position))
            {
                return false;
            }

            _captive = captive;
            _nextSqueezeAt = Time.time + _settings.Drop + _settings.SqueezeInterval;
            Enter(State.Drop);
            return true;
        }

        private void UpdateHold()
        {
            if (_captive == null || !_captive.IsRestrained)
            {
                IRestrainable captive = _captive;
                _captive = null;
                if (captive != null && captive.BrokeFree && captive is Component freed)
                {
                    // She has torn free: she rips it apart in the same moment, and that is its end.
                    _deathClip = "torn";
                    Health.Kill(new HitInfo(Health.Current, -Facing, freed.gameObject, HitKind.Other));
                    return;
                }

                // She fell inside it: it hangs back up; her defeat resets the room anyway.
                Enter(State.Hanging);
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

        // A sturdier sheet in the data (more than one point of health) just takes the blow and keeps hanging.
        protected override void OnHurt(HitInfo hit)
        {
        }

        // Cloth does not reel and is not executed: one blow is enough.
        protected override bool CanStunNow => false;

        protected override void OnStunEnded()
        {
        }

        protected override void OnKilled(HitInfo hit)
        {
            if (CurrentState == State.Hanging || CurrentState == State.Tremble)
            {
                _deathClip = "cut";
            }

            ReleaseCaptive();
            Enter(State.Dead);
        }

        private void Enter(State state)
        {
            CurrentState = state;
            RestartStateClock();
            Vector2 size = state == State.Dead ? _settings.HeapBody : _settings.HangingBody;
            if (state == State.Drop || state == State.Hold)
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
