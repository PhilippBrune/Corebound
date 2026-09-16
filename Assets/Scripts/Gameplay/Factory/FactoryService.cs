using System.Collections.Generic;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Enemies;
using Corebound.Gameplay.Resources;
using Corebound.Gameplay.Towers;
using Corebound.Gameplay.Waves;
using UnityEngine;

namespace Corebound.Gameplay.Factory
{
    // Factory.md's central production evaluator: "Ein Factory-Service wertet registrierte
    // Producer und Consumer aus und löst Produktionsereignisse zentral aus." The first vertical
    // slice ships three triggers only (Docs/07_DESIGN_DECISIONS.md): enemy kills, a time-based
    // tower pulse, and wave completion. Resource conversion and consumer/amplifier nodes are
    // explicitly out of scope for this slice - the data model (ProductionNodeData) already leaves
    // room for them without an architecture change, per the spec's extensibility requirement.
    public sealed class FactoryService : MonoBehaviour
    {
        private sealed class PulseState
        {
            public ProductionChainData Chain;
            public ProductionNodeData Node;
            public float Timer;
        }

        [SerializeField] private ProductionChainData[] _chains;
        [SerializeField] private ResourceWalletService _resourceWalletService;
        [SerializeField] private TowerService _towerService;

        private readonly List<PulseState> _pulseStates = new List<PulseState>();

        private void Awake()
        {
            RebuildPulseStates();
        }

        private void OnEnable()
        {
            CombatEvents.CombatantDied += OnCombatantDied;
            WaveEvents.WaveCompleted += OnWaveCompleted;
        }

        private void OnDisable()
        {
            CombatEvents.CombatantDied -= OnCombatantDied;
            WaveEvents.WaveCompleted -= OnWaveCompleted;
        }

        private void Update()
        {
            for (var index = 0; index < _pulseStates.Count; index++)
            {
                var pulseState = _pulseStates[index];
                pulseState.Timer -= Time.deltaTime;

                if (pulseState.Timer > 0f)
                {
                    continue;
                }

                pulseState.Timer = pulseState.Node.PulseIntervalSeconds;
                TriggerTowerPulse(pulseState.Chain, pulseState.Node);
            }
        }

        private void RebuildPulseStates()
        {
            _pulseStates.Clear();

            ForEachNodeOfType(ProductionTriggerType.TowerPulse, (chain, node) =>
            {
                _pulseStates.Add(new PulseState { Chain = chain, Node = node, Timer = node.PulseIntervalSeconds });
            });
        }

        private void OnCombatantDied(DeathEventArgs eventArgs)
        {
            if (eventArgs.Victim == null || eventArgs.Victim.GetComponent<EnemyRuntimeController>() == null)
            {
                return;
            }

            ForEachNodeOfType(ProductionTriggerType.EnemyKilled, (chain, node) =>
            {
                HandleTrigger(chain, node, node.ClampAmount(node.BaseAmount), "zero_amount");
            });
        }

        private void OnWaveCompleted(WaveProgressEventArgs eventArgs)
        {
            ForEachNodeOfType(ProductionTriggerType.WaveCompleted, (chain, node) =>
            {
                HandleTrigger(chain, node, node.ClampAmount(node.BaseAmount), "zero_amount");
            });
        }

        private void TriggerTowerPulse(ProductionChainData chain, ProductionNodeData node)
        {
            var producerCount = _towerService != null ? _towerService.CountBuiltTowersById(node.RequiredTowerId) : 0;

            if (producerCount <= 0)
            {
                FactoryEvents.RaiseProductionTriggered(new ProductionTriggeredEventArgs(chain.ChainId, node.NodeId, node.TriggerType));
                FactoryEvents.RaiseProductionBlocked(new ProductionBlockedEventArgs(chain.ChainId, node.NodeId, "no_producer_built"));
                return;
            }

            var modifierData = chain.ModifierData;
            var weight = modifierData != null ? modifierData.ResolveEffectiveProducerWeight(producerCount) : producerCount;
            var amount = node.ClampAmount(Mathf.RoundToInt(node.BaseAmount * weight));
            HandleTrigger(chain, node, amount, "zero_amount");
        }

        private void HandleTrigger(ProductionChainData chain, ProductionNodeData node, int rawAmount, string blockedReasonIfZero)
        {
            FactoryEvents.RaiseProductionTriggered(new ProductionTriggeredEventArgs(chain.ChainId, node.NodeId, node.TriggerType));

            if (rawAmount <= 0)
            {
                FactoryEvents.RaiseProductionBlocked(new ProductionBlockedEventArgs(chain.ChainId, node.NodeId, blockedReasonIfZero));
                return;
            }

            if (_resourceWalletService == null)
            {
                FactoryEvents.RaiseProductionBlocked(new ProductionBlockedEventArgs(chain.ChainId, node.NodeId, "no_wallet"));
                return;
            }

            var gained = _resourceWalletService.AddReward(rawAmount);
            FactoryEvents.RaiseResourceGenerated(new ResourceGeneratedEventArgs(chain.ChainId, node.NodeId, node.DisplayName, gained, node.TriggerType));
        }

        private void ForEachNodeOfType(ProductionTriggerType triggerType, System.Action<ProductionChainData, ProductionNodeData> handler)
        {
            if (_chains == null)
            {
                return;
            }

            for (var chainIndex = 0; chainIndex < _chains.Length; chainIndex++)
            {
                var chain = _chains[chainIndex];

                if (chain == null || chain.Nodes == null)
                {
                    continue;
                }

                for (var nodeIndex = 0; nodeIndex < chain.Nodes.Length; nodeIndex++)
                {
                    var node = chain.Nodes[nodeIndex];

                    if (node != null && node.TriggerType == triggerType)
                    {
                        handler(chain, node);
                    }
                }
            }
        }
    }
}
