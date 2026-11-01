using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>Trigger volume for pits and spikes; the touched body decides what happens.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class FallHazard : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Component owner = other.attachedRigidbody != null ? other.attachedRigidbody : other;
            if (owner.TryGetComponent(out IHazardTarget target))
            {
                target.OnHazardTouched();
            }
        }
    }
}
