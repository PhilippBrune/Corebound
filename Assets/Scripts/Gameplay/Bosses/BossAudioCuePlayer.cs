using Corebound.Audio;
using Corebound.Gameplay.Waves;
using UnityEngine;

namespace Corebound.Gameplay.Bosses
{
    public sealed class BossAudioCuePlayer : MonoBehaviour
    {
        [SerializeField] private BossAudioCueData _audioCueData;
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private WaveManager _waveManager;

        private AudioSource _audioSource;
        private AudioClip _warningClip;
        private AudioClip _spawnClip;
        private AudioClip _phaseChangeClip;
        private AudioClip _heavyAttackClip;
        private AudioClip _defeatClip;

        public BossAudioCueData AudioCueData
        {
            get => _audioCueData;
            set
            {
                _audioCueData = value;
                RebuildClips();
            }
        }

        private void Awake()
        {
            EnsureAudioSource();
            RebuildClips();
        }

        private void OnEnable()
        {
            BossEvents.OnBossSpawned += OnBossSpawned;
            BossEvents.OnBossPhaseChanged += OnBossPhaseChanged;
            BossEvents.OnBossHeavyAttack += OnBossHeavyAttack;
            BossEvents.OnBossDefeated += OnBossDefeated;
            WaveEvents.IntermissionStarted += OnIntermissionStarted;
        }

        private void OnDisable()
        {
            BossEvents.OnBossSpawned -= OnBossSpawned;
            BossEvents.OnBossPhaseChanged -= OnBossPhaseChanged;
            BossEvents.OnBossHeavyAttack -= OnBossHeavyAttack;
            BossEvents.OnBossDefeated -= OnBossDefeated;
            WaveEvents.IntermissionStarted -= OnIntermissionStarted;
        }

        private void OnDestroy()
        {
            DestroyProceduralClipsOnly();
        }

        private void OnIntermissionStarted(WaveProgressEventArgs args)
        {
            if (_waveManager != null && _waveManager.CurrentWaveNumber >= _waveManager.TotalWaveCount)
            {
                PlayCue(_warningClip, _audioCueData != null ? _audioCueData.WarningVolume : 0f);
            }
            else if (!string.IsNullOrEmpty(args.Label) && args.Label.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                PlayCue(_warningClip, _audioCueData != null ? _audioCueData.WarningVolume : 0f);
            }
        }

        private void OnBossSpawned(BossEventArgs args)
        {
            PlayCue(_spawnClip, _audioCueData != null ? _audioCueData.SpawnVolume : 0f);
        }

        private void OnBossPhaseChanged(BossEventArgs args)
        {
            PlayCue(_phaseChangeClip, _audioCueData != null ? _audioCueData.PhaseChangeVolume : 0f);
        }

        private void OnBossHeavyAttack(BossEventArgs args)
        {
            PlayCue(_heavyAttackClip, _audioCueData != null ? _audioCueData.HeavyAttackVolume : 0f);
        }

        private void OnBossDefeated(BossEventArgs args)
        {
            PlayCue(_defeatClip, _audioCueData != null ? _audioCueData.DefeatVolume : 0f);
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

            _warningClip = _audioCueData.HasRealWarningClip ? _audioCueData.WarningClip : ProceduralToneFactory.BuildToneClip("BossWarningCue", _audioCueData.WarningFrequency, _audioCueData.WarningDurationSeconds);
            _spawnClip = _audioCueData.HasRealSpawnClip ? _audioCueData.SpawnClip : ProceduralToneFactory.BuildToneClip("BossSpawnCue", _audioCueData.SpawnFrequency, _audioCueData.SpawnDurationSeconds);
            _phaseChangeClip = _audioCueData.HasRealPhaseChangeClip ? _audioCueData.PhaseChangeClip : ProceduralToneFactory.BuildToneClip("BossPhaseChangeCue", _audioCueData.PhaseChangeFrequency, _audioCueData.PhaseChangeDurationSeconds);
            _heavyAttackClip = _audioCueData.HasRealHeavyAttackClip ? _audioCueData.HeavyAttackClip : ProceduralToneFactory.BuildToneClip("BossHeavyAttackCue", _audioCueData.HeavyAttackFrequency, _audioCueData.HeavyAttackDurationSeconds);
            _defeatClip = _audioCueData.HasRealDefeatClip ? _audioCueData.DefeatClip : ProceduralToneFactory.BuildToneClip("BossDefeatCue", _audioCueData.DefeatFrequency, _audioCueData.DefeatDurationSeconds);
        }

        // Real, asset-referenced clips (HasReal*Clip) must never be passed to
        // ProceduralToneFactory.DestroyClip - it unconditionally calls Object.Destroy/
        // DestroyImmediate, which is only safe for clips this player generated itself.
        private void DestroyProceduralClipsOnly()
        {
            if (_audioCueData == null || !_audioCueData.HasRealWarningClip)
            {
                ProceduralToneFactory.DestroyClip(_warningClip);
            }

            if (_audioCueData == null || !_audioCueData.HasRealSpawnClip)
            {
                ProceduralToneFactory.DestroyClip(_spawnClip);
            }

            if (_audioCueData == null || !_audioCueData.HasRealPhaseChangeClip)
            {
                ProceduralToneFactory.DestroyClip(_phaseChangeClip);
            }

            if (_audioCueData == null || !_audioCueData.HasRealHeavyAttackClip)
            {
                ProceduralToneFactory.DestroyClip(_heavyAttackClip);
            }

            if (_audioCueData == null || !_audioCueData.HasRealDefeatClip)
            {
                ProceduralToneFactory.DestroyClip(_defeatClip);
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
