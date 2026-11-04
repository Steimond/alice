using Alice.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Alice.UI
{
    /// <summary>
    /// Black over the whole screen while Alice is defeated: it falls as she collapses and lifts as she wakes.
    /// The overlay canvas is built at runtime, so the scenes and the Alice prefab need no UI objects.
    /// </summary>
    [RequireComponent(typeof(PlayerDamageReaction))]
    public sealed class ScreenFade : MonoBehaviour
    {
        private const int SortingOrder = 1000;

        private PlayerDamageReaction _damage;
        private Image _image;

        private void Awake()
        {
            _damage = GetComponent<PlayerDamageReaction>();

            GameObject overlay = new GameObject("ScreenFade", typeof(Canvas), typeof(Image));
            Canvas canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            _image = overlay.GetComponent<Image>();
            _image.color = Color.clear;
            _image.raycastTarget = false;
            RectTransform rect = _image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (_image != null)
            {
                Destroy(_image.gameObject);
            }
        }

        private void LateUpdate()
        {
            float darkness = _damage.Darkness;
            _image.enabled = darkness > 0f;
            _image.color = new Color(0f, 0f, 0f, darkness);
        }
    }
}
