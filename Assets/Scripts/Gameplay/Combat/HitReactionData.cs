using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Combat/Hit Reaction", fileName = "HitReaction")]
    public sealed class HitReactionData : ScriptableObject
    {
        [SerializeField] private float _flashDurationSeconds = 0.08f;
        [SerializeField] private float _flashIntensity = 0.7f;
        [SerializeField] private float _knockbackDistance = 0.2f;

        public float FlashDurationSeconds => _flashDurationSeconds;

        public float FlashIntensity => _flashIntensity;

        public float KnockbackDistance => _knockbackDistance;
    }
}