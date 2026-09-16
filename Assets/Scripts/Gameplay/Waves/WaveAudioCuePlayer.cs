using Corebound.Audio;
using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    public sealed class WaveAudioCuePlayer : MonoBehaviour
    {
        [SerializeField] private WaveAudioCueData _audioCueData;
        [SerializeField] private AudioManager _audioManager;

        private AudioSource _audioSource;
        private AudioClip _waveStartClip;
        private AudioClip _eliteWaveClip;
        private AudioClip _waveCompleteClip;
        private AudioClip _intermissionClip;
        private AudioClip _stageCompleteClip;

        private void Awake()
        {
            EnsureAudioSource();
            RebuildClips();
        }

        private void OnEnable()
        {
            WaveEvents.WaveStarted += OnWaveStarted;
            WaveEvents.WaveCompleted += OnWaveCompleted;
            WaveEvents.IntermissionStarted += OnIntermissionStarted;
            StageEvents.StageCompleted += OnStageCompleted;
        }

        private void OnDisable()
        {
            WaveEvents.WaveStarted -= OnWaveStarted;
            WaveEvents.WaveCompleted -= OnWaveCompleted;
            WaveEvents.IntermissionStarted -= OnIntermissionStarted;
            StageEvents.StageCompleted -= OnStageCompleted;
        }

        private void OnDestroy()
        {
            DestroyProceduralClipsOnly();
        }

        private void OnWaveStarted(WaveProgressEventArgs eventArgs)
        {
            if (eventArgs.IsEliteEncounter)
            {
                PlayCue(_eliteWaveClip, _audioCueData != null ? _audioCueData.EliteWaveVolume : 0f);
                return;
            }

            PlayCue(_waveStartClip, _audioCueData != null ? _audioCueData.WaveStartVolume : 0f);
        }

        private void OnWaveCompleted(WaveProgressEventArgs eventArgs)
        {
            PlayCue(_waveCompleteClip, _audioCueData != null ? _audioCueData.WaveCompleteVolume : 0f);
        }

        private void OnIntermissionStarted(WaveProgressEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Label)
                && eventArgs.Label.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            PlayCue(_intermissionClip, _audioCueData != null ? _audioCueData.IntermissionVolume : 0f);
        }

        private void OnStageCompleted(StageEventArgs eventArgs)
        {
            PlayCue(_stageCompleteClip, _audioCueData != null ? _audioCueData.StageCompleteVolume : 0f);
        }

        private void EnsureAudioSource()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
        }

        private void RebuildClips()
        {
            DestroyProceduralClipsOnly();

            if (_audioCueData == null)
            {
                return;
            }

            _waveStartClip = _audioCueData.HasRealWaveStartClip ? _audioCueData.WaveStartClip : ProceduralToneFactory.BuildToneClip("WaveStartCue", _audioCueData.WaveStartFrequency, _audioCueData.WaveStartDurationSeconds);
            _eliteWaveClip = _audioCueData.HasRealEliteWaveClip ? _audioCueData.EliteWaveClip : ProceduralToneFactory.BuildToneClip("EliteWaveCue", _audioCueData.EliteWaveFrequency, _audioCueData.EliteWaveDurationSeconds);
            _waveCompleteClip = _audioCueData.HasRealWaveCompleteClip ? _audioCueData.WaveCompleteClip : ProceduralToneFactory.BuildToneClip("WaveCompleteCue", _audioCueData.WaveCompleteFrequency, _audioCueData.WaveCompleteDurationSeconds);
            _intermissionClip = _audioCueData.HasRealIntermissionClip ? _audioCueData.IntermissionClip : ProceduralToneFactory.BuildToneClip("IntermissionCue", _audioCueData.IntermissionFrequency, _audioCueData.IntermissionDurationSeconds);
            _stageCompleteClip = _audioCueData.HasRealStageCompleteClip ? _audioCueData.StageCompleteClip : ProceduralToneFactory.BuildToneClip("StageCompleteCue", _audioCueData.StageCompleteFrequency, _audioCueData.StageCompleteDurationSeconds);
        }

        // Real, asset-referenced clips (HasReal*Clip) must never be passed to
        // ProceduralToneFactory.DestroyClip - it unconditionally calls Object.Destroy/
        // DestroyImmediate, which is only safe for clips this player generated itself.
        // Destroying a real imported AudioClip asset object would corrupt project state.
        private void DestroyProceduralClipsOnly()
        {
            if (_audioCueData == null || !_audioCueData.HasRealWaveStartClip)
            {
                ProceduralToneFactory.DestroyClip(_waveStartClip);
            }

            if (_audioCueData == null || !_audioCueData.HasRealEliteWaveClip)
            {
                ProceduralToneFactory.DestroyClip(_eliteWaveClip);
            }

            if (_audioCueData == null || !_audioCueData.HasRealWaveCompleteClip)
            {
                ProceduralToneFactory.DestroyClip(_waveCompleteClip);
            }

            if (_audioCueData == null || !_audioCueData.HasRealIntermissionClip)
            {
                ProceduralToneFactory.DestroyClip(_intermissionClip);
            }

            if (_audioCueData == null || !_audioCueData.HasRealStageCompleteClip)
            {
                ProceduralToneFactory.DestroyClip(_stageCompleteClip);
            }
        }

        private void PlayCue(AudioClip clip, float volume)
        {
            if (_audioSource == null || clip == null || volume <= 0f)
            {
                return;
            }

            var scaledVolume = _audioManager != null ? volume * _audioManager.SfxVolume : volume;
            _audioSource.PlayOneShot(clip, scaledVolume);
        }

    }
}