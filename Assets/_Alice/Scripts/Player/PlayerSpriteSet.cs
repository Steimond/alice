using System;
using System.Linq;
using Alice.Combat;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>Alice's frames for one enemy type's execution, or for one special technique, by ID.</summary>
    [Serializable]
    public struct ExecutionFrames
    {
        [SerializeField] private string _id;
        [SerializeField] private StrikeFrames _frames;

        public ExecutionFrames(string id, StrikeFrames frames)
        {
            _id = id;
            _frames = frames;
        }

        public string Id => _id;
        public StrikeFrames Frames => _frames;
    }

    /// <summary>Frames of one needle strike, each group spread over its attack phase.</summary>
    [Serializable]
    public struct StrikeFrames
    {
        [SerializeField] private Sprite[] _windup;
        [SerializeField] private Sprite[] _active;
        [SerializeField] private Sprite[] _recovery;

        public StrikeFrames(Sprite[] windup, Sprite[] active, Sprite[] recovery)
        {
            _windup = windup;
            _active = active;
            _recovery = recovery;
        }

        public bool IsEmpty => _active == null || _active.Length == 0;

        public Sprite[] Phase(AttackPhase phase)
        {
            return phase switch
            {
                AttackPhase.Windup => _windup,
                AttackPhase.Active => _active,
                AttackPhase.Recovery => _recovery,
                _ => null
            };
        }
    }

    /// <summary>
    /// Alice's frame-by-frame sprites for <see cref="PlayerSpriteView"/>. The run advances by distance
    /// travelled, not by time, so the feet stay planted at any speed the motor produces.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Player Sprite Set", fileName = "AliceSprites")]
    public sealed class PlayerSpriteSet : ScriptableObject
    {
        [SerializeField] private Sprite _idle;
        [Tooltip("Breathing while standing; the first frame is the stance itself, so a stop never jumps.")]
        [SerializeField] private Sprite[] _idleBreath = new Sprite[0];
        [SerializeField, Min(0.01f)] private float _idleFrameTime = 0.3f;
        [Tooltip("Played once after a stretch of breathing (she looks at her hem and sways), then the breath resumes.")]
        [SerializeField] private Sprite[] _idleFidget = new Sprite[0];
        [SerializeField, Min(0.01f)] private float _idleFidgetFrameTime = 0.15f;
        [Tooltip("Breathing before the fidget; rounded up to whole breaths so it always starts from the stance.")]
        [SerializeField, Min(0f)] private float _idleFidgetDelay = 6f;
        [SerializeField] private Sprite[] _run = new Sprite[0];

        [Tooltip("Distance in units the body travels during one run frame; matches how far the planted foot slides back in the drawing.")]
        [SerializeField, Min(0.01f)] private float _runFrameDistance = 0.4375f;

        [Tooltip("Below this horizontal speed on the ground Alice is drawn standing.")]
        [SerializeField, Min(0f)] private float _runMinSpeed = 0.5f;

        [Tooltip("Run frame shown in the air when the jump frames are missing.")]
        [SerializeField, Min(0)] private int _airRunFrame = 3;

        [Header("Jump")]
        [Tooltip("From the push-off to the slowing climb; picked by upward speed, the first at launch speed.")]
        [SerializeField] private Sprite[] _jumpRise = new Sprite[0];
        [Tooltip("Top of the jump: the tuck while still rising, then opening up as the fall begins.")]
        [SerializeField] private Sprite[] _jumpApex = new Sprite[0];
        [SerializeField] private Sprite _fallStart;
        [Tooltip("Looped for as long as Alice falls.")]
        [SerializeField] private Sprite[] _fallLoop = new Sprite[0];
        [Tooltip("Touchdown without running on: impact, then recovery.")]
        [SerializeField] private Sprite[] _landing = new Sprite[0];
        [Tooltip("Walking off a ledge without a jump, before the fall loop takes over.")]
        [SerializeField] private Sprite[] _stepOff = new Sprite[0];

        [Tooltip("Upward speed (units/s) of the push-off frame; slower climbs move through the rise frames.")]
        [SerializeField, Min(0f)] private float _riseTopSpeed = 22f;
        [Tooltip("Vertical speed (units/s) within which Alice is drawn at the top of the jump.")]
        [SerializeField, Min(0f)] private float _apexSpeed = 4f;
        [SerializeField, Min(0f)] private float _fallStartTime = 0.1f;
        [SerializeField, Min(0.01f)] private float _fallFrameTime = 0.1f;
        [SerializeField, Min(0.01f)] private float _landFrameTime = 0.06f;
        [SerializeField, Min(0.01f)] private float _stepOffFrameTime = 0.08f;

        [Tooltip("Air time after walking off an edge during which the run frame holds, so small steps and seams do not flicker.")]
        [SerializeField, Min(0f)] private float _stepOffDelay = 0.06f;

        [Tooltip("Shorter air time lands without the crouch.")]
        [SerializeField, Min(0f)] private float _landMinAirTime = 0.2f;

        [Header("Needle thrust, spread over each attack phase")]
        [SerializeField] private Sprite[] _thrustWindup = new Sprite[0];
        [SerializeField] private Sprite[] _thrustActive = new Sprite[0];
        [SerializeField] private Sprite[] _thrustRecovery = new Sprite[0];

        [Header("Slash (second hit of the combo) and the air attack")]
        [SerializeField] private StrikeFrames _slash;
        [SerializeField] private StrikeFrames _airSlash;

        [Header("Counter after a parry; the thrust stands in while it has no frames")]
        [SerializeField] private StrikeFrames _counter;

        [Header("Executions, by the enemy type's execution ID")]
        [SerializeField] private ExecutionFrames[] _executions = new ExecutionFrames[0];

        [Header("Parry: guard, deflect, hold, lower, one after another")]
        [SerializeField] private Sprite[] _parry = new Sprite[0];
        [Tooltip("How many of the parry frames belong to the guard, the deflect, the hold and the lowering.")]
        [SerializeField] private int[] _parryPhaseCounts = { 1, 1, 1, 1 };
        [Tooltip("After a caught blow the deflect frames play over this time, then the hold.")]
        [SerializeField, Min(0f)] private float _parryDeflectTime = 0.12f;
        [Tooltip("The guard frame shows this long when nothing is caught; the catch itself is timed by each blow.")]
        [SerializeField, Min(0f)] private float _parryGuardTime = 0.15f;

        [Header("Hurt, defeat and waking")]
        [Tooltip("Flinch after a hit, spread over the hurt time.")]
        [SerializeField] private Sprite[] _hurt = new Sprite[0];
        [Tooltip("Collapse, spread over the fall time; the last frame holds into the darkness.")]
        [SerializeField] private Sprite[] _defeat = new Sprite[0];
        [Tooltip("The gasp at the rest and getting up, spread over the wake time.")]
        [SerializeField] private Sprite[] _wake = new Sprite[0];

        [Header("Special techniques, by technique ID")]
        [SerializeField] private ExecutionFrames[] _techniques = new ExecutionFrames[0];

        [Header("Small (mushroom, A02)")]
        [Tooltip("Standing small: a breathing loop.")]
        [SerializeField] private Sprite[] _smallIdle = new Sprite[0];
        [SerializeField] private Sprite[] _smallRun = new Sprite[0];
        [Tooltip("In the air, by vertical speed: rising, the top, falling.")]
        [SerializeField] private Sprite[] _smallRise = new Sprite[0];
        [SerializeField] private Sprite[] _smallApex = new Sprite[0];
        [SerializeField] private Sprite[] _smallFall = new Sprite[0];
        [SerializeField] private Sprite[] _smallLanding = new Sprite[0];
        [Tooltip("The bite of the mushroom, before she shrinks; full size, from the stance back to the stance.")]
        [SerializeField] private Sprite[] _bite = new Sprite[0];
        [SerializeField, Min(0.01f)] private float _biteFrameTime = 0.08f;
        [Tooltip("From her standing pose to the small one; played backwards when she grows back.")]
        [SerializeField] private Sprite[] _shrink = new Sprite[0];
        [SerializeField, Min(0.01f)] private float _smallIdleFrameTime = 0.3f;
        [Tooltip("Metres per run frame while small: her legs are short, so they turn over faster at the same speed.")]
        [SerializeField, Min(0.01f)] private float _smallRunFrameDistance = 0.25f;
        [SerializeField, Min(0.01f)] private float _shrinkFrameTime = 0.06f;
        [Tooltip("What has no small frames (attacks, reactions) is drawn at this scale while she is small.")]
        [SerializeField, Range(0.2f, 1f)] private float _smallFallbackScale = 0.5f;

        public Sprite[] SmallRun => _smallRun;
        public float SmallRunFrameDistance => _smallRunFrameDistance;
        public Sprite[] SmallRise => _smallRise;
        public Sprite[] SmallApex => _smallApex;
        public Sprite[] SmallFall => _smallFall;
        public Sprite[] SmallLanding => _smallLanding;
        public Sprite[] Shrink => _shrink;
        public Sprite[] Bite => _bite;
        public float BiteFrameTime => _biteFrameTime;

        public void AssignBite(Sprite[] bite)
        {
            _bite = bite;
        }
        public float ShrinkFrameTime => _shrinkFrameTime;
        public float SmallFallbackScale => _smallFallbackScale;

        public bool HasSmall => _smallIdle != null && _smallIdle.Length > 0 && _smallRun != null && _smallRun.Length > 0
            && _smallRise != null && _smallRise.Length > 0 && _smallApex != null && _smallApex.Length > 0
            && _smallFall != null && _smallFall.Length > 0;

        public Sprite SmallIdleAt(float time)
        {
            return _smallIdle[Mathf.FloorToInt(time / _smallIdleFrameTime) % _smallIdle.Length];
        }

        public void AssignSmall(Sprite[] idle, Sprite[] run, Sprite[] rise, Sprite[] apex, Sprite[] fall, Sprite[] landing,
            Sprite[] shrink)
        {
            _smallIdle = idle;
            _smallRun = run;
            _smallRise = rise;
            _smallApex = apex;
            _smallFall = fall;
            _smallLanding = landing;
            _shrink = shrink;
        }

        [Header("Slide")]
        [Tooltip("Dropping into the slide, shown first.")]
        [SerializeField] private Sprite _slideDrop;
        [Tooltip("Sliding on the back; alternates for the rest of the slide.")]
        [SerializeField] private Sprite[] _slideLoop = new Sprite[0];
        [Tooltip("Getting up, shown briefly after the slide if she stays on the ground.")]
        [SerializeField] private Sprite _slideRise;
        [SerializeField, Min(0.01f)] private float _slideDropTime = 0.06f;
        [SerializeField, Min(0.01f)] private float _slideFrameTime = 0.08f;
        [SerializeField, Min(0f)] private float _slideRiseTime = 0.08f;

        public Sprite SlideDrop => _slideDrop;
        public Sprite[] SlideLoop => _slideLoop;
        public Sprite SlideRise => _slideRise;
        public float SlideDropTime => _slideDropTime;
        public float SlideFrameTime => _slideFrameTime;
        public float SlideRiseTime => _slideRiseTime;
        public bool HasSlide => _slideLoop != null && _slideLoop.Length > 0;

        public void AssignSlide(Sprite drop, Sprite[] loop, Sprite rise)
        {
            _slideDrop = drop;
            _slideLoop = loop;
            _slideRise = rise;
        }

        public void AssignTechnique(string id, StrikeFrames frames)
        {
            var list = new System.Collections.Generic.List<ExecutionFrames>(_techniques ?? new ExecutionFrames[0]);
            list.RemoveAll(entry => entry.Id == id);
            list.Add(new ExecutionFrames(id, frames));
            _techniques = list.ToArray();
        }

        public bool HasTechnique(string id)
        {
            return !TechniquePhases(id).IsEmpty;
        }

        public StrikeFrames TechniquePhases(string id)
        {
            if (_techniques != null)
            {
                foreach (ExecutionFrames entry in _techniques)
                {
                    if (entry.Id == id)
                    {
                        return entry.Frames;
                    }
                }
            }

            return default;
        }

        [Tooltip("Smelling salts: the vial to the nose, the sniff, the jolt; spread over the use.")]
        [SerializeField] private Sprite[] _salts = new Sprite[0];

        public Sprite[] Salts => _salts;

        public void AssignSalts(Sprite[] salts)
        {
            _salts = salts;
        }

        [Tooltip("Stabbing the needle into the pincushion and sitting down in the armchair, spread over the sit; the chair, spools and cushion are drawn into every frame. Played backwards to get up.")]
        [SerializeField] private Sprite[] _restSit = new Sprite[0];
        [Tooltip("Dozing in the chair, looped while she rests: the head nods.")]
        [SerializeField] private Sprite[] _restDoze = new Sprite[0];
        [SerializeField, Min(0.05f)] private float _restDozeFrameTime = 0.35f;

        public Sprite[] RestSit => _restSit;
        public Sprite[] RestDoze => _restDoze;
        public float RestDozeFrameTime => _restDozeFrameTime;

        public void AssignRest(Sprite[] sit, Sprite[] doze)
        {
            _restSit = sit;
            _restDoze = doze;
        }

        public Sprite[] Hurt => _hurt;
        public Sprite[] Defeat => _defeat;
        public Sprite[] Wake => _wake;

        public void AssignReactions(Sprite[] hurt, Sprite[] defeat, Sprite[] wake)
        {
            _hurt = hurt;
            _defeat = defeat;
            _wake = wake;
        }

        public Sprite Idle => _idle;
        public Sprite[] IdleBreath => _idleBreath;

        /// <summary>The standing frame after <paramref name="time"/> seconds of standing still.</summary>
        public Sprite IdleAt(float time)
        {
            if (_idleBreath == null || _idleBreath.Length == 0)
            {
                return _idle;
            }

            if (_idleFidget != null && _idleFidget.Length > 0)
            {
                float breath = _idleBreath.Length * _idleFrameTime;
                float breathing = Mathf.Max(1f, Mathf.Ceil(_idleFidgetDelay / breath)) * breath;
                float cycle = breathing + _idleFidget.Length * _idleFidgetFrameTime;
                float inCycle = time % cycle;
                if (inCycle >= breathing)
                {
                    int frame = Mathf.FloorToInt((inCycle - breathing) / _idleFidgetFrameTime);
                    return _idleFidget[Mathf.Min(frame, _idleFidget.Length - 1)];
                }

                time = inCycle;
            }

            return _idleBreath[Mathf.FloorToInt(time / _idleFrameTime) % _idleBreath.Length];
        }

        public Sprite[] IdleFidget => _idleFidget;

        public void AssignIdleBreath(Sprite[] breath)
        {
            _idleBreath = breath;
        }

        public void AssignIdleFidget(Sprite[] fidget)
        {
            _idleFidget = fidget;
        }

        public Sprite[] Run => _run;
        public float RunFrameDistance => _runFrameDistance;
        public float RunMinSpeed => _runMinSpeed;
        public int AirRunFrame => _airRunFrame;
        public Sprite[] JumpRise => _jumpRise;
        public Sprite[] JumpApex => _jumpApex;
        public Sprite FallStart => _fallStart;
        public Sprite[] FallLoop => _fallLoop;
        public Sprite[] Landing => _landing;
        public Sprite[] StepOff => _stepOff;
        public float RiseTopSpeed => _riseTopSpeed;
        public float ApexSpeed => _apexSpeed;
        public float FallStartTime => _fallStartTime;
        public float FallFrameTime => _fallFrameTime;
        public float LandFrameTime => _landFrameTime;
        public float StepOffFrameTime => _stepOffFrameTime;
        public float StepOffDelay => _stepOffDelay;
        public float LandMinAirTime => _landMinAirTime;
        public Sprite[] ThrustWindup => _thrustWindup;
        public Sprite[] ThrustActive => _thrustActive;
        public Sprite[] ThrustRecovery => _thrustRecovery;

        public bool HasThrust => _thrustActive != null && _thrustActive.Length > 0;
        public Sprite[] Parry => _parry;
        public float ParryDeflectTime => _parryDeflectTime;
        public float ParryGuardTime => _parryGuardTime;
        public bool HasParry => _parry != null && _parryPhaseCounts != null && _parryPhaseCounts.Length == ParryPhaseCount
            && _parry.Length == _parryPhaseCounts.Sum() && _parryPhaseCounts.All(count => count > 0);
        public const int ParryPhaseCount = 4;

        public Sprite[] ParryGuard => ParryPhase(0);
        public Sprite[] ParryDeflect => ParryPhase(1);
        public Sprite[] ParryHold => ParryPhase(2);
        public Sprite[] ParryLower => ParryPhase(3);

        private Sprite[] ParryPhase(int phase)
        {
            int start = _parryPhaseCounts.Take(phase).Sum();
            return _parry.Skip(start).Take(_parryPhaseCounts[phase]).ToArray();
        }
        public StrikeFrames Slash => _slash;
        public StrikeFrames AirSlash => _airSlash;

        /// <summary>Frames for a phase of a strike; empty when that strike has no frames yet.</summary>
        public Sprite[] StrikePhase(PlayerStrike strike, AttackPhase phase)
        {
            if (strike == PlayerStrike.Counter && !_counter.IsEmpty)
            {
                return _counter.Phase(phase);
            }

            // Without its own frames the counter is a deeper thrust: the same frames, held longer.
            if (strike == PlayerStrike.Thrust || strike == PlayerStrike.Counter)
            {
                return phase switch
                {
                    AttackPhase.Windup => _thrustWindup,
                    AttackPhase.Active => _thrustActive,
                    AttackPhase.Recovery => _thrustRecovery,
                    _ => null
                };
            }

            StrikeFrames frames = strike == PlayerStrike.AirSlash ? _airSlash : _slash;
            return strike == PlayerStrike.None ? null : frames.Phase(phase);
        }

        public bool HasJump => _jumpRise != null && _jumpRise.Length > 0
            && _jumpApex != null && _jumpApex.Length > 0
            && _fallLoop != null && _fallLoop.Length > 0;

        public void Assign(Sprite idle, Sprite[] run)
        {
            _idle = idle;
            _run = run;
        }

        public void AssignJump(Sprite[] rise, Sprite[] apex, Sprite fallStart, Sprite[] fallLoop, Sprite[] landing,
            Sprite[] stepOff)
        {
            _jumpRise = rise;
            _jumpApex = apex;
            _fallStart = fallStart;
            _fallLoop = fallLoop;
            _landing = landing;
            _stepOff = stepOff;
        }

        public void AssignThrust(Sprite[] windup, Sprite[] active, Sprite[] recovery)
        {
            _thrustWindup = windup;
            _thrustActive = active;
            _thrustRecovery = recovery;
        }

        public StrikeFrames Counter => _counter;

        public void AssignCounter(StrikeFrames counter)
        {
            _counter = counter;
        }

        public void AssignSlashes(StrikeFrames slash, StrikeFrames airSlash)
        {
            _slash = slash;
            _airSlash = airSlash;
        }

        public void AssignParry(Sprite[] parry, int guard, int deflect, int hold, int lower)
        {
            _parry = parry;
            _parryPhaseCounts = new[] { guard, deflect, hold, lower };
        }

        public void AssignExecution(string id, StrikeFrames frames)
        {
            var list = new System.Collections.Generic.List<ExecutionFrames>(_executions ?? new ExecutionFrames[0]);
            list.RemoveAll(entry => entry.Id == id);
            list.Add(new ExecutionFrames(id, frames));
            _executions = list.ToArray();
        }

        public bool HasExecution(string id)
        {
            return !ExecutionPhases(id).IsEmpty;
        }

        /// <summary>Frames of one execution, spread like a strike over its wind-up, killing blow and finish.</summary>
        public StrikeFrames ExecutionPhases(string id)
        {
            if (_executions != null)
            {
                foreach (ExecutionFrames entry in _executions)
                {
                    if (entry.Id == id)
                    {
                        return entry.Frames;
                    }
                }
            }

            return default;
        }
    }
}
