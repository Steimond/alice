namespace Alice.Core
{
    /// <summary>Anything a weapon can hit. Attackers find it on the struck body and know nothing else about it.</summary>
    public interface IDamageable
    {
        /// <summary>Returns false when the blow did not land: already dead, invulnerable or dodging.</summary>
        bool ReceiveHit(HitInfo hit);
    }
}
