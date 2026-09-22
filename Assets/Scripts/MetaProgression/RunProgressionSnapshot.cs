using Corebound.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corebound.MetaProgression
{
    // Everything permanent that shapes a run, resolved once when the run's scene starts and then
    // frozen (SkillTree.md: "read at run start and frozen for the run's duration"; Upgrades.md: the
    // same). Gameplay code reads this and never the save, so nothing bought in a menu - and
    // nothing earned mid-run, like the Skill Points from a boss kill - can change a run in
    // progress.
    //
    // Composition order (SkillTree.md, Upgrades.md): base -> skills and upgrades (here) -> weapon
    // set bonuses -> in-run cards. Callers multiply these in before the card modifiers.
    public sealed class RunProgressionSnapshot
    {
        public const int MaxWeaponSlots = 6;

        private static RunProgressionSnapshot _current;
        private static int _capturedSceneHandle = int.MinValue;

        private RunProgressionSnapshot(SkillModifierSnapshot skills, UpgradeSnapshot upgrades, int weaponSlotCount, string characterId)
        {
            Skills = skills;
            Upgrades = upgrades;
            WeaponSlotCount = weaponSlotCount;
            CharacterId = characterId;
        }

        public static RunProgressionSnapshot Empty { get; } = new RunProgressionSnapshot(new SkillModifierSnapshot(), UpgradeSnapshot.Empty, 1, MetaProgressionSaveData.DefaultCharacterId);

        // Falls back to an empty snapshot, never null: an isolated test scene without a save
        // manager simply plays with no permanent progression.
        public static RunProgressionSnapshot Current => _current ?? Empty;

        public SkillModifierSnapshot Skills { get; }

        public UpgradeSnapshot Upgrades { get; }

        public int WeaponSlotCount { get; }

        public string CharacterId { get; }

        // Idempotent per scene load: the first caller in a freshly loaded gameplay scene resolves
        // the snapshot, everyone after it in that scene gets the same one. A new run always loads
        // the scene again, so it always gets a fresh capture.
        public static RunProgressionSnapshot Capture(ISaveManager saveManager, MetaProgressionDefinitionData definition)
        {
            var sceneHandle = SceneManager.GetActiveScene().handle;

            if (_current != null && _capturedSceneHandle == sceneHandle)
            {
                return _current;
            }

            _capturedSceneHandle = sceneHandle;

            if (saveManager == null || definition == null)
            {
                _current = Empty;
                return _current;
            }

            var save = saveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData());
            var tree = definition.FindSkillTree(save.SelectedCharacterId);
            var skills = SkillTreeRules.BuildSnapshot(save, tree);
            var upgrades = UpgradeSnapshot.Build(save, definition.UpgradeCatalog);

            // WeaponLoadout.md slot sources: 1 at start, +1 once any stage has ever been cleared,
            // +1 each from two Combat key nodes, +1 each from two Alloy unlocks.
            var slots = 1
                + (save.HasClearedAnyStage ? 1 : 0)
                + Mathf.RoundToInt(skills[SkillEffectTarget.WeaponSlots])
                + definition.CountPurchasedWeaponSlots(save);

            _current = new RunProgressionSnapshot(skills, upgrades, Mathf.Clamp(slots, 1, MaxWeaponSlots), save.SelectedCharacterId);
            Debug.Log("[Progression] Run snapshot: " + _current.WeaponSlotCount + " weapon slot(s), "
                + save.SkillNodeRanks.Count + " skill node(s), " + save.UpgradeRanks.Count + " upgrade rank(s), ascension " + save.AscensionRank + ".");
            return _current;
        }
    }
}
