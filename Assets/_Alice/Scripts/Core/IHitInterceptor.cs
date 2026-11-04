using UnityEngine;

namespace Alice.Core
{
    /// <summary>Gets the first look at a blow and may stop it before it does damage, as a parry does.</summary>
    public interface IHitInterceptor
    {
        /// <summary>Returns true when the blow is stopped and must not land.</summary>
        bool TryIntercept(HitInfo hit);
    }

    /// <summary>An attacker that can be thrown open by a parry (GDD section 4).</summary>
    public interface IParryable
    {
        void OnParried(GameObject by);
    }
}
