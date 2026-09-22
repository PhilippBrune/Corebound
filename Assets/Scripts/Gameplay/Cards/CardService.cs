using System;
using System.Collections.Generic;
using Corebound.Gameplay.Relics;
using Corebound.Gameplay.Weapons;
using Corebound.MetaProgression;
using Corebound.SaveSystem;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    public sealed class CardService : MonoBehaviour
    {
        [SerializeField] private CardPoolData _cardPoolData;
        [SerializeField] private CardOfferData _cardOfferData;
        [SerializeField] private RelicPoolData _relicPoolData;
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private MetaProgressionService _metaProgressionService;

        private readonly List<CardData> _selectedCards = new List<CardData>();
        private readonly List<CardData> _currentOffer = new List<CardData>();
        private readonly List<RelicData> _selectedRelics = new List<RelicData>();
        private readonly List<string> _evolvedRelicIds = new List<string>();
        private readonly CardModifierSnapshot _modifierSnapshot = new CardModifierSnapshot();
        private readonly List<WeaponModifierData> _weaponModifiers = new List<WeaponModifierData>();
        private int _rangedEnemiesKilledCount;
        private int _wavesClearedCount;

        public event Action StateChanged;

        public IReadOnlyList<CardData> SelectedCards => _selectedCards;

        public IReadOnlyList<RelicData> SelectedRelics => _selectedRelics;

        public IReadOnlyList<CardData> CurrentOffer => _currentOffer;

        // Set by the loadout owner. Null in a scene without a player, where weapon cards are then
        // simply not offered.
        public IWeaponGrantHandler WeaponGrantHandler { get; set; }

        // WeaponLoadout.md: "Boss drop, guaranteed one weapon offer after each stage boss."
        private bool _guaranteeWeaponInNextOffer;

        public float WeaponDamageMultiplier => _modifierSnapshot.WeaponDamageMultiplier;

        public float WeaponFireIntervalMultiplier => _modifierSnapshot.WeaponFireIntervalMultiplier;

        public float ResourceGainMultiplier => _modifierSnapshot.ResourceGainMultiplier;

        public float TowerDamageMultiplier => _modifierSnapshot.TowerDamageMultiplier;

        public float IncomingDamageTakenMultiplier => _modifierSnapshot.IncomingDamageTakenMultiplier;

        public float ProjectileSpeedMultiplier => _modifierSnapshot.ProjectileSpeedMultiplier;

        public float PlayerMovementSpeedMultiplier => _modifierSnapshot.PlayerMovementSpeedMultiplier;

        public float PurchaseCostMultiplier => _modifierSnapshot.PurchaseCostMultiplier;

        // Every WeaponModifierData the current build grants, cards and relics together.
        // PlayerCombatController folds these into a WeaponModifierState alongside the three flat
        // multipliers above; nothing here knows which weapon is equipped.
        public IReadOnlyList<WeaponModifierData> WeaponModifiers => _weaponModifiers;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private void OnEnable()
        {
            Corebound.Gameplay.Bosses.BossEvents.OnBossDefeated += OnBossDefeated;
        }

        private void OnDisable()
        {
            Corebound.Gameplay.Bosses.BossEvents.OnBossDefeated -= OnBossDefeated;
        }

        private void OnBossDefeated(Corebound.Gameplay.Bosses.BossEventArgs eventArgs)
        {
            _guaranteeWeaponInNextOffer = true;
        }

        private void Awake()
        {
            LoadEvolutionStateFromSave();
            LoadSelectedCardsFromSave();
            LoadSelectedRelicsFromSave();
        }

        public void BuildRewardOffer()
        {
            _currentOffer.Clear();

            if (_cardPoolData == null || _cardPoolData.Cards == null || _cardOfferData == null)
            {
                StateChanged?.Invoke();
                return;
            }

            var eligibleCards = new List<CardData>();

            for (var index = 0; index < _cardPoolData.Cards.Length; index++)
            {
                var card = _cardPoolData.Cards[index];

                if (card == null || !CanOfferCard(card))
                {
                    continue;
                }

                eligibleCards.Add(card);
            }

            if (eligibleCards.Count == 0)
            {
                StateChanged?.Invoke();
                return;
            }

            var optionCount = Mathf.Min(_cardOfferData.OptionCount, eligibleCards.Count);

            if (_guaranteeWeaponInNextOffer)
            {
                _guaranteeWeaponInNextOffer = false;
                var weaponCards = eligibleCards.FindAll(card => card.IsWeaponCard);
                var weaponPick = PickWeightedCard(weaponCards);

                if (weaponPick != null)
                {
                    _currentOffer.Add(weaponPick);
                    eligibleCards.Remove(weaponPick);
                    optionCount = Mathf.Min(optionCount, eligibleCards.Count + 1);
                }
            }

            for (var optionIndex = _currentOffer.Count; optionIndex < optionCount; optionIndex++)
            {
                var picked = PickWeightedCard(eligibleCards);

                if (picked == null)
                {
                    break;
                }

                _currentOffer.Add(picked);
                eligibleCards.Remove(picked);
            }

            StateChanged?.Invoke();
        }

        private CardData PickWeightedCard(List<CardData> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var totalWeight = 0f;

            for (var index = 0; index < candidates.Count; index++)
            {
                totalWeight += _cardOfferData.ResolveWeight(candidates[index].Rarity);
            }

            if (totalWeight <= 0f)
            {
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }

            var roll = UnityEngine.Random.Range(0f, totalWeight);
            var cumulative = 0f;

            for (var index = 0; index < candidates.Count; index++)
            {
                cumulative += _cardOfferData.ResolveWeight(candidates[index].Rarity);

                if (roll <= cumulative)
                {
                    return candidates[index];
                }
            }

            return candidates[candidates.Count - 1];
        }

        public bool TryApplyCard(CardData card)
        {
            return TryApplyCard(card, 0);
        }

        // purchasePrice: what the shop charged (0 for a reward). Only weapon cards use it.
        public bool TryApplyCard(CardData card, int purchasePrice)
        {
            if (card == null)
            {
                return false;
            }

            if (card.IsWeaponCard)
            {
                var granted = WeaponGrantHandler != null && WeaponGrantHandler.TryGrantWeapon(card.GrantedWeapon, (int)card.Rarity, purchasePrice);

                if (granted)
                {
                    _currentOffer.Clear();
                    StateChanged?.Invoke();
                }

                return granted;
            }

            if (!card.Stackable && ContainsSelected(card.CardId))
            {
                return false;
            }

            _selectedCards.Add(card);
            RebuildModifierSnapshot();
            SaveSelectedCardsToRun();
            _currentOffer.Clear();
            StateChanged?.Invoke();
            return true;
        }

        public bool CanOfferCard(CardData card)
        {
            if (card == null || !IsCardAvailable(card))
            {
                return false;
            }

            // A weapon card needs a loadout to land in, and its weapon must be unlocked.
            if (card.IsWeaponCard && WeaponGrantHandler == null)
            {
                return false;
            }

            // Autopilot 2026-09-22: the boss's guaranteed weapon went pending (full loadout), the
            // next stage starts without an Intermission to place it, and the first reward of that
            // stage offered the same weapon again. TryGrantWeapon refuses while one is pending, so
            // Confirm on that card silently did nothing. Never offer a card that cannot be taken.
            if (card.IsWeaponCard && WeaponGrantHandler.HasPendingWeapon)
            {
                return false;
            }

            if (ContainsSelected(card.CardId) && !card.Stackable)
            {
                return false;
            }

            if (card.Rarity == CardRarity.Legendary && _cardOfferData != null && _selectedCards.Count < _cardOfferData.LegendaryMinimumSelectedCards)
            {
                return false;
            }

            return true;
        }

        private void LoadSelectedCardsFromSave()
        {
            _selectedCards.Clear();

            if (_cardPoolData == null || _cardPoolData.Cards == null || SaveManager == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData();

            if (runSaveData == null || runSaveData.SelectedCardIds == null)
            {
                return;
            }

            for (var idIndex = 0; idIndex < runSaveData.SelectedCardIds.Count; idIndex++)
            {
                var cardId = runSaveData.SelectedCardIds[idIndex];

                for (var cardIndex = 0; cardIndex < _cardPoolData.Cards.Length; cardIndex++)
                {
                    var card = _cardPoolData.Cards[cardIndex];

                    if (card != null && card.CardId == cardId)
                    {
                        _selectedCards.Add(card);
                        break;
                    }
                }
            }

            RebuildModifierSnapshot();
        }

        private void SaveSelectedCardsToRun()
        {
            if (SaveManager == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
            var cardIds = new List<string>(_selectedCards.Count);

            for (var index = 0; index < _selectedCards.Count; index++)
            {
                cardIds.Add(_selectedCards[index].CardId);
            }

            runSaveData.SetSelectedCardIds(cardIds);
            SaveManager.SaveRunSaveData(runSaveData);
        }

        private void RebuildModifierSnapshot()
        {
            _modifierSnapshot.Reset();
            _weaponModifiers.Clear();

            for (var index = 0; index < _selectedCards.Count; index++)
            {
                ApplyEffects(_selectedCards[index].Effects);
                CollectWeaponModifiers(_selectedCards[index].WeaponModifiers);
            }

            for (var index = 0; index < _selectedRelics.Count; index++)
            {
                var relic = _selectedRelics[index];
                var hasEvolved = relic.IsEvolving && _evolvedRelicIds.Contains(relic.RelicId);
                ApplyEffects(hasEvolved ? relic.EvolvedEffects : relic.Effects);
                CollectWeaponModifiers(relic.WeaponModifiers);
            }
        }

        // Duplicates are kept, not deduplicated: two copies of a stackable +10% crit card must
        // apply twice, exactly as two copies of the same CardEffectData already do.
        private void CollectWeaponModifiers(WeaponModifierData[] modifiers)
        {
            if (modifiers == null)
            {
                return;
            }

            for (var index = 0; index < modifiers.Length; index++)
            {
                if (modifiers[index] != null)
                {
                    _weaponModifiers.Add(modifiers[index]);
                }
            }
        }

        private void ApplyEffects(CardEffectData[] effects)
        {
            if (effects == null)
            {
                return;
            }

            for (var effectIndex = 0; effectIndex < effects.Length; effectIndex++)
            {
                _modifierSnapshot.Apply(effects[effectIndex]);
            }
        }

        // Relics.md: "Relics erscheinen selten an hochwertigen Reward-Momenten wie
        // Boss-Abschlüssen". Called by WaveManager on boss defeat. Unlike Card offers (player
        // picks 1 of 3 via CardRewardPresenter), this grants immediately and automatically.
        // RelicEvents.RelicGranted drives RelicRevealPresenter's dedicated ceremonial reveal
        // (Relics.md's "eigenständige, hochwertige visuelle Sprache" requirement) instead of the
        // generic NotificationPresenter toast this used to fall back to.
        public RelicData TryGrantRandomRelic()
        {
            if (_relicPoolData == null || _relicPoolData.Relics == null)
            {
                return null;
            }

            var eligible = new List<RelicData>();

            for (var index = 0; index < _relicPoolData.Relics.Length; index++)
            {
                var relic = _relicPoolData.Relics[index];

                if (relic == null || ContainsSelectedRelic(relic.RelicId))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(relic.RequiredUnlockId) && !MetaProgressionService.IsUnlockSatisfied(_metaProgressionService, SaveManager, relic.RequiredUnlockId))
                {
                    continue;
                }

                eligible.Add(relic);
            }

            if (eligible.Count == 0)
            {
                return null;
            }

            var picked = eligible[UnityEngine.Random.Range(0, eligible.Count)];
            _selectedRelics.Add(picked);
            RebuildModifierSnapshot();
            SaveSelectedRelicsToRun();
            StateChanged?.Invoke();
            RelicEvents.RaiseRelicGranted(new RelicGrantedEventArgs(picked, false));
            return picked;
        }

        // Called by WaveManager.OnCombatantDied for each defeated EnemyRoleType.RangedPressure
        // enemy. Counter is run-scoped and persists across scene reload; boss-fight helper enemies
        // count too, ordinary combat kills only (see WaveManager for the exact call sites).
        public void NotifyRangedEnemyKilled()
        {
            _rangedEnemiesKilledCount += 1;
            ProcessEvolution(RelicEvolutionCondition.RangedEnemiesKilled, _rangedEnemiesKilledCount);
        }

        // Called by WaveManager.CompleteCurrentWave for each ordinary wave clear (Boss encounters
        // are excluded - they already grant a Relic of their own via TryGrantRandomRelic).
        public void NotifyWaveCleared()
        {
            _wavesClearedCount += 1;
            ProcessEvolution(RelicEvolutionCondition.WavesCleared, _wavesClearedCount);
        }

        private void ProcessEvolution(RelicEvolutionCondition condition, int currentCount)
        {
            var didEvolve = false;

            for (var index = 0; index < _selectedRelics.Count; index++)
            {
                var relic = _selectedRelics[index];

                if (relic == null
                    || !relic.IsEvolving
                    || relic.EvolutionCondition != condition
                    || currentCount < relic.EvolutionThreshold
                    || _evolvedRelicIds.Contains(relic.RelicId))
                {
                    continue;
                }

                _evolvedRelicIds.Add(relic.RelicId);
                didEvolve = true;
                RelicEvents.RaiseRelicGranted(new RelicGrantedEventArgs(relic, true));
            }

            if (didEvolve)
            {
                RebuildModifierSnapshot();
                StateChanged?.Invoke();
            }

            SaveEvolutionStateToRun();
        }

        private void LoadEvolutionStateFromSave()
        {
            _rangedEnemiesKilledCount = 0;
            _wavesClearedCount = 0;
            _evolvedRelicIds.Clear();

            if (SaveManager == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData();

            if (runSaveData == null)
            {
                return;
            }

            _rangedEnemiesKilledCount = runSaveData.RangedEnemiesKilledCount;
            _wavesClearedCount = runSaveData.WavesClearedCount;

            if (runSaveData.EvolvedRelicIds != null)
            {
                _evolvedRelicIds.AddRange(runSaveData.EvolvedRelicIds);
            }
        }

        private void SaveEvolutionStateToRun()
        {
            if (SaveManager == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
            runSaveData.SetRangedEnemiesKilledCount(_rangedEnemiesKilledCount);
            runSaveData.SetWavesClearedCount(_wavesClearedCount);
            runSaveData.SetEvolvedRelicIds(_evolvedRelicIds);
            SaveManager.SaveRunSaveData(runSaveData);
        }

        private bool ContainsSelectedRelic(string relicId)
        {
            for (var index = 0; index < _selectedRelics.Count; index++)
            {
                if (_selectedRelics[index].RelicId == relicId)
                {
                    return true;
                }
            }

            return false;
        }

        private void LoadSelectedRelicsFromSave()
        {
            _selectedRelics.Clear();

            if (_relicPoolData == null || _relicPoolData.Relics == null || SaveManager == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData();

            if (runSaveData == null || runSaveData.SelectedRelicIds == null)
            {
                return;
            }

            for (var idIndex = 0; idIndex < runSaveData.SelectedRelicIds.Count; idIndex++)
            {
                var relicId = runSaveData.SelectedRelicIds[idIndex];

                for (var relicIndex = 0; relicIndex < _relicPoolData.Relics.Length; relicIndex++)
                {
                    var relic = _relicPoolData.Relics[relicIndex];

                    if (relic != null && relic.RelicId == relicId)
                    {
                        _selectedRelics.Add(relic);
                        break;
                    }
                }
            }

            RebuildModifierSnapshot();
        }

        private void SaveSelectedRelicsToRun()
        {
            if (SaveManager == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
            var relicIds = new List<string>(_selectedRelics.Count);

            for (var index = 0; index < _selectedRelics.Count; index++)
            {
                relicIds.Add(_selectedRelics[index].RelicId);
            }

            runSaveData.SetSelectedRelicIds(relicIds);
            SaveManager.SaveRunSaveData(runSaveData);
        }

        private bool IsCardAvailable(CardData card)
        {
            return card != null && MetaProgressionService.IsUnlockSatisfied(_metaProgressionService, SaveManager, card.RequiredUnlockId);
        }

        private bool ContainsSelected(string cardId)
        {
            for (var index = 0; index < _selectedCards.Count; index++)
            {
                if (_selectedCards[index] != null && _selectedCards[index].CardId == cardId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}