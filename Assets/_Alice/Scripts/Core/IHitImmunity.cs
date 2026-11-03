namespace Alice.Core
{
    /// <summary>Lets another system on the same body veto hits, e.g. the invulnerable frames of a slide.</summary>
    public interface IHitImmunity
    {
        bool IsImmune { get; }
    }
}
