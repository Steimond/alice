namespace Alice.Core
{
    /// <summary>
    /// Alice's side of <see cref="IBodyBlocker"/>: while this is true (the slide, GDD section 4) she goes
    /// through enemy bodies and their touch does not hurt her.
    /// </summary>
    public interface IBodyPasser
    {
        bool PassesBodies { get; }
    }
}
