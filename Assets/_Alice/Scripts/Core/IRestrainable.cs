using UnityEngine;

namespace Alice.Core
{
    /// <summary>
    /// Someone who can be caught and held, as the ambushing sheet wraps Alice (GDD section 9). Holding is a grab:
    /// it cannot be parried, and the captive breaks free by struggling.
    /// </summary>
    public interface IRestrainable
    {
        /// <summary>True while someone holds her.</summary>
        bool IsRestrained { get; }

        /// <summary>0 when caught, 1 when she breaks free.</summary>
        float StruggleProgress { get; }

        /// <summary>True when the last hold ended by her own struggle, not by the holder letting go or her defeat.</summary>
        bool BrokeFree { get; }

        /// <summary>
        /// Starts a hold at <paramref name="holdAt"/> (feet) unless she is immune right now: sliding through, already
        /// held, defeated. <paramref name="pressesToEscape"/> is how many presses free her.
        /// </summary>
        bool TryRestrain(GameObject by, int pressesToEscape, Vector2 holdAt);

        /// <summary>Ends the hold from the holder's side, e.g. when it dies or its grip runs out.</summary>
        void Release(GameObject by);
    }
}
