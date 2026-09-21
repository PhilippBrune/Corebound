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

        private readonly Dictionary<string, int> _lifetimeProducedByChain = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _lastReportedTierByChain = new Dictionary<string, int>();
        private int _wavesCleared;
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

        private void EvaluateAllChainTiers()
        {
            if (_chains == null)
            {
                return;
            }

            for (var index = 0; index < _chains.Length; index++)
            {
                EvaluateChainTier(_chains[index]);
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
            _wavesCleared++;
            EvaluateAllChainTiers();

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

            // A node above the chain's current tier is dormant, not broken - reporting it as
            // blocked with a named reason is what lets the player see there is more chain to earn.
            if (node.RequiredChainTier > ResolveChainTier(chain))
            {
                FactoryEvents.RaiseProductionBlocked(new ProductionBlockedEventArgs(chain.ChainId, node.NodeId, "chain_tier_too_low"));
                return;
            }

            rawAmount = ApplyAmplifiers(chain, node, rawAmount);

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
            RecordProduction(chain, gained);
            FactoryEvents.RaiseResourceGenerated(new ResourceGeneratedEventArgs(chain.ChainId, node.NodeId, node.DisplayName, gained, node.TriggerType));
        }

        // The amplifying half of the vision's closed loop (00_PROJECT_VISION.md §20.1, accepted
        // 2026-09-20). An amplifier multiplies every *other* producing node in its own chain while
        // its tower condition holds, so the reward for building more towers is not just more
        // pulses but a better rate on everything the chain makes.
        //
        // Three deliberate constraints keep it from snowballing, which is the risk Factory.md calls
        // out by name:
        //   - an amplifier never amplifies itself, so two amplifiers cannot feed each other,
        //   - bonuses from multiple amplifiers add rather than multiply,
        //   - the result still passes through node.ClampAmount, so MaxAmountPerTrigger remains a
        //     hard ceiling on any single trigger no matter how many amplifiers are active.
        private int ApplyAmplifiers(ProductionChainData chain, ProductionNodeData node, int rawAmount)
        {
            if (rawAmount <= 0 || chain == null || chain.Nodes == null || node.IsAmplifier)
            {
                return rawAmount;
            }

            var totalBonus = 0f;

            for (var index = 0; index < chain.Nodes.Length; index++)
            {
                var candidate = chain.Nodes[index];

                if (candidate == null || candidate == node || !candidate.IsAmplifier || !IsAmplifierActive(candidate))
                {
                    continue;
                }

                totalBonus += candidate.AmplifierOutputBonus;
            }

            if (totalBonus <= Mathf.Epsilon)
            {
                return rawAmount;
            }

            // Rounded up so a small bonus on a 1-resource trigger is still visible. A +25% that
            // silently rounds to nothing would read as the amplifier not working at all.
            var amplified = Mathf.CeilToInt(rawAmount * (1f + totalBonus));
            return node.ClampAmount(amplified);
        }

        private bool IsAmplifierActive(ProductionNodeData amplifier)
        {
            if (_towerService == null)
            {
                return false;
            }

            var builtCount = string.IsNullOrWhiteSpace(amplifier.AmplifierRequiredTowerId)
                ? _towerService.BuiltTowerCount
                : _towerService.CountBuiltTowersById(amplifier.AmplifierRequiredTowerId);

            return builtCount >= amplifier.AmplifierRequiredTowerCount;
        }

        // Chain tiers are the Factory's depth answer (00_PROJECT_VISION.md section 20.4): a chain
        // climbs as the player's own investment grows, and each tier switches on more of its nodes.
        // Towers -> tier -> more nodes produce -> more resources -> more towers, which is the
        // vision's closed amplifying loop with no logistics layer anywhere in it.
        private int ResolveChainTier(ProductionChainData chain)
        {
            if (chain == null)
            {
                return 0;
            }

            var towersBuilt = _towerService != null ? _towerService.BuiltTowerCount : 0;
            var lifetimeProduced = _lifetimeProducedByChain.TryGetValue(chain.ChainId, out var produced) ? produced : 0;
            return chain.ResolveTier(towersBuilt, lifetimeProduced, _wavesCleared);
        }

        private void RecordProduction(ProductionChainData chain, int gained)
        {
            if (chain == null || gained <= 0)
            {
                return;
            }

            _lifetimeProducedByChain.TryGetValue(chain.ChainId, out var previous);
            _lifetimeProducedByChain[chain.ChainId] = previous + gained;
            EvaluateChainTier(chain);
        }

        // Announced only on a change, and only upward, so a tower being sold does not spam a
        // demotion the player did not ask about.
        private void EvaluateChainTier(ProductionChainData chain)
        {
            if (chain == null)
            {
                return;
            }

            var tier = ResolveChainTier(chain);
            _lastReportedTierByChain.TryGetValue(chain.ChainId, out var previous);

            if (tier == previous)
            {
                return;
            }

            _lastReportedTierByChain[chain.ChainId] = tier;

            if (tier < previous)
            {
                return;
            }

            var tierName = chain.Tiers != null && tier > 0 && tier <= chain.Tiers.Length
                ? chain.Tiers[tier - 1].DisplayName
                : "Tier " + tier;

            FactoryEvents.RaiseChainTierChanged(new ChainTierChangedEventArgs(
                chain.ChainId,
                tierName,
                previous,
                tier,
                CountActiveNodes(chain, tier)));
        }

        private static int CountActiveNodes(ProductionChainData chain, int tier)
        {
            if (chain == null || chain.Nodes == null)
            {
                return 0;
            }

            var count = 0;

            for (var index = 0; index < chain.Nodes.Length; index++)
            {
                if (chain.Nodes[index] != null && chain.Nodes[index].RequiredChainTier <= tier)
                {
                    count++;
                }
            }

            return count;
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
