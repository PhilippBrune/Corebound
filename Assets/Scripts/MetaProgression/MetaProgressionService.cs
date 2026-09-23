using System;
using System.Collections.Generic;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Towers;
using Corebound.Gameplay.Weapons;
using Corebound.SaveSystem;
using UnityEngine;

namespace Corebound.MetaProgression
{
    public sealed class MetaProgressionService : MonoBehaviour
    {
        [SerializeField] private MetaProgressionDefinitionData _definitionData;
        [SerializeField] private UnityEngine.Object _saveManagerSource;

        private MetaProgressionSaveData _saveData;

        public event Action ProgressionChanged;

        public MetaProgressionDefinitionData DefinitionData => _definitionData;

        public int CurrencyAmount => EnsureLoaded().MetaCurrencyAmount;

        public bool HasSeenTutorial => EnsureLoaded().HasSeenTutorial;

        public bool IsMetaProgressionUnlocked => _definitionData != null && _definitionData.Unlocks != null && _definitionData.Unlocks.Length > 0;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private void OnEnable()
        {
            // Playtest 2026-09-22: switching to an empty profile still showed the old profile's
            // progression. The service cached the save data from whichever profile was active when
            // it first loaded and never dropped it.
            Corebound.SaveSystem.ProfileSelection.ActiveProfileChanged += OnActiveProfileChanged;
        }

        private void OnDisable()
        {
            Corebound.SaveSystem.ProfileSelection.ActiveProfileChanged -= OnActiveProfileChanged;
        }

        private void OnActiveProfileChanged()
        {
            _saveData = null;
            EnsureLoaded();
            ProgressionChanged?.Invoke();
        }

        private void Awake()
        {
            EnsureLoaded();

            if (GetComponent<AscensionRankBadgePresenter>() == null)
            {
                gameObject.AddComponent<AscensionRankBadgePresenter>().Bind(this);
            }
        }

        public bool IsUnlocked(string unlockId)
        {
            return EnsureLoaded().HasPurchased(unlockId);
        }

        public bool IsCardUnlocked(CardData cardData)
        {
            return cardData != null && (string.IsNullOrWhiteSpace(cardData.RequiredUnlockId) || IsUnlocked(cardData.RequiredUnlockId));
        }

        public bool IsTowerUnlocked(TowerData towerData)
        {
            return towerData != null && (string.IsNullOrWhiteSpace(towerData.RequiredUnlockId) || IsUnlocked(towerData.RequiredUnlockId));
        }

        // Shared by CardService/TowerService so the "prefer the live service, else read the
        // save file directly" fallback chain exists in exactly one place.
        public static bool IsUnlockSatisfied(MetaProgressionService service, ISaveManager saveManager, string requiredUnlockId)
        {
            if (string.IsNullOrWhiteSpace(requiredUnlockId))
            {
                return true;
            }

            if (service != null)
            {
                return service.IsUnlocked(requiredUnlockId);
            }

            if (saveManager == null)
            {
                return false;
            }

            var metaSaveData = saveManager.LoadMetaProgressionSaveData(new MetaProgressionSaveData());
            return metaSaveData != null && metaSaveData.HasPurchased(requiredUnlockId);
        }

        public WeaponData ResolvePreferredWeapon(WeaponData fallbackWeapon)
        {
            EnsureLoaded();

            if (_definitionData == null || _definitionData.Unlocks == null)
            {
                return fallbackWeapon;
            }

            for (var index = _definitionData.Unlocks.Length - 1; index >= 0; index--)
            {
                var unlockDefinition = _definitionData.Unlocks[index];

                if (!_saveData.HasPurchased(unlockDefinition.UnlockId) || unlockDefinition.WeaponReward == null)
                {
                    continue;
                }

                return unlockDefinition.WeaponReward;
            }

            return fallbackWeapon;
        }

        public IReadOnlyList<StartLoadoutData> GetUnlockedStartLoadouts()
        {
            EnsureLoaded();
            var loadouts = new List<StartLoadoutData>();

            if (_definitionData == null || _definitionData.Unlocks == null)
            {
                return loadouts;
            }

            for (var index = 0; index < _definitionData.Unlocks.Length; index++)
            {
                var unlockDefinition = _definitionData.Unlocks[index];

                if (_saveData.HasPurchased(unlockDefinition.UnlockId) && unlockDefinition.StartLoadoutReward != null)
                {
                    loadouts.Add(unlockDefinition.StartLoadoutReward);
                }
            }

            return loadouts;
        }

        public bool TryPurchase(int index)
        {
            if (_definitionData == null || _definitionData.Unlocks == null || index < 0 || index >= _definitionData.Unlocks.Length)
            {
                return false;
            }

            var unlockDefinition = _definitionData.Unlocks[index];

            if (!_saveData.TryPurchase(unlockDefinition.UnlockId, unlockDefinition.Cost))
            {
                return false;
            }

            if (SaveManager != null)
            {
                SaveManager.SaveMetaProgressionSaveData(_saveData);
            }

            ProgressionChanged?.Invoke();
            return true;
        }

        // ---- Skill tree, Ascension and Upgrades (2026-09-22) -------------------------------------
        // The Progression screen mutates the save through SkillTreeRules / UpgradeRules and then
        // calls Commit, so every rule lives in one place and this service only persists and
        // notifies.

        public MetaProgressionSaveData SaveData => EnsureLoaded();

        public int AlloyBalance => EnsureLoaded().AlloyBalance;

        public int SkillPointsAvailable => EnsureLoaded().SkillPointsAvailable;

        public int AscensionRank => EnsureLoaded().AscensionRank;

        public SkillTreeDefinitionData ActiveSkillTree => _definitionData != null ? _definitionData.FindSkillTree(EnsureLoaded().SelectedCharacterId) : null;

        public void Commit()
        {
            var saveData = EnsureLoaded();

            if (SaveManager != null)
            {
                SaveManager.SaveMetaProgressionSaveData(saveData);
            }

            ProgressionChanged?.Invoke();
        }

#if UNITY_EDITOR
        // Editor-only: lets an agent verify purchases without playing runs to earn the currency.
        public void DebugGrant(int skillPoints, int alloy)
        {
            var saveData = EnsureLoaded();
            saveData.GrantSkillPoints(skillPoints);
            saveData.GrantAlloy(alloy);
            Commit();
        }
#endif

        public void SetHasSeenTutorial(bool hasSeenTutorial)
        {
            var saveData = EnsureLoaded();

            if (saveData.HasSeenTutorial == hasSeenTutorial)
            {
                return;
            }

            saveData.SetHasSeenTutorial(hasSeenTutorial);

            if (SaveManager != null)
            {
                SaveManager.SaveMetaProgressionSaveData(saveData);
            }

            ProgressionChanged?.Invoke();
        }

        private MetaProgressionSaveData EnsureLoaded()
        {
            if (_saveData != null)
            {
                return _saveData;
            }

            var defaults = _definitionData != null ? _definitionData.CreateDefaultSaveData() : new MetaProgressionSaveData();
            _saveData = SaveManager != null ? SaveManager.LoadMetaProgressionSaveData(defaults) : defaults;

            // SkillTree.md: a content update that changes the tree grants a free respec.
            if (_definitionData != null && SkillTreeRules.ApplyTreeVersion(_saveData, _definitionData.FindSkillTree(_saveData.SelectedCharacterId)) && SaveManager != null)
            {
                SaveManager.SaveMetaProgressionSaveData(_saveData);
            }

            return _saveData;
        }
    }
}