using System;
using System.IO;
using UnityEngine;

namespace Corebound.SaveSystem
{
    public sealed class LocalProfileSaveManager : MonoBehaviour, ISaveManager
    {
        private const string RunFileName = "run.json";
        private const string SettingsFileName = "settings.json";
        private const string MetaFileName = "meta.json";
        private const string StatisticsFileName = "statistics.json";

        // Every *SaveData class stamps this into its own SaveFormatVersion field. No format
        // change has shipped yet, so this never fires today; it exists so a future schema change
        // is a loud warning instead of JsonUtility silently defaulting the new fields on old saves.
        private const int CurrentSaveFormatVersion = 1;

        [SerializeField] private string _profileFolderName = "Profile_01";
        [SerializeField] private RunSaveBootstrapData _runBootstrapData;

        private readonly JsonFileSaveStore _saveStore = new JsonFileSaveStore();

        public static LocalProfileSaveManager Instance { get; private set; }

        public event Action<string, string> SaveFailed;

        public event Action<string, string> LoadFailed;

        private RunSaveData _runSaveData;
        private SettingsSaveData _settingsSaveData;
        private MetaProgressionSaveData _metaProgressionSaveData;
        private StatisticsSaveData _statisticsSaveData;

        // Every scene carries a LocalProfileSaveManager on a shared GameObject (MainMenu's
        // MainMenuStateProvider, SampleScene's CombatRuntime), but exactly one of them may own the
        // caches. That one lives on its own host object; every other instance forwards to it.
        //
        // Playtest 2026-09-22 ("after death there are still 2 sounds playing in the main menu",
        // traced with [AudioProbe]): this used to call DontDestroyOnLoad on its own GameObject,
        // which on MainMenuStateProvider also dragged AudioManager, MusicStateController,
        // MetaProgressionService, SettingsManager and InputRebindManager into DontDestroyOnLoad.
        // Every later MainMenu load added a second set, so two music loops played (and a stale
        // MetaProgressionService survived). Duplicates also used to Destroy(this), which left the
        // scene's references pointing at a destroyed component with its own, separate caches.
        private LocalProfileSaveManager _forwardTo;

        private static string _hostProfileFolderName;
        private static RunSaveBootstrapData _hostRunBootstrapData;
        private static bool _creatingHost;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                ForwardTo(Instance);
                return;
            }

            if (!_creatingHost && gameObject.GetComponents<Component>().Length > 2)
            {
                // Shared GameObject: persist a dedicated host instead of everything on this one.
                _hostProfileFolderName = _profileFolderName;
                _hostRunBootstrapData = _runBootstrapData;
                _creatingHost = true;
                var host = new GameObject("LocalProfileSaveManager (persistent)");
                DontDestroyOnLoad(host);
                var persistent = host.AddComponent<LocalProfileSaveManager>();
                _creatingHost = false;
                ForwardTo(persistent);
                return;
            }

            if (_creatingHost)
            {
                _profileFolderName = _hostProfileFolderName;
                _runBootstrapData = _hostRunBootstrapData;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _saveStore.SaveFailed += (path, message) => SaveFailed?.Invoke(path, message);
            _saveStore.LoadFailed += (path, message) => LoadFailed?.Invoke(path, message);
        }

        private void ForwardTo(LocalProfileSaveManager target)
        {
            _forwardTo = target;
            target.SaveFailed += (path, message) => SaveFailed?.Invoke(path, message);
            target.LoadFailed += (path, message) => LoadFailed?.Invoke(path, message);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public RunSaveData LoadRunSaveData()
        {
            if (_forwardTo != null)
            {
                return _forwardTo.LoadRunSaveData();
            }

            if (_runSaveData != null)
            {
                return _runSaveData;
            }

            if (_saveStore.TryLoad(GetRunPath(), out _runSaveData))
            {
                if (_runSaveData.MigrateToCurrentFormat())
                {
                    Debug.Log("[Save] Migrated run save to format " + RunSaveData.CurrentSaveFormatVersion + " (single weapon -> slot 0).");
                    _saveStore.Save(GetRunPath(), _runSaveData);
                }

                WarnIfVersionMismatch("run", _runSaveData.SaveFormatVersion, RunSaveData.CurrentSaveFormatVersion);
                return _runSaveData;
            }

            _runSaveData = RunSaveData.CreateBootstrap(_runBootstrapData);
            SaveRunSaveData(_runSaveData);
            return _runSaveData;
        }

        public void SaveRunSaveData(RunSaveData saveData)
        {
            if (_forwardTo != null)
            {
                _forwardTo.SaveRunSaveData(saveData);
                return;
            }

            _runSaveData = saveData ?? RunSaveData.CreateBootstrap(_runBootstrapData);
            _runSaveData.Touch();
            _saveStore.Save(GetRunPath(), _runSaveData);
        }

        public void ResetRunSaveDataToBootstrap()
        {
            if (_forwardTo != null)
            {
                _forwardTo.ResetRunSaveDataToBootstrap();
                return;
            }

            _runSaveData = RunSaveData.CreateBootstrap(_runBootstrapData);
            SaveRunSaveData(_runSaveData);
        }

        public void ClearRunSaveData()
        {
            if (_forwardTo != null)
            {
                _forwardTo.ClearRunSaveData();
                return;
            }

            _runSaveData = new RunSaveData();
            _runSaveData.SetInactive();
            _saveStore.Save(GetRunPath(), _runSaveData);
        }

        public SettingsSaveData LoadSettingsSaveData(SettingsSaveData defaults)
        {
            if (_forwardTo != null)
            {
                return _forwardTo.LoadSettingsSaveData(defaults);
            }

            if (_settingsSaveData != null)
            {
                return _settingsSaveData;
            }

            if (_saveStore.TryLoad(GetSettingsPath(), out _settingsSaveData))
            {
                WarnIfVersionMismatch("settings", _settingsSaveData.SaveFormatVersion);
                return _settingsSaveData;
            }

            _settingsSaveData = defaults != null ? defaults.Clone() : new SettingsSaveData();
            SaveSettingsSaveData(_settingsSaveData);
            return _settingsSaveData;
        }

        public void SaveSettingsSaveData(SettingsSaveData saveData)
        {
            if (_forwardTo != null)
            {
                _forwardTo.SaveSettingsSaveData(saveData);
                return;
            }

            _settingsSaveData = saveData != null ? saveData.Clone() : new SettingsSaveData();
            _saveStore.Save(GetSettingsPath(), _settingsSaveData);
        }

        public MetaProgressionSaveData LoadMetaProgressionSaveData(MetaProgressionSaveData defaults)
        {
            if (_forwardTo != null)
            {
                return _forwardTo.LoadMetaProgressionSaveData(defaults);
            }

            if (_metaProgressionSaveData != null)
            {
                return _metaProgressionSaveData;
            }

            if (_saveStore.TryLoad(GetMetaPath(), out _metaProgressionSaveData))
            {
                // v1 -> v2 (Alloy, Skill Points, upgrades). Written back at once so every later
                // reader - including the gameplay scene's direct save reads - sees one format.
                if (_metaProgressionSaveData.MigrateToCurrentFormat())
                {
                    Debug.Log("[Save] Migrated meta progression save to format " + MetaProgressionSaveData.CurrentSaveFormatVersion + ".");
                    _saveStore.Save(GetMetaPath(), _metaProgressionSaveData);
                }

                WarnIfVersionMismatch("meta", _metaProgressionSaveData.SaveFormatVersion, MetaProgressionSaveData.CurrentSaveFormatVersion);
                return _metaProgressionSaveData;
            }

            _metaProgressionSaveData = defaults != null ? defaults.Clone() : new MetaProgressionSaveData();
            SaveMetaProgressionSaveData(_metaProgressionSaveData);
            return _metaProgressionSaveData;
        }

        public void SaveMetaProgressionSaveData(MetaProgressionSaveData saveData)
        {
            if (_forwardTo != null)
            {
                _forwardTo.SaveMetaProgressionSaveData(saveData);
                return;
            }

            _metaProgressionSaveData = saveData != null ? saveData.Clone() : new MetaProgressionSaveData();
            _saveStore.Save(GetMetaPath(), _metaProgressionSaveData);
        }

        public StatisticsSaveData LoadStatisticsSaveData(StatisticsSaveData defaults)
        {
            if (_forwardTo != null)
            {
                return _forwardTo.LoadStatisticsSaveData(defaults);
            }

            if (_statisticsSaveData != null)
            {
                return _statisticsSaveData;
            }

            if (_saveStore.TryLoad(GetStatisticsPath(), out _statisticsSaveData))
            {
                WarnIfVersionMismatch("statistics", _statisticsSaveData.SaveFormatVersion);
                return _statisticsSaveData;
            }

            _statisticsSaveData = defaults != null ? defaults.Clone() : new StatisticsSaveData();
            SaveStatisticsSaveData(_statisticsSaveData);
            return _statisticsSaveData;
        }

        public void SaveStatisticsSaveData(StatisticsSaveData saveData)
        {
            if (_forwardTo != null)
            {
                _forwardTo.SaveStatisticsSaveData(saveData);
                return;
            }

            _statisticsSaveData = saveData != null ? saveData.Clone() : new StatisticsSaveData();
            _saveStore.Save(GetStatisticsPath(), _statisticsSaveData);
        }

        private static void WarnIfVersionMismatch(string saveName, int loadedVersion, int expectedVersion = CurrentSaveFormatVersion)
        {
            if (loadedVersion != expectedVersion)
            {
                Debug.LogWarning("Corebound: '" + saveName + "' save data has format version " + loadedVersion
                    + " but the game expects " + expectedVersion + ". Loaded fields not present in the old format used their C# defaults with no migration applied.");
            }
        }

        private string GetRunPath()
        {
            return Path.Combine(GetProfileRootPath(), RunFileName);
        }

        private string GetSettingsPath()
        {
            return Path.Combine(GetProfileRootPath(), SettingsFileName);
        }

        private string GetMetaPath()
        {
            return Path.Combine(GetProfileRootPath(), MetaFileName);
        }

        private string GetStatisticsPath()
        {
            return Path.Combine(GetProfileRootPath(), StatisticsFileName);
        }

        private string GetProfileRootPath()
        {
            return Path.Combine(Application.persistentDataPath, _profileFolderName);
        }
    }
}