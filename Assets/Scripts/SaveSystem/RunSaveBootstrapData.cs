using UnityEngine;

namespace Corebound.SaveSystem
{
    [CreateAssetMenu(menuName = "Corebound/Save System/Run Save Bootstrap Data", fileName = "RunSaveBootstrapData")]
    public sealed class RunSaveBootstrapData : ScriptableObject
    {
        [SerializeField] private bool _seedContinueRun = true;
        [SerializeField] private string _sceneName = "SampleScene";
        [SerializeField] private int _stageIndex = 1;
        [SerializeField] private int _waveIndex = 1;
        [SerializeField] private float _playerHealthNormalized = 1f;
        [SerializeField] private float _coreHealthNormalized = 1f;
        [SerializeField] private int _walletAmount = 25;
        [SerializeField] private string _equippedWeaponId = "weapon_rapid";

        public bool SeedContinueRun => _seedContinueRun;

        public string SceneName => _sceneName;

        public int StageIndex => _stageIndex;

        public int WaveIndex => _waveIndex;

        public float PlayerHealthNormalized => _playerHealthNormalized;

        public float CoreHealthNormalized => _coreHealthNormalized;

        public int WalletAmount => _walletAmount;

        public string EquippedWeaponId => _equippedWeaponId;
    }
}