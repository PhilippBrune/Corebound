using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Waves/Wave Audio Cue Data", fileName = "WaveAudioCueData")]
    public sealed class WaveAudioCueData : ScriptableObject
    {
        [SerializeField] private AudioClip _waveStartClip;
        [SerializeField] private float _waveStartFrequency = 392f;
        [SerializeField] private float _waveStartDurationSeconds = 0.14f;
        [SerializeField] private float _waveStartVolume = 0.12f;

        [SerializeField] private AudioClip _eliteWaveClip;
        [SerializeField] private float _eliteWaveFrequency = 311.13f;
        [SerializeField] private float _eliteWaveDurationSeconds = 0.18f;
        [SerializeField] private float _eliteWaveVolume = 0.14f;

        [SerializeField] private AudioClip _waveCompleteClip;
        [SerializeField] private float _waveCompleteFrequency = 659.25f;
        [SerializeField] private float _waveCompleteDurationSeconds = 0.18f;
        [SerializeField] private float _waveCompleteVolume = 0.14f;

        [SerializeField] private AudioClip _intermissionClip;
        [SerializeField] private float _intermissionFrequency = 523.25f;
        [SerializeField] private float _intermissionDurationSeconds = 0.12f;
        [SerializeField] private float _intermissionVolume = 0.1f;

        [SerializeField] private AudioClip _stageCompleteClip;
        [SerializeField] private float _stageCompleteFrequency = 783.99f;
        [SerializeField] private float _stageCompleteDurationSeconds = 0.26f;
        [SerializeField] private float _stageCompleteVolume = 0.18f;

        public AudioClip WaveStartClip => _waveStartClip;

        public bool HasRealWaveStartClip => _waveStartClip != null;

        public float WaveStartFrequency => _waveStartFrequency;

        public float WaveStartDurationSeconds => _waveStartDurationSeconds;

        public float WaveStartVolume => _waveStartVolume;

        public AudioClip EliteWaveClip => _eliteWaveClip;

        public bool HasRealEliteWaveClip => _eliteWaveClip != null;

        public float EliteWaveFrequency => _eliteWaveFrequency;

        public float EliteWaveDurationSeconds => _eliteWaveDurationSeconds;

        public float EliteWaveVolume => _eliteWaveVolume;

        public AudioClip WaveCompleteClip => _waveCompleteClip;

        public bool HasRealWaveCompleteClip => _waveCompleteClip != null;

        public float WaveCompleteFrequency => _waveCompleteFrequency;

        public float WaveCompleteDurationSeconds => _waveCompleteDurationSeconds;

        public float WaveCompleteVolume => _waveCompleteVolume;

        public AudioClip IntermissionClip => _intermissionClip;

        public bool HasRealIntermissionClip => _intermissionClip != null;

        public float IntermissionFrequency => _intermissionFrequency;

        public float IntermissionDurationSeconds => _intermissionDurationSeconds;

        public float IntermissionVolume => _intermissionVolume;

        public AudioClip StageCompleteClip => _stageCompleteClip;

        public bool HasRealStageCompleteClip => _stageCompleteClip != null;

        public float StageCompleteFrequency => _stageCompleteFrequency;

        public float StageCompleteDurationSeconds => _stageCompleteDurationSeconds;

        public float StageCompleteVolume => _stageCompleteVolume;
    }
}
