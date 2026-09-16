using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Waves/Stage Reward Data", fileName = "StageRewardData")]
    public sealed class StageRewardData : ScriptableObject
    {
        [SerializeField] private string _completionLabel = "Stage Complete";
        [SerializeField] private string _transitionLabel = "Run Complete";
        [SerializeField] private int _completionResourceBonus;
        [SerializeField] private bool _endRunOnCompletion = true;

        public string CompletionLabel => _completionLabel;

        public string TransitionLabel => _transitionLabel;

        public int CompletionResourceBonus => Mathf.Max(0, _completionResourceBonus);

        public bool EndRunOnCompletion => _endRunOnCompletion;
    }
}