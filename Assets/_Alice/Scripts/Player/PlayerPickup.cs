using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Alice picking a bundle up (GDD 0.28): she turns to it, stands where her drawn hand meets it, bends, takes it,
    /// looks at it and tucks it into her apron. She is held still for the whole gesture; the bundle stays on the floor
    /// until the grab, which is when the find is shown.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerPickup : MonoBehaviour, IItemPicker
    {
        [Tooltip("The whole gesture, in seconds; the pickup frames are spread over it.")]
        [SerializeField, Min(0.2f)] private float _duration = 1.3f;
        [Tooltip("When her hand closes on the bundle, in seconds from the start.")]
        [SerializeField, Min(0f)] private float _grabTime = 0.35f;
        [Tooltip("How far in front of her feet the frames put the bundle, in tiles.")]
        [SerializeField, Min(0f)] private float _reach = 0.7f;

        private PlayerMotor _motor;
        private float _startedAt = float.NegativeInfinity;

        public bool IsPickingUp => Time.time - _startedAt < _duration;
        public float Progress => Mathf.Clamp01((Time.time - _startedAt) / _duration);

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
        }

        public float BeginPickUp(Vector2 itemFoot)
        {
            int facing = itemFoot.x >= _motor.Position.x ? 1 : -1;
            _motor.Face(facing);
            _motor.Teleport(new Vector2(itemFoot.x - facing * _reach, _motor.Position.y));
            _motor.Lock(_duration);
            _startedAt = Time.time;
            return _grabTime;
        }
    }
}
