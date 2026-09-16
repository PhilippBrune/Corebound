using UnityEngine;

namespace Corebound.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Enemies/Enemy Reward Data", fileName = "EnemyRewardData")]
    public sealed class EnemyRewardData : ScriptableObject
    {
        [SerializeField] private int _resourceRewardAmount = 2;

        public int ResourceRewardAmount => Mathf.Max(0, _resourceRewardAmount);
    }
}