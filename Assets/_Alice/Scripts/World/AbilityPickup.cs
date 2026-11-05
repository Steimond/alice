using Alice.Abilities;
using UnityEngine;

namespace Alice.World
{
    /// <summary>Grants a permanent ability on touch. Persistence arrives with saves in M3.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class AbilityPickup : MonoBehaviour
    {
        [SerializeField] private string _abilityId = AbilityIds.Fan;

        public string AbilityId
        {
            get => _abilityId;
            set => _abilityId = value;
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Start()
        {
            // A loaded save may already hold this ability; the pickup is then long gone.
            Transform alice = Alice.Core.PlayerPresence.Current;
            if (alice != null && alice.TryGetComponent(out AbilityInventory inventory) && inventory.Has(_abilityId))
            {
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.attachedRigidbody != null
                && other.attachedRigidbody.TryGetComponent(out AbilityInventory inventory))
            {
                inventory.Unlock(_abilityId);
                gameObject.SetActive(false);
            }
        }
    }
}
