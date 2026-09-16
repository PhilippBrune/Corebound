using UnityEngine;

namespace Corebound.Gameplay.Factory
{
    // A single producer within a ProductionChainData (Factory.md's "einzelner Erzeuger,
    // Verbraucher oder Verstärker"). The first vertical slice only ships producers - consumers
    // and amplifiers are a natural extension of this same class (a node with a negative
    // BaseAmount, or one that reads another node's recent output) but neither is needed yet, so
    // neither is built.
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Factory/Production Node Data", fileName = "ProductionNodeData")]
    public sealed class ProductionNodeData : ScriptableObject
    {
        [SerializeField] private string _nodeId = "node_default";
        [SerializeField] private string _displayName = "Production Node";
        [SerializeField] private ProductionTriggerType _triggerType = ProductionTriggerType.EnemyKilled;
        [SerializeField] private int _baseAmount = 1;
        [SerializeField] private int _maxAmountPerTrigger;

        [Header("Tower Pulse (only used when TriggerType == TowerPulse)")]
        [SerializeField] private float _pulseIntervalSeconds = 8f;
        [SerializeField] private string _requiredTowerId = string.Empty;

        public string NodeId => _nodeId;

        public string DisplayName => _displayName;

        public ProductionTriggerType TriggerType => _triggerType;

        public int BaseAmount => Mathf.Max(0, _baseAmount);

        // 0 means uncapped. Backs the design decision that wave-based production bonuses need a
        // hard data ceiling against runaway snowballing.
        public int MaxAmountPerTrigger => Mathf.Max(0, _maxAmountPerTrigger);

        public float PulseIntervalSeconds => Mathf.Max(0.5f, _pulseIntervalSeconds);

        public string RequiredTowerId => _requiredTowerId;

        public int ClampAmount(int amount)
        {
            var clamped = Mathf.Max(0, amount);
            return MaxAmountPerTrigger > 0 ? Mathf.Min(clamped, MaxAmountPerTrigger) : clamped;
        }
    }
}
