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
            WaveEvents.WaveStarted += OnWaveStarted;
        }

        private void OnDisable()
        {
            CombatEvents.CombatantDied -= OnCombatantDied;
            WaveEvents.WaveCompleted -= OnWaveCompleted;
            WaveEvents.WaveStarted -= OnWaveStarted;
        }

        private void Update()
        {
            // Playtest 2026-09-22: "I get random Scrap in the intermission. Why do I get this?"
            // Tower pulses ran on a wall clock, so standing in an Intermission ticked Scrap in with
            // no visible cause. Production is part of fighting: pulses only run while a wave does.
            if (!IsProductionActive)
            {
                return;
            }

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

        private bool IsProductionActive
        {
            get
            {
                if (_waveManager == null)
                {
                    _waveManager = FindFirstObjectByType<Corebound.Gameplay.Waves.WaveManager>();
                }

                var state = _waveManager != null ? _waveManager.State : Corebound.Gameplay.Waves.WaveState.Combat;
                return state == Corebound.Gameplay.Waves.WaveState.Combat || state == Corebound.Gameplay.Waves.WaveState.BossCombat;
            }
        }

        private Corebound.Gameplay.Waves.WaveManager _waveManager;

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

        // A tier can rise without production: the Salvager's floor (tier 1 from the start) is known
        // once the run's class is set up, and towers built in the intermission count too. Evaluated
        // at each wave start, so the HUD and the tier announcement do not wait for the first kill.
        private void OnWaveStarted(WaveProgressEventArgs eventArgs)
        {
            EvaluateAllChainTiers();
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
        // One short line for the Intermission HUD, naming what the Factory is currently doing.
        // Deliberately reports the *next* tier's requirement too: knowing a chain is at tier 1
        // is much less useful than knowing two more towers would take it to tier 2, which is the
        // investment decision the Factory exists to create.
        // OPEN_TOPICS 2.2: the amplifier paid out with nothing on screen to say it existed, let
        // alone that it was working. Vision section 5.5 asks that a player can recognise a system
        // is running, so the HUD needs two numbers - the chain's tier, and the live bonus.
        //
        // Both are derived on demand rather than cached: they change when a tower is built or lost,
        // which has no event of its own, and the readout only asks a few times a second.
        public int HighestChainTier
        {
            get
            {
                var highest = 0;

                foreach (var pair in _lastReportedTierByChain)
                {
                    if (pair.Value > highest)
                    {
                        highest = pair.Value;
                    }
                }

                return highest;
            }
        }

        // The multiplier a producing node would currently receive, i.e. 1.4 for a live +40%.
        // Matches ApplyAmplifiers exactly: bonuses add rather than multiply, and an amplifier never
        // counts itself.
        public float ActiveAmplifierMultiplier
        {
            get
            {
                var totalBonus = 0f;

                for (var chainIndex = 0; _chains != null && chainIndex < _chains.Length; chainIndex++)
                {
                    var chain = _chains[chainIndex];

                    for (var index = 0; chain != null && chain.Nodes != null && index < chain.Nodes.Length; index++)
                    {
                        var candidate = chain.Nodes[index];

                        if (candidate != null && candidate.IsAmplifier && IsAmplifierActive(candidate))
                        {
                            totalBonus += candidate.AmplifierOutputBonus;
                        }
                    }
                }

                return 1f + totalBonus;
            }
        }

        public string DescribeActiveProduction()
        {
            if (_chains == null || _chains.Length == 0)
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder(64);

            for (var index = 0; index < _chains.Length; index++)
            {
                var chain = _chains[index];

                if (chain == null)
                {
                    continue;
                }

                var tier = ResolveChainTier(chain);
                var activeNodes = CountActiveNodes(chain, tier);
                var totalNodes = chain.Nodes != null ? chain.Nodes.Length : 0;

                if (builder.Length > 0)
                {
                    builder.Append("  ");
                }

                builder.Append(Corebound.UI.LocalizationService.Resolve("Factory T", "Fabrik S"));
                builder.Append(tier).Append(' ').Append(activeNodes).Append('/').Append(totalNodes);

                if (HasActiveAmplifier(chain, tier))
                {
                    builder.Append(Corebound.UI.LocalizationService.Resolve(" amplified", " verstärkt"));
                }

                var nextRequirement = DescribeNextTierRequirement(chain, tier);

                if (!string.IsNullOrEmpty(nextRequirement))
                {
                    builder.Append(" (").Append(nextRequirement).Append(')');
                }
            }

            return builder.ToString();
        }

        private bool HasActiveAmplifier(ProductionChainData chain, int tier)
        {
            if (chain == null || chain.Nodes == null)
            {
                return false;
            }

            for (var index = 0; index < chain.Nodes.Length; index++)
            {
                var node = chain.Nodes[index];

                if (node != null && node.IsAmplifier && node.RequiredChainTier <= tier && IsAmplifierActive(node))
                {
                    return true;
                }
            }

            return false;
        }

        private string DescribeNextTierRequirement(ProductionChainData chain, int tier)
        {
            if (chain == null || chain.Tiers == null || tier >= chain.Tiers.Length)
            {
                return string.Empty;
            }

            var next = chain.Tiers[tier];
            var towersBuilt = _towerService != null ? _towerService.BuiltTowerCount : 0;

            if (next.RequiredTowersBuilt > towersBuilt)
            {
                return Corebound.UI.LocalizationService.Resolve("+", "+")
                    + (next.RequiredTowersBuilt - towersBuilt)
                    + Corebound.UI.LocalizationService.Resolve(" towers", " Türme");
            }

            _lifetimeProducedByChain.TryGetValue(chain.ChainId, out var produced);

            if (next.RequiredLifetimeProduced > produced)
            {
                return "+" + (next.RequiredLifetimeProduced - produced)
                    + Corebound.UI.LocalizationService.Resolve(" produced", " produziert");
            }

            if (next.RequiredWavesCleared > _wavesCleared)
            {
                return "+" + (next.RequiredWavesCleared - _wavesCleared)
                    + Corebound.UI.LocalizationService.Resolve(" waves", " Wellen");
            }

            return string.Empty;
        }
        private int ResolveChainTier(ProductionChainData chain)
        {
            if (chain == null)
            {
                return 0;
            }

            var towersBuilt = _towerService != null ? _towerService.BuiltTowerCount : 0;
            var lifetimeProduced = _lifetimeProducedByChain.TryGetValue(chain.ChainId, out var produced) ? produced : 0;

            // Salvager kit (CHARACTER_CLASSES_2026-09-30): every chain starts at tier 1 at least.
            var floor = Mathf.Min(Corebound.Gameplay.Combat.CharacterEffects.MinimumChainTier, chain.Tiers != null ? chain.Tiers.Length : 0);
            return Mathf.Max(floor, chain.ResolveTier(towersBuilt, lifetimeProduced, _wavesCleared));
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
