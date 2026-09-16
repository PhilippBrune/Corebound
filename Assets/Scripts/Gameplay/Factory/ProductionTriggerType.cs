namespace Corebound.Gameplay.Factory
{
    // Deliberately limited to the three triggers the first vertical slice scopes in
    // (Docs/07_DESIGN_DECISIONS.md, Factory section). ResourceConversion and any trigger beyond
    // these three (e.g. OnCardApplied) are explicitly out of scope for this slice.
    public enum ProductionTriggerType
    {
        EnemyKilled,
        TowerPulse,
        WaveCompleted,
    }
}
