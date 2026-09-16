namespace Corebound.Gameplay.Waves
{
    public readonly struct WaveProgressEventArgs
    {
        public WaveProgressEventArgs(int stageIndex, int waveIndex, string label, bool isEliteEncounter)
        {
            StageIndex = stageIndex;
            WaveIndex = waveIndex;
            Label = label;
            IsEliteEncounter = isEliteEncounter;
        }

        public int StageIndex { get; }

        public int WaveIndex { get; }

        public string Label { get; }

        // Sourced directly from WaveModifierData.IsEliteEncounter rather than left for
        // consumers to infer by matching the word "Elite" inside the display label string.
        public bool IsEliteEncounter { get; }
    }
}