using UnityEngine;

namespace Corebound.SaveSystem
{
    [CreateAssetMenu(menuName = "Corebound/Save System/Run Progression Checkpoint Data", fileName = "RunProgressionCheckpointData")]
    public sealed class RunProgressionCheckpointData : ScriptableObject
    {
        [SerializeField] private int _stageIndex = 1;
        [SerializeField] private int _waveIndex = 1;
        [SerializeField] private int _rewardWalletIncrement = 5;
        [SerializeField] private string _pendingRewardId = "reward_card_offer";
        [SerializeField] private string _pendingRewardLabel = "Card Reward Available";
        [SerializeField] private float _coreHealthNormalized = 1f;

        public int StageIndex => _stageIndex;

        public int WaveIndex => _waveIndex;

        public int RewardWalletIncrement => _rewardWalletIncrement;

        public string PendingRewardId => _pendingRewardId;

        public string PendingRewardLabel => _pendingRewardLabel;

        public float CoreHealthNormalized => _coreHealthNormalized;
    }
}