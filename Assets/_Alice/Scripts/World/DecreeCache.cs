using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// Shows the decrees Alice dropped when she fell (GDD section 4) where they lie in this room: a daisy whose
    /// petals are rolled decrees. Picking them up is Alice's own action (Interact next to it), so this only
    /// shows and hides the flower. One per room is enough: there is only ever one loss.
    /// </summary>
    public sealed class DecreeCache : MonoBehaviour
    {
        [Tooltip("The visible daisy; hidden while there is nothing here.")]
        [SerializeField] private GameObject _visual;

        private IDecreeLoss _loss;

        public bool IsShown { get; private set; }

        private void FixedUpdate()
        {
            Transform alice = PlayerPresence.Current;
            if (_loss == null && alice != null)
            {
                alice.TryGetComponent(out _loss);
            }

            bool here = _loss != null && _loss.HasLoss && _loss.LossRoomId == RoomPresence.CurrentId;
            if (here)
            {
                transform.position = _loss.LossPosition;
            }

            IsShown = here;
            if (_visual != null && _visual.activeSelf != here)
            {
                _visual.SetActive(here);
            }
        }
    }
}
