using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // Core.md's documented Data Model: "CoreData: Basis-Health, Praesentationsreferenzen und
    // Kernregeln". Health/presentation are already fully covered by CombatTargetData (used
    // identically for enemies, bosses and the Core), so this wraps a reference to that asset
    // instead of duplicating its fields, and adds the one Core-specific rule the spec's Technical
    // Design section actually needs: the health fraction at which OnCoreCritical fires.
    [CreateAssetMenu(fileName = "CoreData", menuName = "Corebound/Combat/Core Data")]
    public sealed class CoreData : ScriptableObject
    {
        [SerializeField] private string _displayName = "Core";
        [SerializeField] private CombatTargetData _combatTargetData;
        [SerializeField, Range(0f, 1f)] private float _criticalHealthThreshold = 0.35f;

        public string DisplayName => _displayName;

        public CombatTargetData CombatTargetData => _combatTargetData;

        public float CriticalHealthThreshold => _criticalHealthThreshold;
    }
}
