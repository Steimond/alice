using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// A bundle with one pocket trinket (GDD section 10, 0.28): Interact opens it, and it is gone for good, since
    /// Alice keeps the trinket in her save. Worn only from the next rest.
    /// </summary>
    public sealed class TrinketCache : MonoBehaviour, IInteractable
    {
        [Tooltip("Trinket ID (T01...), as in its TrinketDefinition.")]
        [SerializeField] private string _trinketId = "T00";

        public string TrinketId
        {
            get => _trinketId;
            set => _trinketId = value;
        }

        private void Start()
        {
            if (AliceHasIt())
            {
                gameObject.SetActive(false);
            }
        }

        public bool CanInteract(Vector2 alicePosition)
        {
            return !AliceHasIt();
        }

        public void Interact(GameObject alice)
        {
            if (!alice.TryGetComponent(out ITrinketReceiver receiver) || receiver.HasTrinket(_trinketId))
            {
                return;
            }

            receiver.ReceiveTrinket(_trinketId);
            BundleOpening.Begin(this, alice, _trinketId);
        }

        private bool AliceHasIt()
        {
            Transform alice = PlayerPresence.Current;
            return alice != null && alice.TryGetComponent(out ITrinketReceiver receiver) && receiver.HasTrinket(_trinketId);
        }
    }
}
