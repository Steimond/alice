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

        [Tooltip("Run frame shown in the air until jump frames exist.")]
        [SerializeField, Min(0)] private int _airRunFrame = 3;

        public Sprite Idle => _idle;
        public Sprite[] Run => _run;
        public float RunFrameDistance => _runFrameDistance;
        public float RunMinSpeed => _runMinSpeed;
        public int AirRunFrame => _airRunFrame;

        public void Assign(Sprite idle, Sprite[] run)
        {
            _idle = idle;
            _run = run;
        }
    }
}
