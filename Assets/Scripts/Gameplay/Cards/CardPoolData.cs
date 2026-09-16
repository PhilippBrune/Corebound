using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Cards/Card Pool Data", fileName = "CardPoolData")]
    public sealed class CardPoolData : ScriptableObject
    {
        [SerializeField] private CardData[] _cards;

        public CardData[] Cards => _cards;
    }
}