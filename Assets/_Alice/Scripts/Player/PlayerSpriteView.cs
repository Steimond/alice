using Alice.Combat;
using Alice.Core;
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

        public SpriteRenderer Renderer => _renderer;
        [SerializeField] private SpriteRenderer _placeholderBody;
        [SerializeField] private SpriteRenderer _placeholderMarker;

        private float _runDistance;
        private float _idleTime;
        private int _idleFrame = -2;
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
        private PlayerRest _rest;
        private bool _wasSliding;
        private float _slideElapsed;
        private float _riseUntil;
        private IShrinkable _size;
        private PlayerPickup _pickup;
        private IRestrainable _restraint;
        private bool _wasSmall;
        private float _shrinkElapsed = float.PositiveInfinity;

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
            _rest = GetComponent<PlayerRest>();
            _size = GetComponent<IShrinkable>();
            _pickup = GetComponent<PlayerPickup>();
            _restraint = GetComponent<IRestrainable>();
        }

        private void LateUpdate()
        {
            if (_motor == null || _sprites == null || _renderer == null)
            {
                return;
            }

            // The grey-box body stands in only while a slide has no frames yet.
            bool drawn = !_motor.IsSliding || _sprites.HasSlide;
            // Held inside a sheet: the holder draws the struggling bundle in her place.
            _renderer.enabled = drawn && (_restraint == null || !_restraint.IsRestrained);
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

            bool small = _size != null && _size.IsSmall;
            bool smallFrames = _sprites.HasSmall;
            Sprite shrink = SelectShrinkSprite(small, smallFrames);

            Sprite slide = SelectSlideSprite();
            if (slide != null)
            {
                Draw(slide, _motor.Facing, small);
                return;
            }

            Sprite reaction = SelectReactionSprite();
            if (reaction != null)
            {
                Draw(reaction, _motor.Facing, small);
                return;
            }

            Sprite strike = SelectStrikeSprite();
            if (strike != null)
            {
                Draw(strike, _attack.Facing, small);
                return;
            }

            Sprite parry = SelectParrySprite();
            if (parry != null)
            {
                Draw(parry, _parryAction.Facing, small);
                return;
            }

            if (shrink != null)
            {
                Draw(shrink, _motor.Facing, false);
                return;
            }

            if (small && smallFrames)
            {
                Draw(SelectSmallSprite(), _motor.Facing, false);
                return;
            }

            Draw(SelectSprite(), _motor.Facing, small);
        }

        /// <summary>
        /// Full-size frames are drawn at the fallback scale while she is small: only movement has small frames, and
        /// scaling pixel art down is a stopgap (GDD section 12), never the look of small Alice.
        /// </summary>
        private void Draw(Sprite sprite, float facing, bool fullSizeWhileSmall)
        {
            _renderer.flipX = facing < 0;
            _renderer.sprite = sprite;
            float scale = fullSizeWhileSmall ? _sprites.SmallFallbackScale : 1f;
            _renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// Becoming small: she bites the mushroom, then shrinks. Growing back plays the shrink backwards.
        /// </summary>
        private Sprite SelectShrinkSprite(bool small, bool smallFrames)
        {
            Sprite[] frames = _sprites.Shrink;
            if (small != _wasSmall)
            {
                _wasSmall = small;
                _shrinkElapsed = 0f;
            }
            else
            {
                _shrinkElapsed += Time.deltaTime;
            }

            if (!smallFrames || frames == null || frames.Length == 0)
            {
                return null;
            }

            float elapsed = _shrinkElapsed;
            Sprite[] bite = _sprites.Bite;
            if (small && bite != null && bite.Length > 0)
            {
                float biteTime = bite.Length * _sprites.BiteFrameTime;
                if (elapsed < biteTime)
                {
                    return bite[Mathf.Min(Mathf.FloorToInt(elapsed / _sprites.BiteFrameTime), bite.Length - 1)];
                }

                elapsed -= biteTime;
            }

            // Compared as time first: the idle value is infinite and would overflow as a frame index.
            if (elapsed >= frames.Length * _sprites.ShrinkFrameTime)
            {
                return null;
            }

            int frame = Mathf.Min(Mathf.FloorToInt(elapsed / _sprites.ShrinkFrameTime), frames.Length - 1);
            return small ? frames[frame] : frames[frames.Length - 1 - frame];
        }

        /// <summary>Small Alice only moves: standing, running, the jump by vertical speed and the landing.</summary>
        private Sprite SelectSmallSprite()
        {
            if (!_motor.IsGrounded)
            {
                if (!_wasAirborne)
                {
                    _wasAirborne = true;
                    _airTime = 0f;
                    _fallTime = 0f;
                }

                _airTime += Time.deltaTime;
                float vertical = _motor.Velocity.y;
                if (vertical > _sprites.ApexSpeed)
                {
                    Sprite[] rise = _sprites.SmallRise;
                    float climb = Mathf.InverseLerp(_sprites.RiseTopSpeed, _sprites.ApexSpeed, vertical);
                    return rise[Mathf.Min(Mathf.FloorToInt(climb * rise.Length), rise.Length - 1)];
                }

                if (vertical >= -_sprites.ApexSpeed)
                {
                    Sprite[] apex = _sprites.SmallApex;
                    return vertical >= 0f ? apex[0] : apex[apex.Length - 1];
                }

                _fallTime += Time.deltaTime;
                Sprite[] fall = _sprites.SmallFall;
                return fall[Mathf.FloorToInt(_fallTime / _sprites.FallFrameTime) % fall.Length];
            }

            if (_wasAirborne)
            {
                _wasAirborne = false;
                _landing = _airTime >= _sprites.LandMinAirTime;
                _landElapsed = 0f;
            }

            float speed = Mathf.Abs(_motor.Velocity.x);
            if (speed < _sprites.RunMinSpeed)
            {
                _runDistance = 0f;
                Sprite[] landing = _sprites.SmallLanding;
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

                return _sprites.SmallIdleAt(NextIdleTime());
            }

            _landing = false;
            _runDistance += speed * Time.deltaTime;
            Sprite[] run = _sprites.SmallRun;
            return run[Mathf.FloorToInt(_runDistance / _sprites.SmallRunFrameDistance) % run.Length];
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

            Sprite resting = SelectRestSprite();
            if (resting != null)
            {
                return resting;
            }

            if (_pickup != null && _pickup.IsPickingUp && _sprites.Pickup != null && _sprites.Pickup.Length > 0)
            {
                return Spread(_sprites.Pickup, _pickup.Progress);
            }

            return _salts != null && _salts.IsUsing ? Spread(_sprites.Salts, _salts.Progress) : null;
        }

        /// <summary>Sitting down into the chair, dozing in it, and the sit played backwards to get up.</summary>
        private Sprite SelectRestSprite()
        {
            Sprite[] sit = _sprites.RestSit;
            if (_rest == null || !_rest.IsResting || sit == null || sit.Length == 0)
            {
                return null;
            }

            PlayerCombatSettings settings = _rest.Settings;
            switch (_rest.Stage)
            {
                case PlayerRest.RestStage.Sitting:
                    return Spread(sit, _rest.StageTime / settings.RestSitTime);
                case PlayerRest.RestStage.Rising:
                    return Spread(sit, 1f - _rest.StageTime / settings.RestRiseTime);
                default:
                    Sprite[] doze = _sprites.RestDoze;
                    if (doze == null || doze.Length == 0)
                    {
                        return sit[sit.Length - 1];
                    }

                    return doze[Mathf.FloorToInt(_rest.StageTime / _sprites.RestDozeFrameTime) % doze.Length];
            }
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

                return IdleFrame();
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

        private Sprite IdleFrame()
        {
            return _sprites.IdleAt(NextIdleTime());
        }

        private float NextIdleTime()
        {
            // Any frame drawn with something else restarts the breath, so stopping always begins at the stance.
            if (_idleFrame != Time.frameCount - 1)
            {
                _idleTime = 0f;
            }
            else
            {
                _idleTime += Time.deltaTime;
            }

            _idleFrame = Time.frameCount;
            return _idleTime;
        }

        private Sprite RunFrame(Sprite[] run)
        {
            return run[Mathf.FloorToInt(_runDistance / _sprites.RunFrameDistance) % run.Length];
        }
    }
}
