using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corebound.SaveSystem
{
    public sealed class LocalProfileSaveManager : MonoBehaviour, ISaveManager
    {
        private const string RunFileName = "run.json";
        private const string SettingsFileName = "settings.json";
        private const string MetaFileName = "meta.json";
        private const string StatisticsFileName = "statistics.json";

        // D5: run history is its own file. statistics.json is rewritten on every enemy kill, so
        // an unbounded array living there would re-serialise the whole history per death.
        private const string RunHistoryFileName = "history.json";

        // Every *SaveData class stamps this into its own SaveFormatVersion field. No format
        // change has shipped yet, so this never fires today; it exists so a future schema change
        // is a loud warning instead of JsonUtility silently defaulting the new fields on old saves.
        private const int CurrentSaveFormatVersion = 1;

        private RunHistorySaveData _runHistorySaveData;

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

        // ---- Write batching (playtest 2026-09-29, "the aim jumps for some frames") -------------
        //
        // Every enemy kill wrote meta.json and statistics.json, and every hit on the player or Core
        // wrote run.json - each a JSON serialise, a temp file and an atomic File.Replace, on the
        // main thread. With antivirus scanning every write that is milliseconds per file, several
        // times a second in a busy wave, and the frame hitches were what made the software-drawn
        // reticle jump.
        //
        // The caches were already the source of truth for every reader, so a Save now updates the
        // cache at once and only the disk write waits: all pending files go out together at most
        // WriteCoalesceSeconds later, and immediately on a scene change, a profile switch or
        // delete, pause and quit. A crash loses at most that window. The path is captured when the
        // write is queued, so a profile switch can never send one profile's data to another.
        private const float WriteCoalesceSeconds = 1.5f;
        private readonly Dictionary<string, object> _pendingWrites = new Dictionary<string, object>();
        private float _flushDueAt;

        private void QueueWrite(string path, object data)
        {
            if (_pendingWrites.Count == 0)
            {
                _flushDueAt = Time.unscaledTime + WriteCoalesceSeconds;
            }

            _pendingWrites[path] = data;
        }

        // Writes everything queued. Safe to call at any time; readers never need it, because they
        // read the caches, but anything that reads the files directly does.
        public void FlushPendingWrites()
        {
            if (_forwardTo != null)
            {
                _forwardTo.FlushPendingWrites();
                return;
            }

            if (_pendingWrites.Count == 0)
            {
                return;
            }

            foreach (var pair in _pendingWrites)
            {
                _saveStore.Save(pair.Key, pair.Value);
            }

            _pendingWrites.Clear();
        }

        private void Update()
        {
            if (_forwardTo == null && _pendingWrites.Count > 0 && Time.unscaledTime >= _flushDueAt)
            {
                FlushPendingWrites();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                FlushPendingWrites();
            }
        }

        private void OnApplicationQuit()
        {
            FlushPendingWrites();
        }

        private void OnActiveSceneChanged(Scene from, Scene to)
        {
            FlushPendingWrites();
        }

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
            ProfileSelection.ActiveProfileChanged += OnActiveProfileChanged;
            ProfileSelection.FlushPendingWritesRequested += FlushPendingWrites;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            _saveStore.SaveFailed += (path, message) => SaveFailed?.Invoke(path, message);
            _saveStore.LoadFailed += (path, message) => LoadFailed?.Invoke(path, message);
        }

        private void ForwardTo(LocalProfileSaveManager target)
        {
            _forwardTo = target;
            target.SaveFailed += (path, message) => SaveFailed?.Invoke(path, message);
            target.LoadFailed += (path, message) => LoadFailed?.Invoke(path, message);
        }

        // Switching profile must not leave another profile's data in the caches.
        private void OnActiveProfileChanged()
        {
            _runSaveData = null;
            _settingsSaveData = null;
            _metaProgressionSaveData = null;
            _statisticsSaveData = null;
            _runHistorySaveData = null;
        }

        private void OnDestroy()
        {
            FlushPendingWrites();
            ProfileSelection.ActiveProfileChanged -= OnActiveProfileChanged;
            ProfileSelection.FlushPendingWritesRequested -= FlushPendingWrites;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;

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
            QueueWrite(GetRunPath(), _runSaveData);
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
            QueueWrite(GetRunPath(), _runSaveData);
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
            QueueWrite(GetSettingsPath(), _settingsSaveData);
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

            // A new profile goes through the same pass as a loaded one: it is owed its level-1
            // Skill Points (levelling v3), which only the migration pass pays.
            _metaProgressionSaveData.MigrateToCurrentFormat();
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
            QueueWrite(GetMetaPath(), _metaProgressionSaveData);
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
            QueueWrite(GetStatisticsPath(), _statisticsSaveData);
        }

        public RunHistorySaveData LoadRunHistorySaveData()
        {
            if (_forwardTo != null)
            {
                return _forwardTo.LoadRunHistorySaveData();
            }

            if (_runHistorySaveData != null)
            {
                return _runHistorySaveData;
            }

            if (_saveStore.TryLoad(GetRunHistoryPath(), out _runHistorySaveData))
            {
                WarnIfVersionMismatch("history", _runHistorySaveData.SaveFormatVersion);
                return _runHistorySaveData;
            }

            // Deliberately not written on first load: an empty history file for a profile that has
            // never finished a run is noise, and the first completed run creates it.
            _runHistorySaveData = new RunHistorySaveData();
            return _runHistorySaveData;
        }

        public void SaveRunHistorySaveData(RunHistorySaveData saveData)
        {
            if (_forwardTo != null)
            {
                _forwardTo.SaveRunHistorySaveData(saveData);
                return;
            }

            _runHistorySaveData = saveData != null ? saveData.Clone() : new RunHistorySaveData();
            QueueWrite(GetRunHistoryPath(), _runHistorySaveData);
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

        private string GetRunHistoryPath()
        {
            return Path.Combine(GetProfileRootPath(), RunHistoryFileName);
        }

        private string GetProfileRootPath()
        {
            // SaveSystem.md "Drei Profile": the folder follows the active profile, so all four save
            // domains switch together. _profileFolderName stays as the fallback for a scene authored
            // before profiles existed.
            var folder = ProfileSelection.ActiveFolderName;

            return Path.Combine(Application.persistentDataPath, string.IsNullOrWhiteSpace(folder) ? _profileFolderName : folder);
        }
    }
}