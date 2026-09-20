using UnityEngine;

namespace Corebound.Gameplay.Bosses
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Bosses/Boss Audio Cue Data", fileName = "BossAudioCueData")]
    public sealed class BossAudioCueData : ScriptableObject
    {
        [SerializeField] private AudioClip _warningClip;
        [SerializeField] private float _warningFrequency = 220f;
        [SerializeField] private float _warningDurationSeconds = 0.25f;
        [SerializeField] private float _warningVolume = 0.15f;

        [SerializeField] private AudioClip _spawnClip;
        [SerializeField] private float _spawnFrequency = 180f;
        [SerializeField] private float _spawnDurationSeconds = 0.35f;
        [SerializeField] private float _spawnVolume = 0.2f;

        [SerializeField] private AudioClip _phaseChangeClip;
        [SerializeField] private float _phaseChangeFrequency = 440f;
        [SerializeField] private float _phaseChangeDurationSeconds = 0.2f;
        [SerializeField] private float _phaseChangeVolume = 0.18f;

        // Lowest and longest of the five on purpose: a Grossangriff warning has to be
        // distinguishable from the phase-change chime while the same fight is still going on.
        [SerializeField] private AudioClip _heavyAttackClip;
        [SerializeField] private float _heavyAttackFrequency = 138.59f;
        [SerializeField] private float _heavyAttackDurationSeconds = 0.3f;
        [SerializeField] private float _heavyAttackVolume = 0.17f;

        [SerializeField] private AudioClip _defeatClip;
        [SerializeField] private float _defeatFrequency = 587.33f;
        [SerializeField] private float _defeatDurationSeconds = 0.4f;
        [SerializeField] private float _defeatVolume = 0.25f;

        public AudioClip WarningClip => _warningClip;

        public bool HasRealWarningClip => _warningClip != null;

        public float WarningFrequency => _warningFrequency;

        public float WarningDurationSeconds => _warningDurationSeconds;

        public float WarningVolume => _warningVolume;

        public AudioClip SpawnClip => _spawnClip;

        public bool HasRealSpawnClip => _spawnClip != null;

        public float SpawnFrequency => _spawnFrequency;

        public float SpawnDurationSeconds => _spawnDurationSeconds;

        public float SpawnVolume => _spawnVolume;

        public AudioClip PhaseChangeClip => _phaseChangeClip;

        public bool HasRealPhaseChangeClip => _phaseChangeClip != null;

        public float PhaseChangeFrequency => _phaseChangeFrequency;

        public float PhaseChangeDurationSeconds => _phaseChangeDurationSeconds;

        public float PhaseChangeVolume => _phaseChangeVolume;

        public AudioClip HeavyAttackClip => _heavyAttackClip;

        public bool HasRealHeavyAttackClip => _heavyAttackClip != null;

        public float HeavyAttackFrequency => _heavyAttackFrequency;

        public float HeavyAttackDurationSeconds => _heavyAttackDurationSeconds;

        public float HeavyAttackVolume => _heavyAttackVolume;

        public AudioClip DefeatClip => _defeatClip;

        public bool HasRealDefeatClip => _defeatClip != null;

        public float DefeatFrequency => _defeatFrequency;

        public float DefeatDurationSeconds => _defeatDurationSeconds;

        public float DefeatVolume => _defeatVolume;
    }
}
