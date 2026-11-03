using UnityEngine;

namespace Alice.Player
{
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
    }
}
