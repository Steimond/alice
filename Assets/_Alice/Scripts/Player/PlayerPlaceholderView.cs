using UnityEngine;

namespace Alice.Player
{
    /// <summary>Grey-box body for M1: matches the collider and shows facing and slide invulnerability.</summary>
    public sealed class PlayerPlaceholderView : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Transform _facingMarker;
        [SerializeField] private Color _normalColor = new Color(0.92f, 0.9f, 0.85f);
        [SerializeField] private Color _invulnerableColor = new Color(0.55f, 0.8f, 1f);

        public void Bind(PlayerMotor motor, SpriteRenderer body, Transform facingMarker)
        {
            _motor = motor;
            _body = body;
            _facingMarker = facingMarker;
        }

        private void LateUpdate()
        {
            if (_motor == null || _motor.Settings == null || _body == null)
            {
                return;
            }

            Vector2 size = _motor.IsSliding ? TraversalMetrics.SlidingSize : TraversalMetrics.StandingSize;
            _body.size = size;
            _body.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            _body.color = _motor.IsInvulnerable ? _invulnerableColor : _normalColor;

            if (_facingMarker != null)
            {
                _facingMarker.localPosition = new Vector3(_motor.Facing * size.x * 0.5f, size.y * 0.8f, 0f);
            }
        }
    }
}
