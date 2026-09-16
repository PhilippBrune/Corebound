using System.Collections.Generic;
using Corebound.Gameplay.Bosses;
using Corebound.Gameplay.Waves;
using Corebound.Settings;
using UnityEngine;

namespace Corebound.Audio
{
    // Audio.md calls for a music-state system (normal wave / eskalation / boss) with ducking
    // priority for danger signals; this is the missing piece - AudioManager already centralizes
    // one-shot Combat SFX, but nothing tracked an ongoing music state before. Two looping
    // AudioSources crossfade between MusicStateData entries driven by real Wave/Boss events.
    // Real composed loops slot into MusicStateData.Clip once they exist; until then each state
    // plays a distinct, quiet procedural drone so the states are already audible today.
    public sealed class MusicStateController : MonoBehaviour
    {
        [SerializeField] private MusicStateData[] _states;
        [SerializeField] private SettingsManager _settingsManager;

        private readonly Dictionary<MusicStateData, AudioClip> _proceduralClipCache = new Dictionary<MusicStateData, AudioClip>();

        private AudioSource _activeSource;
        private AudioSource _fadingSource;
        private MusicStateData _currentStateData;
        private float _blendTimer;
        private float _blendDuration;
        private float _activeTargetVolume;
        private float _fadingStartVolume;
        private bool _isFading;

        private void Awake()
        {
            _activeSource = gameObject.AddComponent<AudioSource>();
            _fadingSource = gameObject.AddComponent<AudioSource>();
            ConfigureSource(_activeSource);
            ConfigureSource(_fadingSource);
        }

        private void OnEnable()
        {
            WaveEvents.WaveStarted += OnWaveStarted;
            WaveEvents.IntermissionStarted += OnIntermissionStarted;
            WaveEvents.StageCompleted += OnStageCompleted;
            BossEvents.OnBossSpawned += OnBossSpawned;
            BossEvents.OnBossDefeated += OnBossDefeated;

            SwitchToState(MusicState.Calm);
        }

        private void OnDisable()
        {
            WaveEvents.WaveStarted -= OnWaveStarted;
            WaveEvents.IntermissionStarted -= OnIntermissionStarted;
            WaveEvents.StageCompleted -= OnStageCompleted;
            BossEvents.OnBossSpawned -= OnBossSpawned;
            BossEvents.OnBossDefeated -= OnBossDefeated;
        }

        private void OnDestroy()
        {
            foreach (var pair in _proceduralClipCache)
            {
                ProceduralToneFactory.DestroyClip(pair.Value);
            }

            _proceduralClipCache.Clear();
        }

        private void Update()
        {
            var musicVolume = _settingsManager != null && _settingsManager.RuntimeSettings != null ? _settingsManager.RuntimeSettings.MusicVolume : 1f;

            if (!_isFading)
            {
                _activeSource.volume = _activeTargetVolume * musicVolume;
                return;
            }

            _blendTimer += Time.deltaTime;
            var t = _blendDuration > 0f ? Mathf.Clamp01(_blendTimer / _blendDuration) : 1f;
            _activeSource.volume = Mathf.Lerp(0f, _activeTargetVolume, t) * musicVolume;
            _fadingSource.volume = Mathf.Lerp(_fadingStartVolume, 0f, t) * musicVolume;

            if (t >= 1f)
            {
                _isFading = false;
                _fadingSource.Stop();
            }
        }

        private void OnWaveStarted(WaveProgressEventArgs eventArgs)
        {
            SwitchToState(MusicState.Combat);
        }

        private void OnIntermissionStarted(WaveProgressEventArgs eventArgs)
        {
            SwitchToState(MusicState.Calm);
        }

        private void OnStageCompleted(WaveProgressEventArgs eventArgs)
        {
            SwitchToState(MusicState.Calm);
        }

        private void OnBossSpawned(BossEventArgs eventArgs)
        {
            SwitchToState(MusicState.Boss);
        }

        private void OnBossDefeated(BossEventArgs eventArgs)
        {
            SwitchToState(MusicState.Calm);
        }

        private void SwitchToState(MusicState state)
        {
            var data = ResolveStateData(state);

            if (data == null || data == _currentStateData)
            {
                return;
            }

            var previousActive = _activeSource;
            _activeSource = _fadingSource;
            _fadingSource = previousActive;

            PlayOnSource(_activeSource, data);

            _fadingStartVolume = _fadingSource.volume;
            _activeTargetVolume = data.Volume;
            _blendDuration = data.CrossfadeSeconds;
            _blendTimer = 0f;
            _isFading = true;
            _currentStateData = data;
        }

        private void PlayOnSource(AudioSource source, MusicStateData data)
        {
            source.clip = ResolveClip(data);
            source.volume = 0f;
            source.Play();
        }

        private AudioClip ResolveClip(MusicStateData data)
        {
            if (data.HasRealClip)
            {
                return data.Clip;
            }

            if (_proceduralClipCache.TryGetValue(data, out var cachedClip) && cachedClip != null)
            {
                return cachedClip;
            }

            var clip = ProceduralToneFactory.BuildToneClip("Music_" + data.State, data.ProceduralFrequency, data.ProceduralLoopSeconds);
            _proceduralClipCache[data] = clip;
            return clip;
        }

        private MusicStateData ResolveStateData(MusicState state)
        {
            if (_states == null)
            {
                return null;
            }

            for (var index = 0; index < _states.Length; index++)
            {
                if (_states[index] != null && _states[index].State == state)
                {
                    return _states[index];
                }
            }

            return null;
        }

        private static void ConfigureSource(AudioSource source)
        {
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
        }
    }
}
