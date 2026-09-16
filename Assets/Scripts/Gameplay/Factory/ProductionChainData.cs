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

        public string ChainId => _chainId;

        public string DisplayName => _displayName;

        public ProductionNodeData[] Nodes => _nodes;

        public ProductionModifierData ModifierData => _modifierData;
    }
}
