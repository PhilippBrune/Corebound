using UnityEngine;

namespace Corebound.Gameplay.Factory
{
    // A single producer within a ProductionChainData (Factory.md's "einzelner Erzeuger,
    // Verbraucher oder Verstärker").
    //
    // Amplifiers were added 2026-09-20 after the vision's §20.1 proposal was accepted. They are
    // deliberately not a new node type or a new trigger: an amplifier is an ordinary node with
    // AmplifierOutputBonus set, which multiplies every *other* node in its chain while its
    // condition holds. That keeps the vision's "closed, amplifying circuit" true - towers make
    // resources, resources buy cards, cards improve towers, and now more towers make more - while
    // adding one data field rather than a logistics layer or a second currency.
    //
    // Consumers are still not built; a node with a negative BaseAmount remains the natural way in.
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Factory/Production Node Data", fileName = "ProductionNodeData")]
    public sealed class ProductionNodeData : ScriptableObject
    {
        [SerializeField] private string _nodeId = "node_default";
        [SerializeField] private string _displayName = "Production Node";
        [SerializeField] private string _displayNameDe = string.Empty;
        [SerializeField] private ProductionTriggerType _triggerType = ProductionTriggerType.EnemyKilled;
        [SerializeField] private int _baseAmount = 1;
        [SerializeField] private int _maxAmountPerTrigger;

        // 0 disables amplification, so every existing node asset is unaffected. 0.25 means
        // "+25% to every other node in this chain" while the condition below holds.
        // 0 means "always active", so every node authored before tiers existed keeps producing
        // from the first wave. A node at tier 2 stays silent until its chain reaches tier 2.
        [SerializeField] private int _requiredChainTier;

        [Header("Amplifier (0 output bonus means this node does not amplify)")]
        [SerializeField] [Range(0f, 2f)] private float _amplifierOutputBonus;
        [SerializeField] private int _amplifierRequiredTowerCount = 3;

        // Empty counts every built tower; a tower id counts only that kind. Lets one amplifier
        // read "three towers of any kind" and another read "two Mortars" with no code change.
        [SerializeField] private string _amplifierRequiredTowerId = string.Empty;

        [Header("Tower Pulse (only used when TriggerType == TowerPulse)")]
        [SerializeField] private float _pulseIntervalSeconds = 8f;
        [SerializeField] private string _requiredTowerId = string.Empty;

        public string NodeId => _nodeId;

        public string DisplayName => Corebound.UI.LocalizationService.Resolve(_displayName, _displayNameDe);

        public ProductionTriggerType TriggerType => _triggerType;

        public int BaseAmount => Mathf.Max(0, _baseAmount);

        // 0 means uncapped. Backs the design decision that wave-based production bonuses need a
        // hard data ceiling against runaway snowballing.
        public int MaxAmountPerTrigger => Mathf.Max(0, _maxAmountPerTrigger);

        public float PulseIntervalSeconds => Mathf.Max(0.5f, _pulseIntervalSeconds);

        public string RequiredTowerId => _requiredTowerId;

        public int RequiredChainTier => Mathf.Max(0, _requiredChainTier);

        public bool IsAmplifier => _amplifierOutputBonus > Mathf.Epsilon;

        public float AmplifierOutputBonus => Mathf.Max(0f, _amplifierOutputBonus);

        public int AmplifierRequiredTowerCount => Mathf.Max(1, _amplifierRequiredTowerCount);

        public string AmplifierRequiredTowerId => _amplifierRequiredTowerId;

        public int ClampAmount(int amount)
        {
            var clamped = Mathf.Max(0, amount);
            return MaxAmountPerTrigger > 0 ? Mathf.Min(clamped, MaxAmountPerTrigger) : clamped;
        }
    }
}
