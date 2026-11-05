using Alice.Abilities;
using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// A little door (A02, GDD 0.26): Interact in front of it bites the mushroom, Alice shrinks and the door swings
    /// open. Opened once, it stays open for good on both sides of the passage (world flag door:ID, autosave);
    /// getting through still takes being small, which the doorway checks. Without the mushroom nothing happens.
    /// </summary>
    public sealed class LittleDoor : MonoBehaviour, IInteractable
    {
        [Tooltip("Shared by both sides of the passage, so opening it on one opens it on the other.")]
        [SerializeField] private string _doorId = "LD_DOOR";
        [SerializeField] private SpriteRenderer _renderer;
        [Tooltip("Opening frames: the first is shut, the last stands open.")]
        [SerializeField] private Sprite[] _frames = new Sprite[0];
        [SerializeField, Min(0.02f)] private float _frameTime = 0.1f;
        [Tooltip("The door starts to open this long after the bite, as she finishes shrinking, in seconds.")]
        [SerializeField, Min(0f)] private float _openDelay = 1f;

        private float _openingAt = float.NegativeInfinity;

        public string DoorId
        {
            get => _doorId;
            set => _doorId = value;
        }

        public string Flag => "door:" + _doorId;
        public bool IsOpen { get; private set; }

        public void Bind(SpriteRenderer renderer, Sprite[] frames)
        {
            _renderer = renderer;
            _frames = frames;
        }

        private void Start()
        {
            IsOpen = WorldFlags.Has(Flag);
            Draw();
        }

        private void Update()
        {
            Draw();
        }

        public bool CanInteract(Vector2 alicePosition)
        {
            Transform alice = PlayerPresence.Current;
            return AbilityCheck.AliceHas(AbilityIds.Mushroom) && alice != null
                && alice.TryGetComponent(out IShrinkable size) && !size.IsSmall;
        }

        public void Interact(GameObject alice)
        {
            if (!alice.TryGetComponent(out IShrinkable size))
            {
                return;
            }

            size.Shrink();
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            _openingAt = Time.time + _openDelay;
            WorldFlags.Raise(Flag);
            Autosave.Request();
        }

        private void Draw()
        {
            if (_renderer == null || _frames == null || _frames.Length == 0)
            {
                return;
            }

            int last = _frames.Length - 1;
            int frame = 0;
            if (IsOpen)
            {
                // Opened in an earlier visit, _openingAt is minus infinity: compared as time first, since an
                // infinite time would overflow as a frame index.
                float elapsed = Time.time - _openingAt;
                frame = elapsed < 0f ? 0
                    : elapsed >= last * _frameTime ? last
                    : Mathf.FloorToInt(elapsed / _frameTime);
            }

            _renderer.sprite = _frames[frame];
        }
    }
}
