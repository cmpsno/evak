namespace CODClone.Core
{
    /// <summary>
    /// Trivial story flags. Resets on play-mode exit — a save system is out of
    /// scope for the greybox pass. v0.1 tracks exactly one flag.
    /// </summary>
    public static class JobState
    {
        public static bool MookJobAssigned;
    }
}
