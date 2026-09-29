using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Corebound.SaveSystem
{
    // SaveSystem.md, "Drei Profile" (owner decision 2026-09-22): the game has three profiles, each
    // with its own completely separate save state - meta progression, settings, statistics and the
    // continuable run. This class owns which one is active and how the folders are named; every
    // path in LocalProfileSaveManager is built from it.
    //
    // The choice itself lives in profile.json *next to* the profile folders, never inside one of
    // them, so switching profiles can never be lost with the profile it points at.
    public static class ProfileSelection
    {
        public const int ProfileCount = 3;
        private const string SelectionFileName = "profile.json";

        private static int _activeIndex = -1;

        [Serializable]
        private sealed class SelectionFile
        {
            public int ActiveProfileIndex = 1;
        }

        public static event Action ActiveProfileChanged;

        // 1-based, so it matches the folder name and what the player sees.
        public static int ActiveIndex
        {
            get
            {
                if (_activeIndex < 1)
                {
                    _activeIndex = LoadActiveIndex();
                }

                return _activeIndex;
            }
        }

        public static string ActiveFolderName => FolderName(ActiveIndex);

        public static string FolderName(int index)
        {
            return "Profile_" + Mathf.Clamp(index, 1, ProfileCount).ToString("00", CultureInfo.InvariantCulture);
        }

        public static void SetActive(int index)
        {
            index = Mathf.Clamp(index, 1, ProfileCount);

            if (index == ActiveIndex)
            {
                return;
            }

            _activeIndex = index;
            Save();
            ActiveProfileChanged?.Invoke();
        }

        // What the menu shows per row, so three profiles are told apart without opening them.
        public readonly struct ProfileSummary
        {
            public ProfileSummary(bool isUsed, int ascensionRank, int alloy, int skillPoints, bool hasRun, int runStageIndex, int level = 1, int paragonLevel = 0)
            {
                IsUsed = isUsed;
                AscensionRank = ascensionRank;
                Level = level;
                ParagonLevel = paragonLevel;
                Alloy = alloy;
                SkillPoints = skillPoints;
                HasRun = hasRun;
                RunStageIndex = runStageIndex;
            }

            public bool IsUsed { get; }

            public int AscensionRank { get; }

            public int Alloy { get; }

            public int SkillPoints { get; }

            public bool HasRun { get; }

            public int RunStageIndex { get; }

            public int Level { get; }

            public int ParagonLevel { get; }

            // "Level 12", or "Paragon 3" past the cap - how far a profile has come, which is what
            // the old "Ascension N" label stood for before Ascension moved behind the cap.
            public string ProgressLabel => Level >= MetaProgressionSaveData.MaxLevel
                ? "Paragon " + ParagonLevel
                : Corebound.UI.LocalizationService.Resolve("Level ", "Stufe ") + Level;
        }

        // Wipes a profile's four save files. The folder is kept so the next run writes into a
        // clean profile rather than recreating a half-state; the active profile can be deleted too,
        // and simply comes back empty.
        public static void DeleteProfile(int index)
        {
            var folder = Path.Combine(Application.persistentDataPath, FolderName(index));

            try
            {
                if (Directory.Exists(folder))
                {
                    foreach (var file in Directory.GetFiles(folder, "*.json"))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ProfileSelection] could not delete profile " + index + ": " + exception.Message);
                return;
            }

            // Anything holding cached data for this profile has to drop it, exactly as on a switch.
            ActiveProfileChanged?.Invoke();
        }

        public static ProfileSummary Summarize(int index)
        {
            var folder = Path.Combine(Application.persistentDataPath, FolderName(index));

            if (!Directory.Exists(folder))
            {
                return new ProfileSummary(false, 0, 0, 0, false, 0);
            }

            var meta = ReadJson<MetaProgressionSaveData>(Path.Combine(folder, "meta.json"));
            var run = ReadJson<RunSaveData>(Path.Combine(folder, "run.json"));
            var used = meta != null || run != null;

            return new ProfileSummary(
                used,
                meta != null ? meta.AscensionRank : 0,
                meta != null ? meta.AlloyBalance : 0,
                meta != null ? meta.SkillPointsAvailable : 0,
                run != null,
                run != null ? run.StageIndex : 0,
                meta != null ? meta.Level : 1,
                meta != null ? meta.ParagonLevel : 0);
        }

        private static T ReadJson<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                var json = File.ReadAllText(path);
                return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<T>(json);
            }
            catch (Exception)
            {
                // A corrupt file must not stop the menu from listing the other profiles.
                return null;
            }
        }

        private static int LoadActiveIndex()
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, SelectionFileName);

                if (File.Exists(path))
                {
                    var selection = JsonUtility.FromJson<SelectionFile>(File.ReadAllText(path));

                    if (selection != null)
                    {
                        return Mathf.Clamp(selection.ActiveProfileIndex, 1, ProfileCount);
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to profile 1 - the same profile every install started with.
            }

            return 1;
        }

        private static void Save()
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, SelectionFileName);
                File.WriteAllText(path, JsonUtility.ToJson(new SelectionFile { ActiveProfileIndex = _activeIndex }));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ProfileSelection] could not save the active profile: " + exception.Message);
            }
        }
    }
}
