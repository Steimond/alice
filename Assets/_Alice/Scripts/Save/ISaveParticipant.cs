namespace Alice.Save
{
    /// <summary>A system on Alice that puts its part into a save and takes it back on load.</summary>
    public interface ISaveParticipant
    {
        void Capture(SaveData data);
        void Restore(SaveData data);
    }
}
