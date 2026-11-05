using Alice.Abilities;
using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// A clock face on a marked mechanism (A04): Interact here holds the mechanism back for a while; it can be used
    /// again once the clock has rewound.
    /// </summary>
    public sealed class ClockMarker : MonoBehaviour, IInteractable
    {
        [SerializeField] private MonoBehaviour _mechanism;
        [SerializeField, Min(0.5f)] private float _delayTime = 5f;

        public void Bind(MonoBehaviour mechanism, float delayTime)
        {
            _mechanism = mechanism;
            _delayTime = delayTime;
        }

        private IDelayableMechanism Mechanism => _mechanism as IDelayableMechanism;

        public bool CanInteract(Vector2 alicePosition)
        {
            return Mechanism != null && !Mechanism.IsDelayed && AbilityCheck.AliceHas(AbilityIds.Clock);
        }

        public void Interact(GameObject alice)
        {
            Mechanism.Delay(_delayTime);
        }
    }
}
