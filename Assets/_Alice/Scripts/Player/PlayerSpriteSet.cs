using System;
using Alice.Combat;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>Alice's frames for one enemy type's execution.</summary>
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

        [Header("Executions, by the enemy type's execution ID")]
        [SerializeField] private ExecutionFrames[] _executions = new ExecutionFrames[0];

        [Header("Parry: guard, deflect, hold, lower")]
        [SerializeField] private Sprite[] _parry = new Sprite[0];
        [Tooltip("After a caught blow the deflect frame shows this long, then the hold.")]
        [SerializeField, Min(0f)] private float _parryDeflectTime = 0.1f;
        [Tooltip("The guard frame shows this long when nothing is caught; the catch itself is timed by each blow.")]
        [SerializeField, Min(0f)] private float _parryGuardTime = 0.15f;

        [Header("Hurt, defeat and waking")]
        [Tooltip("Flinch after a hit, spread over the hurt time.")]
        [SerializeField] private Sprite[] _hurt = new Sprite[0];
        [Tooltip("Collapse, spread over the fall time; the last frame holds into the darkness.")]
        [SerializeField] private Sprite[] _defeat = new Sprite[0];
        [Tooltip("The gasp at the rest and getting up, spread over the wake time.")]
        [SerializeField] private Sprite[] _wake = new Sprite[0];

        [Tooltip("Smelling salts: the vial to the nose, the sniff, the jolt; spread over the use.")]
        [SerializeField] private Sprite[] _salts = new Sprite[0];

        public Sprite[] Salts => _salts;

        public void AssignSalts(Sprite[] salts)
        {
            _salts = salts;
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
        public bool HasParry => _parry != null && _parry.Length >= ParryFrameCount;
        public const int ParryFrameCount = 4;
        public StrikeFrames Slash => _slash;
        public StrikeFrames AirSlash => _airSlash;

        /// <summary>Frames for a phase of a strike; empty when that strike has no frames yet.</summary>
        public Sprite[] StrikePhase(PlayerStrike strike, AttackPhase phase)
        {
            // The counter is a deeper thrust: the same frames, held longer by its recovery.
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

        public void AssignSlashes(StrikeFrames slash, StrikeFrames airSlash)
        {
            _slash = slash;
            _airSlash = airSlash;
        }

        public void AssignParry(Sprite[] parry)
        {
            _parry = parry;
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
