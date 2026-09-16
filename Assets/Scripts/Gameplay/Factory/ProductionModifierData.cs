using UnityEngine;

namespace Corebound.Gameplay.Factory
{
    // Snowball-prevention rules for TowerPulse nodes (Docs/07_DESIGN_DECISIONS.md, Factory
    // section): "doppelte Resource-Produzenten verlieren nach den ersten starken Kopien an
    // Effizienz". A player who builds three of the same producing tower gets the first copy at
    // full strength and diminishing value from each further copy, rather than linear stacking.
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Factory/Production Modifier Data", fileName = "ProductionModifierData")]
    public sealed class ProductionModifierData : ScriptableObject
    {
        [SerializeField] [Range(0f, 1f)] private float _duplicateProducerEfficiencyMultiplier = 0.5f;
        [SerializeField] private int _maxDuplicateProducersCounted = 4;

        // Multiplier applied per additional identical active producer beyond the first (e.g. the
        // 2nd copy of the same tower contributes at this fraction of a full copy, the 3rd at this
        // fraction squared, and so on).
        public float DuplicateProducerEfficiencyMultiplier => Mathf.Clamp01(_duplicateProducerEfficiencyMultiplier);

        // Hard ceiling: producer copies beyond this count contribute nothing further at all.
        public int MaxDuplicateProducersCounted => Mathf.Max(1, _maxDuplicateProducersCounted);

        // Effective producer "weight" for a given number of identical active copies, as a
        // geometric falloff: 1 + m + m^2 + ... capped at MaxDuplicateProducersCounted copies.
        public float ResolveEffectiveProducerWeight(int producerCount)
        {
            var clampedCount = Mathf.Clamp(producerCount, 0, MaxDuplicateProducersCounted);
            var multiplier = DuplicateProducerEfficiencyMultiplier;
            var weight = 0f;
            var copyValue = 1f;

            for (var index = 0; index < clampedCount; index++)
            {
                weight += copyValue;
                copyValue *= multiplier;
            }

            return weight;
        }
    }
}
