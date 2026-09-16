namespace Corebound.Gameplay.Relics
{
    // Tracks a per-run counter maintained by CardService. Values are checked against
    // RelicData.EvolutionThreshold each time the matching counter increments.
    public enum RelicEvolutionCondition
    {
        None,
        RangedEnemiesKilled,
        WavesCleared
    }
}
