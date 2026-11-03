using Alice.Combat;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Draws Alice from <see cref="PlayerSpriteSet"/>: standing, running, the jump by vertical speed, the
    /// step off a ledge, the fall loop, the landing crouch and the needle thrust by attack phase, which
    /// overrides the rest while it lasts. Sliding has no frames yet, so the grey-box
    /// body takes over for it.
    /// </summary>
    public sealed class PlayerSpriteView : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerSpriteSet _sprites;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private SpriteRenderer _placeholderBody;
        [SerializeField] private SpriteRenderer _placeholderMarker;

        private float _runDistance;
        private bool _wasAirborne;
        private bool _jumped;
        private float _airTime;
        private float _fallTime;
        private bool _landing;
        private float _landElapsed;
        private PlayerAttack _attack;

        public void Bind(PlayerMotor motor, PlayerSpriteSet sprites, SpriteRenderer renderer,
            SpriteRenderer placeholderBody, SpriteRenderer placeholderMarker)
        {
            _motor = motor;
            _sprites = sprites;
            _renderer = renderer;
            _placeholderBody = placeholderBody;
            _placeholderMarker = placeholderMarker;
        }

        public bool HasThrustFrames => _sprites != null && _sprites.HasThrust;

        private void Awake()
        {
            _attack = GetComponent<PlayerAttack>();
        }

        private void LateUpdate()
        {
            if (_motor == null || _sprites == null || _renderer == null)
            {
                return;
            }

            bool drawn = !_motor.IsSliding;
            _renderer.enabled = drawn;
            if (_placeholderBody != null)
            {
                _placeholderBody.enabled = !drawn;
            }

            if (_placeholderMarker != null)
            {
                _placeholderMarker.enabled = !drawn;
            }

            if (!drawn)
            {
                _runDistance = 0f;
                return;
            }

            Sprite thrust = SelectThrustSprite();
            if (thrust != null)
            {
                _renderer.flipX = _attack.Facing < 0;
                _renderer.sprite = thrust;
                return;
            }

            _renderer.flipX = _motor.Facing < 0;
            _renderer.sprite = SelectSprite();
        }

        private Sprite SelectThrustSprite()
        {
            if (_attack == null || !_attack.IsAttacking || !_sprites.HasThrust)
            {
                return null;
            }

            Sprite[] frames = _attack.Phase switch
            {
                AttackPhase.Windup => _sprites.ThrustWindup,
                AttackPhase.Active => _sprites.ThrustActive,
                AttackPhase.Recovery => _sprites.ThrustRecovery,
                _ => null
            };

            if (frames == null || frames.Length == 0)
            {
                return null;
            }

            int frame = Mathf.FloorToInt(_attack.PhaseProgress * frames.Length);
            return frames[Mathf.Min(frame, frames.Length - 1)];
        }

        private Sprite SelectSprite()
        {
            Sprite[] run = _sprites.Run;
            if (run == null || run.Length == 0)
            {
                return _sprites.Idle;
            }

            if (!_motor.IsGrounded)
            {
                if (!_wasAirborne)
                {
                    _wasAirborne = true;
                    _airTime = 0f;
                    _fallTime = 0f;
                    _jumped = false;
                }

                _airTime += Time.deltaTime;
                if (_motor.Velocity.y > _sprites.ApexSpeed)
                {
                    _jumped = true;
                }

                return _sprites.HasJump ? SelectAirSprite(run) : run[Mathf.Clamp(_sprites.AirRunFrame, 0, run.Length - 1)];
            }

            if (_wasAirborne)
            {
                _wasAirborne = false;
                // A short hop off a step or a seam in the floor would read as a stumble with the crouch.
                _landing = _airTime >= _sprites.LandMinAirTime;
                _landElapsed = 0f;
            }

            float speed = Mathf.Abs(_motor.Velocity.x);
            if (speed < _sprites.RunMinSpeed)
            {
                _runDistance = 0f;
                Sprite[] landing = _sprites.Landing;
                if (_landing && landing != null && landing.Length > 0)
                {
                    int frame = Mathf.FloorToInt(_landElapsed / _sprites.LandFrameTime);
                    if (frame < landing.Length)
                    {
                        _landElapsed += Time.deltaTime;
                        return landing[frame];
                    }

                    _landing = false;
                }

                return _sprites.Idle;
            }

            // Running on after touchdown skips the crouch: it would read as a stumble.
            _landing = false;

            _runDistance += speed * Time.deltaTime;
            return RunFrame(run);
        }

        private Sprite SelectAirSprite(Sprite[] run)
        {
            float vertical = _motor.Velocity.y;
            if (!_jumped)
            {
                // Walked off an edge: the stride holds for a moment, then the step down leads into the fall.
                if (_airTime < _sprites.StepOffDelay)
                {
                    return RunFrame(run);
                }

                Sprite[] stepOff = _sprites.StepOff;
                int step = Mathf.FloorToInt((_airTime - _sprites.StepOffDelay) / _sprites.StepOffFrameTime);
                if (stepOff != null && step < stepOff.Length)
                {
                    return stepOff[step];
                }

                // The step already led into the fall, so the fall-start frame is skipped.
                return SelectFallSprite(skipStart: stepOff != null && stepOff.Length > 0);
            }

            if (vertical > _sprites.ApexSpeed)
            {
                _fallTime = 0f;
                Sprite[] rise = _sprites.JumpRise;
                float climb = Mathf.InverseLerp(_sprites.RiseTopSpeed, _sprites.ApexSpeed, vertical);
                return rise[Mathf.Min(Mathf.FloorToInt(climb * rise.Length), rise.Length - 1)];
            }

            if (vertical >= -_sprites.ApexSpeed)
            {
                _fallTime = 0f;
                Sprite[] apex = _sprites.JumpApex;
                return vertical >= 0f ? apex[0] : apex[apex.Length - 1];
            }

            return SelectFallSprite(skipStart: false);
        }

        private Sprite SelectFallSprite(bool skipStart)
        {
            _fallTime += Time.deltaTime;
            float loopTime = _fallTime;
            if (!skipStart && _sprites.FallStart != null)
            {
                if (_fallTime < _sprites.FallStartTime)
                {
                    return _sprites.FallStart;
                }

                loopTime -= _sprites.FallStartTime;
            }

            Sprite[] loop = _sprites.FallLoop;
            return loop[Mathf.FloorToInt(loopTime / _sprites.FallFrameTime) % loop.Length];
        }

        private Sprite RunFrame(Sprite[] run)
        {
            return run[Mathf.FloorToInt(_runDistance / _sprites.RunFrameDistance) % run.Length];
        }
    }
}
