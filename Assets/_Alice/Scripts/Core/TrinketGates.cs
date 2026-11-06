namespace Alice.Core
{
    /// <summary>
    /// Who keeps pocket trinkets (GDD section 10): a cache in the world hands its trinket over through this, without
    /// knowing about Alice's components.
    /// </summary>
    public interface ITrinketReceiver
    {
        bool HasTrinket(string trinketId);
        void ReceiveTrinket(string trinketId);
    }

    /// <summary>Changes the damage a body takes before it reaches its health (a worn thimble, GDD section 10).</summary>
    public interface IDamageTakenModifier
    {
        int ModifyDamage(int damage);
    }
}
