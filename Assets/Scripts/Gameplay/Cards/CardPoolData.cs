using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Cards/Card Pool Data", fileName = "CardPoolData")]
    public sealed class CardPoolData : ScriptableObject
    {
        [SerializeField] private CardData[] _cards;

        // How the pool's tags are drawn. Held here so the sheet ships with the cards and is
        // registered for CardTagIconAtlas, which is static and has no reference of its own.
        [SerializeField] private Corebound.Gameplay.Shared.SpriteSheetData _tagIconSheet;

        public CardData[] Cards => _cards;

        public Corebound.Gameplay.Shared.SpriteSheetData TagIconSheet => _tagIconSheet;

        private void OnEnable()
        {
            Corebound.Gameplay.Shared.SpriteSheetData.Register(_tagIconSheet);
        }
    }
}