using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Something that hangs among the decor and drops on Alice (the laundry sheet EN13, GDD section 9). It hangs to
    /// the floor; the pivot is the floor under its middle. Distances in tiles.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Enemies/Ambusher Settings", fileName = "Ambusher")]
    public sealed class AmbusherSettings : EnemySettings
    {
        [Header("Ambush")]
        [Tooltip("It wakes when Alice comes this close sideways. A thrust (2.75) reaches it from outside.")]
        [SerializeField, Min(0f)] private float _triggerRange = 2.2f;
        [Tooltip("The twitching tell before it drops.")]
        [SerializeField, Min(0f)] private float _tremble = 0.5f;
        [SerializeField, Min(0.05f)] private float _drop = 0.4f;
        [Tooltip("Whoever stands this close sideways when it lands is wrapped.")]
        [SerializeField, Min(0f)] private float _wrapRange = 1.1f;
        [Tooltip("Body while it hangs: width and height of the cloth.")]
        [SerializeField] private Vector2 _hangingBody = new Vector2(2.2f, 4.6f);

        [Header("Hold")]
        [Tooltip("Presses of attack, jump, slide or parry that tear Alice free.")]
        [SerializeField, Min(1)] private int _pressesToEscape = 7;
        [SerializeField, Min(0.1f)] private float _squeezeInterval = 0.7f;
        [SerializeField, Min(1)] private int _squeezeDamage = 8;
        [Tooltip("It lets go by itself after this long.")]
        [SerializeField, Min(0.1f)] private float _maxHold = 4.5f;

        [Header("Heap")]
        [Tooltip("Lying on the floor it is open to every blow.")]
        [SerializeField, Min(0f)] private float _heapTime = 2.6f;
        [SerializeField, Min(0.05f)] private float _rise = 0.8f;
        [Tooltip("After it hangs again it waits before it can drop once more.")]
        [SerializeField, Min(0f)] private float _rearm = 1.5f;
        [SerializeField] private Vector2 _heapBody = new Vector2(2.6f, 0.8f);

        public float TriggerRange => _triggerRange;
        public float Tremble => _tremble;
        public float Drop => _drop;
        public float WrapRange => _wrapRange;
        public Vector2 HangingBody => _hangingBody;
        public int PressesToEscape => _pressesToEscape;
        public float SqueezeInterval => _squeezeInterval;
        public int SqueezeDamage => _squeezeDamage;
        public float MaxHold => _maxHold;
        public float HeapTime => _heapTime;
        public float Rise => _rise;
        public float Rearm => _rearm;
        public Vector2 HeapBody => _heapBody;
    }
}
