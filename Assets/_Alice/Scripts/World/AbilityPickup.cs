using Alice.Abilities;
using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// A bundle with a permanent ability (GDD 0.28): Interact opens it, the ability is Alice's for good and the
    /// bundle is gone. A loaded save that already holds the ability hides the bundle.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class AbilityPickup : MonoBehaviour, IInteractable
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
            if (AbilityCheck.AliceHas(_abilityId))
            {
                gameObject.SetActive(false);
            }
        }

        public bool CanInteract(Vector2 alicePosition)
        {
            return !AbilityCheck.AliceHas(_abilityId);
        }

        public void Interact(GameObject alice)
        {
            if (!alice.TryGetComponent(out AbilityInventory inventory) || inventory.Has(_abilityId))
            {
                return;
            }

            // The ability is hers (and saved) at once; the bundle goes when her hand takes it.
            inventory.Unlock(_abilityId);
            BundleOpening.Begin(this, alice, _abilityId);
        }
    }
}
