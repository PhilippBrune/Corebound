using UnityEngine;

namespace Corebound.Gameplay.Factory
{
    // A themed, connected production path (Factory.md's "Definition eines zusammenhängenden
    // Produktionspfads oder Themas"). FactoryService evaluates every node in every assigned chain
    // against the modifier data shared across the whole chain.
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Factory/Production Chain Data", fileName = "ProductionChainData")]
    public sealed class ProductionChainData : ScriptableObject
    {
        [SerializeField] private string _chainId = "chain_default";
        [SerializeField] private string _displayName = "Production Chain";
        [SerializeField] private ProductionNodeData[] _nodes;
        [SerializeField] private ProductionModifierData _modifierData;

        // The Factory's depth answer (00_PROJECT_VISION.md section 20.4). A chain climbs tiers
        // as the player's own investment grows, and each tier switches on more of the chain's
        // nodes. That is the vision's "improved systems generate greater value" loop made real
        // without a logistics layer: towers -> tier -> more nodes produce -> more resources ->
        // more towers.
        //
        // Tiers are ordered and cumulative: tier 2 requires tier 1's condition to also hold.
        [SerializeField] private ProductionChainTier[] _tiers;

        public string ChainId => _chainId;

        public string DisplayName => _displayName;

        public ProductionNodeData[] Nodes => _nodes;

        public ProductionModifierData ModifierData => _modifierData;

        public ProductionChainTier[] Tiers => _tiers;

        // Tier 0 is always active, so a chain with no authored tiers behaves exactly as it did
        // before tiers existed. Every existing chain asset keeps working untouched.
        public int ResolveTier(int towersBuilt, int lifetimeProduced, int wavesCleared)
        {
            if (_tiers == null)
            {
                return 0;
            }

            var tier = 0;

            for (var index = 0; index < _tiers.Length; index++)
            {
                if (!_tiers[index].IsSatisfiedBy(towersBuilt, lifetimeProduced, wavesCleared))
                {
                    break;
                }

                tier = index + 1;
            }

            return tier;
        }
    }

    // A condition a chain must meet to switch on the next group of its nodes. All three
    // requirements must hold; leaving one at 0 makes it irrelevant, which is how a tier gated
    // purely on tower count is authored.
    [System.Serializable]
    public struct ProductionChainTier
    {
        [SerializeField] private string _displayName;
        [SerializeField] private int _requiredTowersBuilt;
        [SerializeField] private int _requiredLifetimeProduced;
        [SerializeField] private int _requiredWavesCleared;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? "Tier" : _displayName;

        public int RequiredTowersBuilt => Mathf.Max(0, _requiredTowersBuilt);

        public int RequiredLifetimeProduced => Mathf.Max(0, _requiredLifetimeProduced);

        public int RequiredWavesCleared => Mathf.Max(0, _requiredWavesCleared);

        public bool IsSatisfiedBy(int towersBuilt, int lifetimeProduced, int wavesCleared)
        {
            return towersBuilt >= RequiredTowersBuilt
                && lifetimeProduced >= RequiredLifetimeProduced
                && wavesCleared >= RequiredWavesCleared;
        }
    }
}
