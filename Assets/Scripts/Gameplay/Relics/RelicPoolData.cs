using UnityEngine;

namespace Corebound.Gameplay.Relics
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Relics/Relic Pool Data", fileName = "RelicPoolData")]
    public sealed class RelicPoolData : ScriptableObject
    {
        [SerializeField] private RelicData[] _relics;

        public RelicData[] Relics => _relics;
    }
}
