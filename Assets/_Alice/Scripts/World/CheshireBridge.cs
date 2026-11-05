using Alice.Abilities;
using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// A Cheshire surface (A03, GDD section 5): a bridge or platform that is always seen as a shimmering outline and
    /// is never there to stand on, until Alice has the Cheshire Step. Then it turns solid by itself whenever she
    /// comes near, and stays solid while she is on it or close by; no marker, no button (owner's choice of
    /// 5 Nov 2026, GDD 0.25). It fades back only after she has moved away, so it never vanishes under her feet. It is
    /// still never safe footing: a fall from it returns her to the bank.
    /// </summary>
    public sealed class CheshireBridge : MonoBehaviour, ITemporaryGround
    {
        [SerializeField] private Collider2D _surface;
        [SerializeField] private SpriteRenderer _renderer;
        [Tooltip("How close Alice's feet must come to the surface, in tiles, for it to turn solid.")]
        [SerializeField, Min(0.5f)] private float _reach = 3f;
        [Tooltip("How long it stays solid after she has moved out of reach, in seconds.")]
        [SerializeField, Min(0f)] private float _linger = 0.4f;
        [Tooltip("The outline's opacity shimmers between these while it is not solid.")]
        [SerializeField, Range(0f, 1f)] private float _ghostMin = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _ghostMax = 0.35f;
        [SerializeField, Min(0.1f)] private float _shimmerPeriod = 1.4f;
        [Tooltip("Opacity per second while it turns solid or fades back.")]
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 6f;

        private float _solidUntil = float.NegativeInfinity;
        private float _alpha;
        private Rect _area;

        public bool IsShown => Time.time < _solidUntil;

        public void Bind(Collider2D surface, SpriteRenderer renderer)
        {
            _surface = surface;
            _renderer = renderer;
        }

        private void Start()
        {
            _area = SurfaceArea();
            _alpha = _ghostMin;
            Apply(true);
        }

        private void Update()
        {
            if (AliceWithinReach())
            {
                _solidUntil = Time.time + _linger;
            }

            Apply(false);
        }

        private bool AliceWithinReach()
        {
            Transform alice = PlayerPresence.Current;
            if (alice == null || !alice.TryGetComponent(out AbilityInventory inventory)
                || !inventory.Has(AbilityIds.CheshireStep))
            {
                return false;
            }

            Vector2 feet = alice.position;
            Vector2 nearest = new Vector2(Mathf.Clamp(feet.x, _area.xMin, _area.xMax), Mathf.Clamp(feet.y, _area.yMin, _area.yMax));
            return (feet - nearest).sqrMagnitude <= _reach * _reach;
        }

        private void Apply(bool immediate)
        {
            bool shown = IsShown;
            if (_surface != null)
            {
                _surface.enabled = shown;
            }

            if (_renderer == null)
            {
                return;
            }

            float shimmer = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / _shimmerPeriod);
            float target = shown ? 1f : Mathf.Lerp(_ghostMin, _ghostMax, shimmer);
            _alpha = immediate ? target : Mathf.MoveTowards(_alpha, target, _fadeSpeed * Time.deltaTime);
            Color color = _renderer.color;
            color.a = _alpha;
            _renderer.color = color;
        }

        /// <summary>The surface in world space, read from the box itself: a disabled collider reports no bounds.</summary>
        private Rect SurfaceArea()
        {
            if (_surface is BoxCollider2D box)
            {
                Vector2 centre = box.transform.TransformPoint(box.offset);
                Vector2 size = Vector2.Scale(box.size, box.transform.lossyScale);
                return new Rect(centre - size * 0.5f, size);
            }

            Vector2 position = transform.position;
            return new Rect(position, Vector2.zero);
        }
    }
}
