namespace Corebound.Gameplay.Bosses
{
    // Whether a boss is on the field right now. Lets systems that do not own the encounter (the
    // skill tree's Overwatch rule on towers) react to it without a reference to WaveManager.
    public static class BossEncounterState
    {
        public static bool IsActive { get; private set; }

        // True while the Boss HUD shows "Boss Defeated". The relic reveal waits for it to fade
        // (Open Topics 5 (b), 2026-09-30): both used to appear at once, one on top of the other.
        public static bool IsDefeatNoticeShowing { get; set; }

        public static void SetActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
