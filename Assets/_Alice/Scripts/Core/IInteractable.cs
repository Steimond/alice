using UnityEngine;

namespace Alice.Core
{
    /// <summary>
    /// Something in the world Alice uses with Interact while standing next to it: a door's latch, later levers
    /// and people. Its trigger collider marks where she can reach it; it decides from where it can be used.
    /// </summary>
    public interface IInteractable
    {
        bool CanInteract(Vector2 alicePosition);
        void Interact(GameObject alice);
    }
}
