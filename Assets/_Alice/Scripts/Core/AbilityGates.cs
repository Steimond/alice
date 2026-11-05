namespace Alice.Core
{
    /// <summary>
    /// Ground that does not stay (a Cheshire bridge, GDD section 5): Alice may stand on it, but it never becomes
    /// the safe footing a fall returns her to.
    /// </summary>
    public interface ITemporaryGround
    {
    }

    /// <summary>
    /// A marked mechanism the clock (A04) can hold back (GDD section 5): it runs on its own clock, never on the
    /// global time scale.
    /// </summary>
    public interface IDelayableMechanism
    {
        bool IsDelayed { get; }
        void Delay(float seconds);
    }

    /// <summary>Alice made small by the mushroom (A02) at a marked passage; she grows back on her own.</summary>
    public interface IShrinkable
    {
        bool IsSmall { get; }
        void Shrink();
        void Grow();
    }
}
