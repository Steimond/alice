namespace Alice.Core
{
    /// <summary>
    /// A body Alice cannot walk through while it stands (a living enemy, GDD section 4). Its collider stays
    /// a trigger so the slide and weapons pass it; her motor treats it as a wall only on foot.
    /// </summary>
    public interface IBodyBlocker
    {
        bool BlocksBodies { get; }
    }
}
