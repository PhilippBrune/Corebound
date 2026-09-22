namespace Corebound.Gameplay.Bosses
{
    // Whether a boss is on the field right now. Lets systems that do not own the encounter (the
    // skill tree's Overwatch rule on towers) react to it without a reference to WaveManager.
    public static class BossEncounterState
    {
        public static bool IsActive { get; private set; }

        public static void SetActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
