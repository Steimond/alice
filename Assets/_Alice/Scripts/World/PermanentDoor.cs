using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// A shortcut door (GDD sections 4 and 10): closed, its body blocks the passage; the latch is on one side
    /// only, so Alice can open it with Interact once she has come round to that side. Opened, it stays open for
    /// good: a rest, a defeat or a reload never closes it again (world flag door:ID), and opening it is an
    /// autosave point.
    /// </summary>
    public sealed class PermanentDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _doorId = "D_DOOR";
        [Tooltip("+1: the latch is on the right; Alice must stand to the right of the door to open it.")]
        [SerializeField] private int _latchSide = 1;
        [Tooltip("The body that blocks the passage while the door is shut.")]
        [SerializeField] private Collider2D _body;
        [SerializeField] private SpriteRenderer _renderer;
        [Tooltip("Opening frames: the first is shut, the last stands open.")]
        [SerializeField] private Sprite[] _frames = new Sprite[0];
        [SerializeField, Min(0.02f)] private float _frameTime = 0.08f;

        private float _openedAt = float.NegativeInfinity;

        public string DoorId
        {
            get => _doorId;
            set => _doorId = value;
        }

        public string Flag => "door:" + _doorId;
        public bool IsOpen { get; private set; }

        public void Bind(int latchSide, Collider2D body, SpriteRenderer renderer, Sprite[] frames)
        {
            _latchSide = latchSide >= 0 ? 1 : -1;
            _body = body;
            _renderer = renderer;
            _frames = frames;
        }

        private void Start()
        {
            if (WorldFlags.Has(Flag))
            {
                SetOpen(true);
                _openedAt = float.NegativeInfinity;
            }

            Draw();
        }

        public bool CanInteract(Vector2 alicePosition)
        {
            int side = alicePosition.x >= transform.position.x ? 1 : -1;
            return !IsOpen && side == (_latchSide >= 0 ? 1 : -1);
        }

        public void Interact(GameObject alice)
        {
            if (IsOpen)
            {
                return;
            }

            SetOpen(true);
            _openedAt = Time.time;
            WorldFlags.Raise(Flag);
            Autosave.Request();
        }

        private void SetOpen(bool open)
        {
            IsOpen = open;
            if (_body != null)
            {
                _body.enabled = !open;
            }
        }

        private void Update()
        {
            Draw();
        }

        private void Draw()
        {
            if (_renderer == null || _frames == null || _frames.Length == 0)
            {
                return;
            }

            int frame = 0;
            if (IsOpen)
            {
                frame = float.IsNegativeInfinity(_openedAt)
                    ? _frames.Length - 1
                    : Mathf.Min(_frames.Length - 1, Mathf.FloorToInt((Time.time - _openedAt) / _frameTime));
            }

            _renderer.sprite = _frames[frame];
        }
    }
}
