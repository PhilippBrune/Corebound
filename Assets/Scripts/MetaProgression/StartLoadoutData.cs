using UnityEngine;

namespace Corebound.MetaProgression
{
    [CreateAssetMenu(menuName = "Corebound/Meta Progression/Start Loadout Data", fileName = "StartLoadoutData")]
    public sealed class StartLoadoutData : ScriptableObject
    {
        [SerializeField] private string _loadoutId = "loadout_default";
        [SerializeField] private string _displayName = "Loadout";
        [SerializeField] [TextArea(2, 3)] private string _summary = string.Empty;
        [SerializeField] private string _startingWeaponId = string.Empty;
        [SerializeField] private string[] _startingTowerIds;
        [SerializeField] private string[] _startingCardIds;

        public string LoadoutId => _loadoutId;

        public string DisplayName => _displayName;

        public string Summary => _summary;

        public string StartingWeaponId => _startingWeaponId;

        public string[] StartingTowerIds => _startingTowerIds;

        public string[] StartingCardIds => _startingCardIds;
    }
}