using System;

namespace Corebound.SaveSystem
{
    public interface ISaveManager
    {
        event Action<string, string> SaveFailed;

        event Action<string, string> LoadFailed;

        RunSaveData LoadRunSaveData();

        void SaveRunSaveData(RunSaveData saveData);

        void ResetRunSaveDataToBootstrap();

        void ClearRunSaveData();

        SettingsSaveData LoadSettingsSaveData(SettingsSaveData defaults);

        void SaveSettingsSaveData(SettingsSaveData saveData);

        MetaProgressionSaveData LoadMetaProgressionSaveData(MetaProgressionSaveData defaults);

        void SaveMetaProgressionSaveData(MetaProgressionSaveData saveData);

        StatisticsSaveData LoadStatisticsSaveData(StatisticsSaveData defaults);

        void SaveStatisticsSaveData(StatisticsSaveData saveData);

        RunHistorySaveData LoadRunHistorySaveData();

        void SaveRunHistorySaveData(RunHistorySaveData saveData);
    }
}
