using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Waves/Wave Reward Data", fileName = "WaveRewardData")]
    public sealed class WaveRewardData : ScriptableObject
    {
        [SerializeField] private string _rewardId = "reward_card_offer";
        [SerializeField] private string _rewardLabel = "Card Reward Available";
        [SerializeField] private string _rewardLabelDe = "Kartenbelohnung verfügbar";
        [SerializeField] private int _completionResourceBonus = 3;

        public string RewardId => _rewardId;

        public string RewardLabel => Corebound.UI.LocalizationService.Resolve(_rewardLabel, _rewardLabelDe);

        public int CompletionResourceBonus => _completionResourceBonus;
    }
}