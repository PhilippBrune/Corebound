using Corebound.Audio;
using UnityEngine;

namespace Corebound.Gameplay.Factory
{
    public sealed class FactoryAudioCuePlayer : MonoBehaviour
    {
        [SerializeField] private FactoryAudioCueData _audioCueData;
        [SerializeField] private AudioManager _audioManager;

        private AudioSource _audioSource;
        private AudioClip _resourceGeneratedClip;
        private float _cueCooldownTimer;

        private void Awake()
        {
            EnsureAudioSource();
            RebuildClips();
        }

        private void OnEnable()
        {
            FactoryEvents.ResourceGenerated += OnResourceGenerated;
        }

        private void OnDisable()
        {
            FactoryEvents.ResourceGenerated -= OnResourceGenerated;
        }

        private void OnDestroy()
        {
            DestroyProceduralClipsOnly();
        }

        private void Update()
        {
            if (_cueCooldownTimer > 0f)
            {
                _cueCooldownTimer -= Time.deltaTime;
            }
        }

        private void OnResourceGenerated(ResourceGeneratedEventArgs eventArgs)
        {
            // WaveCompleted-triggered production is a passive economy tick that fires in the same
            // frame as WaveAudioCuePlayer's wave-complete fanfare (both react to WaveManager
            // completing the wave) - layering a second one-shot on top of that read as "two sounds
            // playing at once" right as the Card Reward screen opens. The fanfare already covers
            // the "wave is over" beat, so skip this cue for that trigger specifically.
            if (eventArgs.TriggerType == ProductionTriggerType.WaveCompleted)
            {
                return;
            }

            if (_cueCooldownTimer > 0f)
            {
                return;
            }

            _cueCooldownTimer = _audioCueData != null ? _audioCueData.MinCueIntervalSeconds : 0.35f;
            PlayCue(_resourceGeneratedClip, _audioCueData != null ? _audioCueData.ResourceGeneratedVolume : 0f);
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

            _resourceGeneratedClip = _audioCueData.HasRealResourceGeneratedClip ? _audioCueData.ResourceGeneratedClip : ProceduralToneFactory.BuildToneClip("FactoryResourceGeneratedCue", _audioCueData.ResourceGeneratedFrequency, _audioCueData.ResourceGeneratedDurationSeconds);
        }

        private void DestroyProceduralClipsOnly()
        {
            if (_audioCueData == null || !_audioCueData.HasRealResourceGeneratedClip)
            {
                ProceduralToneFactory.DestroyClip(_resourceGeneratedClip);
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
