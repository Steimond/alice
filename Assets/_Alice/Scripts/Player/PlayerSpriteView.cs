using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Draws Alice from <see cref="PlayerSpriteSet"/>: standing, running and, until jump frames exist,
    /// a run frame in the air. Sliding has no frames yet, so the grey-box body takes over for it.
    /// </summary>
    public sealed class PlayerSpriteView : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerSpriteSet _sprites;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private SpriteRenderer _placeholderBody;
        [SerializeField] private SpriteRenderer _placeholderMarker;

        private float _runDistance;

        public void Bind(PlayerMotor motor, PlayerSpriteSet sprites, SpriteRenderer renderer,
            SpriteRenderer placeholderBody, SpriteRenderer placeholderMarker)
        {
            _motor = motor;
            _sprites = sprites;
            _renderer = renderer;
            _placeholderBody = placeholderBody;
            _placeholderMarker = placeholderMarker;
        }

        private void LateUpdate()
        {
            if (_motor == null || _sprites == null || _renderer == null)
            {
                return;
            }

            bool drawn = !_motor.IsSliding;
            _renderer.enabled = drawn;
            if (_placeholderBody != null)
            {
                _placeholderBody.enabled = !drawn;
            }

            if (_placeholderMarker != null)
            {
                _placeholderMarker.enabled = !drawn;
            }

            if (!drawn)
            {
                _runDistance = 0f;
                return;
            }

            _renderer.flipX = _motor.Facing < 0;
            _renderer.sprite = SelectSprite();
        }

        private Sprite SelectSprite()
        {
            Sprite[] run = _sprites.Run;
            if (run == null || run.Length == 0)
            {
                return _sprites.Idle;
            }

            if (!_motor.IsGrounded)
            {
                return run[Mathf.Clamp(_sprites.AirRunFrame, 0, run.Length - 1)];
            }

            float speed = Mathf.Abs(_motor.Velocity.x);
            if (speed < _sprites.RunMinSpeed)
            {
                _runDistance = 0f;
                return _sprites.Idle;
            }

            _runDistance += speed * Time.deltaTime;
            int frame = Mathf.FloorToInt(_runDistance / _sprites.RunFrameDistance) % run.Length;
            return run[frame];
        }
    }
}
