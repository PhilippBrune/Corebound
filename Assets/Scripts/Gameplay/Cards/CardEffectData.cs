using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Cards/Card Effect Data", fileName = "CardEffectData")]
    public sealed class CardEffectData : ScriptableObject
    {
        [SerializeField] private CardEffectTarget _target;
        [SerializeField] private CardEffectOperation _operation = CardEffectOperation.Multiply;
        [SerializeField] private float _value = 1f;

        public CardEffectTarget Target => _target;

        public CardEffectOperation Operation => _operation;

        public float Value => _value;
    }
}