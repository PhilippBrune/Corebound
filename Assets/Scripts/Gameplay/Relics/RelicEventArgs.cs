namespace Corebound.Gameplay.Relics
{
    public readonly struct RelicGrantedEventArgs
    {
        public RelicGrantedEventArgs(RelicData relic, bool isEvolution)
        {
            Relic = relic;
            IsEvolution = isEvolution;
        }

        public RelicData Relic { get; }

        // True when this fires because an already-owned relic evolved into its upgraded effect
        // (RelicData.IsEvolving), false for a freshly granted relic (CardService.TryGrantRandomRelic).
        public bool IsEvolution { get; }
    }
}
