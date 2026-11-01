namespace Alice.Core
{
    /// <summary>Receives environmental hazards such as pits and spikes.</summary>
    public interface IHazardTarget
    {
        void OnHazardTouched();
    }
}
