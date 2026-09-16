namespace Corebound.Gameplay.Waves
{
    public readonly struct StageEventArgs
    {
        public StageEventArgs(int stageIndex, string stageId, string stageLabel, string detailLabel, bool isFinalStage)
        {
            StageIndex = stageIndex;
            StageId = stageId;
            StageLabel = stageLabel;
            DetailLabel = detailLabel;
            IsFinalStage = isFinalStage;
        }

        public int StageIndex { get; }

        public string StageId { get; }

        public string StageLabel { get; }

        public string DetailLabel { get; }

        public bool IsFinalStage { get; }
    }
}