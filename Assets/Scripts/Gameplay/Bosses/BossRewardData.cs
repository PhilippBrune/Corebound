using UnityEngine;

namespace Corebound.Gameplay.Bosses
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Bosses/Boss Reward Data", fileName = "BossRewardData")]
    public sealed class BossRewardData : ScriptableObject
    {
        [SerializeField] private string _rewardId = "reward_boss_card_offer";
        [SerializeField] private string _rewardLabel = "Boss Reward Available";
        [SerializeField] private string _rewardLabelDe = "Boss-Belohnung verfügbar";
        [SerializeField] private int _completionResourceBonus = 8;

        public string RewardId => _rewardId;

        public string RewardLabel => Corebound.UI.LocalizationService.Resolve(_rewardLabel, _rewardLabelDe);

        public int CompletionResourceBonus => Mathf.Max(0, _completionResourceBonus);
    }
}