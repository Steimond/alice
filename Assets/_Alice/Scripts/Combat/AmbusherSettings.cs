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
        [Tooltip("It twitches while Alice is this close sideways: the tell. A thrust (2.75) cuts it from outside.")]
        [SerializeField, Min(0f)] private float _triggerRange = 2.2f;
        [Tooltip("One twitch; it repeats while she stays near.")]
        [SerializeField, Min(0f)] private float _tremble = 0.5f;
        [Tooltip("The fall onto her; she is already held.")]
        [SerializeField, Min(0.05f)] private float _drop = 0.3f;
        [Tooltip("Whoever steps this close under it is caught for certain.")]
        [SerializeField, Min(0f)] private float _wrapRange = 1.1f;
        [Tooltip("Body while it hangs: width and height of the cloth.")]
        [SerializeField] private Vector2 _hangingBody = new Vector2(2.2f, 4.6f);

        [Header("Hold")]
        [Tooltip("Presses of attack, jump, slide or parry that tear Alice free; then she rips the sheet apart.")]
        [SerializeField, Min(1)] private int _pressesToEscape = 7;
        [SerializeField, Min(0.1f)] private float _squeezeInterval = 0.7f;
        [SerializeField, Min(1)] private int _squeezeDamage = 8;

        [Tooltip("Body of the dead cloth on the floor.")]
        [SerializeField] private Vector2 _heapBody = new Vector2(2.6f, 0.8f);

        public float TriggerRange => _triggerRange;
        public float Tremble => _tremble;
        public float Drop => _drop;
        public float WrapRange => _wrapRange;
        public Vector2 HangingBody => _hangingBody;
        public int PressesToEscape => _pressesToEscape;
        public float SqueezeInterval => _squeezeInterval;
        public int SqueezeDamage => _squeezeDamage;
        public Vector2 HeapBody => _heapBody;
    }
}
