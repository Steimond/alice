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
        private PlayerParry _parryAction;
        private PlayerDamageReaction _damage;
        private PlayerSalts _salts;
        private bool _wasSliding;
        private float _slideElapsed;
        private float _riseUntil;

        public void Bind(PlayerMotor motor, PlayerSpriteSet sprites, SpriteRenderer renderer,
            SpriteRenderer placeholderBody, SpriteRenderer placeholderMarker)
        {
            _motor = motor;
            _sprites = sprites;
            _renderer = renderer;
            _placeholderBody = placeholderBody;
            _placeholderMarker = placeholderMarker;
        }

        private void Awake()
        {
            _attack = GetComponent<PlayerAttack>();
            _parryAction = GetComponent<PlayerParry>();
            _damage = GetComponent<PlayerDamageReaction>();
            _salts = GetComponent<PlayerSalts>();
        }

        private void LateUpdate()
        {
            if (_motor == null || _sprites == null || _renderer == null)
            {
                return;
            }

            // The grey-box body stands in only while a slide has no frames yet.
            bool drawn = !_motor.IsSliding || _sprites.HasSlide;
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

            Sprite slide = SelectSlideSprite();
            if (slide != null)
            {
                _renderer.flipX = _motor.Facing < 0;
                _renderer.sprite = slide;
                return;
            }

            Sprite reaction = SelectReactionSprite();
            if (reaction != null)
            {
                _renderer.flipX = _motor.Facing < 0;
                _renderer.sprite = reaction;
                return;
            }

            Sprite strike = SelectStrikeSprite();
            if (strike != null)
            {
                _renderer.flipX = _attack.Facing < 0;
                _renderer.sprite = strike;
                return;
            }

            Sprite parry = SelectParrySprite();
            if (parry != null)
            {
                _renderer.flipX = _parryAction.Facing < 0;
                _renderer.sprite = parry;
                return;
            }

            _renderer.flipX = _motor.Facing < 0;
            _renderer.sprite = SelectSprite();
        }

        /// <summary>The drop, the slide on her back while it lasts, then a moment getting up on the ground.</summary>
        private Sprite SelectSlideSprite()
        {
            if (!_sprites.HasSlide)
            {
                return null;
            }

            if (_motor.IsSliding)
            {
                if (!_wasSliding)
                {
                    _wasSliding = true;
                    _slideElapsed = 0f;
                }

                _slideElapsed += Time.deltaTime;
                if (_slideElapsed < _sprites.SlideDropTime && _sprites.SlideDrop != null)
                {
                    return _sprites.SlideDrop;
                }

                Sprite[] loop = _sprites.SlideLoop;
                int frame = Mathf.FloorToInt((_slideElapsed - _sprites.SlideDropTime) / _sprites.SlideFrameTime);
                return loop[Mathf.Max(0, frame) % loop.Length];
            }

            if (_wasSliding)
            {
                _wasSliding = false;
                _riseUntil = Time.time + _sprites.SlideRiseTime;
            }

            bool rising = Time.time < _riseUntil && _motor.IsGrounded && _sprites.SlideRise != null
                && (_attack == null || !_attack.IsAttacking);
            return rising ? _sprites.SlideRise : null;
        }

        /// <summary>Defeat and waking override everything; the flinch overrides all but them.</summary>
        private Sprite SelectReactionSprite()
        {
            if (_damage == null)
            {
                return null;
            }

            PlayerCombatSettings settings = _damage.Settings;
            switch (_damage.Stage)
            {
                case PlayerDamageReaction.DefeatStage.Falling:
                    return Spread(_sprites.Defeat, _damage.StageTime / settings.DefeatFallTime);
                case PlayerDamageReaction.DefeatStage.Dark:
                    return Spread(_sprites.Wake, 0f);
                case PlayerDamageReaction.DefeatStage.Waking:
                    return Spread(_sprites.Wake, _damage.StageTime / settings.WakeTime);
            }

            if (_damage.IsHurt)
            {
                return Spread(_sprites.Hurt, (Time.time - _damage.HurtAt) / settings.HurtTime);
            }

            return _salts != null && _salts.IsUsing ? Spread(_sprites.Salts, _salts.Progress) : null;
        }

        private static Sprite Spread(Sprite[] frames, float progress)
        {
            if (frames == null || frames.Length == 0)
            {
                return null;
            }

            return frames[Mathf.Clamp(Mathf.FloorToInt(progress * frames.Length), 0, frames.Length - 1)];
        }

        private Sprite SelectStrikeSprite()
        {
            if (_attack == null || !_attack.IsAttacking)
            {
                return null;
            }

            Sprite[] frames = _attack.Strike switch
            {
                PlayerStrike.Execution => _sprites.ExecutionPhases(_attack.ExecutionId).Phase(_attack.Phase),
                PlayerStrike.Technique => _sprites.TechniquePhases(_attack.TechniqueId).Phase(_attack.Phase),
                _ => _sprites.StrikePhase(_attack.Strike, _attack.Phase)
            };
            if (frames == null || frames.Length == 0)
            {
                return null;
            }

            int frame = Mathf.FloorToInt(_attack.PhaseProgress * frames.Length);
            return frames[Mathf.Min(frame, frames.Length - 1)];
        }

        /// <summary>
        /// Guard at first; a caught blow flashes the deflect frame, then the hold; a parry
        /// that caught nothing holds high and lowers over the rest of its time.
        /// </summary>
        private Sprite SelectParrySprite()
        {
            if (_parryAction == null || !_parryAction.IsParrying || !_sprites.HasParry)
            {
                return null;
            }

            PlayerCombatSettings settings = _parryAction.Settings;
            if (_parryAction.Succeeded)
            {
                float sinceCatch = Time.time - _parryAction.SucceededAt;
                return sinceCatch < _sprites.ParryDeflectTime
                    ? Spread(_sprites.ParryDeflect, sinceCatch / _sprites.ParryDeflectTime)
                    : Spread(_sprites.ParryHold, 0f);
            }

            float elapsed = _parryAction.Elapsed;
            if (elapsed <= _sprites.ParryGuardTime)
            {
                return Spread(_sprites.ParryGuard, elapsed / _sprites.ParryGuardTime);
            }

            // Nothing caught: she holds high for the first half of what is left, then lowers.
            float rest = Mathf.Max(0.01f, settings.ParryDuration - _sprites.ParryGuardTime);
            float late = (elapsed - _sprites.ParryGuardTime) / rest;
            return late < 0.5f ? Spread(_sprites.ParryHold, late * 2f) : Spread(_sprites.ParryLower, late * 2f - 1f);
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
