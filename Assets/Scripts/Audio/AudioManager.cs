using System.Collections.Generic;
using Corebound.Gameplay.Combat;
using Corebound.Settings;
using UnityEngine;

namespace Corebound.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private SettingsManager _settingsManager;
        [SerializeField] private AudioCueData _combatHitCue;
        [SerializeField] private AudioCueData _combatDeathCue;

        private readonly Dictionary<AudioCueData, AudioClip[]> _proceduralClipCache = new Dictionary<AudioCueData, AudioClip[]>();

        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private float _uiVolume = 1f;
        private AudioSource _sfxSource;

        public float MusicVolume => _musicVolume;

        public float SfxVolume => _sfxVolume;

        public float UiVolume => _uiVolume;

        private void Awake()
        {
            EnsureSfxSource();
            ApplyRuntimeSettings();
        }

        private void OnEnable()
        {
            if (_settingsManager != null)
            {
                _settingsManager.SettingsApplied += ApplyRuntimeSettings;
            }

            ApplyRuntimeSettings();

            // AudioManager now receives Combat events directly rather than only exposing
            // volume scalars for other classes to read - the central, event-driven role
            // Audio.md's Technical Design section calls for. Boss/Wave/MainMenu keep their own
            // dedicated cue players for now (each reacts to feature-specific events an
            // Enum-keyed AudioCueData catalog does not cover yet); this covers the two
            // universal Combat SFX events every combatant already raises.
            CombatEvents.DamageTaken += OnDamageTaken;
            CombatEvents.CombatantDied += OnCombatantDied;
        }

        private void OnDisable()
        {
            if (_settingsManager != null)
            {
                _settingsManager.SettingsApplied -= ApplyRuntimeSettings;
            }

            CombatEvents.DamageTaken -= OnDamageTaken;
            CombatEvents.CombatantDied -= OnCombatantDied;
        }

        private void OnDestroy()
        {
            foreach (var pair in _proceduralClipCache)
            {
                var clips = pair.Value;

                if (clips == null)
                {
                    continue;
                }

                for (var index = 0; index < clips.Length; index++)
                {
                    ProceduralToneFactory.DestroyClip(clips[index]);
                }
            }

            _proceduralClipCache.Clear();
        }

        private void OnDamageTaken(DamageEventArgs eventArgs)
        {
            PlayCue(_combatHitCue);
        }

        private void OnCombatantDied(DeathEventArgs eventArgs)
        {
            PlayCue(_combatDeathCue);
        }

        // A static entry for systems that own their own cue references rather than being wired
        // through this component's serialized fields. Arena objectives are the first: the cue lives
        // on ArenaObjectiveData beside the rest of that objective's tuning, which keeps a new
        // objective type a pure data addition instead of three more fields here and three more
        // scene references (2026-09-28).
        private static AudioManager _instance;

        public static void PlayCueStatic(AudioCueData cue)
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>();
            }

            if (_instance != null)
            {
                _instance.PlayCue(cue);
            }
        }

        private void PlayCue(AudioCueData cue)
        {
            if (cue == null || _sfxSource == null || cue.Volume <= 0f)
            {
                return;
            }

            _sfxSource.pitch = cue.ResolveRandomPitch();

            if (cue.HasRealClips)
            {
                var clips = cue.ClipVariations;
                var clip = clips[Random.Range(0, clips.Length)];
                _sfxSource.PlayOneShot(clip, cue.Volume * _sfxVolume);
                return;
            }

            var proceduralClip = ResolveProceduralVariation(cue);

            if (proceduralClip != null)
            {
                _sfxSource.PlayOneShot(proceduralClip, cue.Volume * _sfxVolume);
            }
        }

        private AudioClip ResolveProceduralVariation(AudioCueData cue)
        {
            var frequencies = cue.FrequencyVariations;

            if (frequencies == null || frequencies.Length == 0)
            {
                return null;
            }

            if (!_proceduralClipCache.TryGetValue(cue, out var clips) || clips == null || clips.Length != frequencies.Length)
            {
                clips = new AudioClip[frequencies.Length];

                for (var index = 0; index < frequencies.Length; index++)
                {
                    clips[index] = ProceduralToneFactory.BuildToneClip(cue.name + "_" + index, frequencies[index], cue.DurationSeconds);
                }

                _proceduralClipCache[cue] = clips;
            }

            return clips[Random.Range(0, clips.Length)];
        }

        private void EnsureSfxSource()
        {
            _sfxSource = GetComponent<AudioSource>();

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }

            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;
        }

        private void ApplyRuntimeSettings()
        {
            var runtimeSettings = _settingsManager != null ? _settingsManager.RuntimeSettings : null;

            _musicVolume = runtimeSettings != null ? runtimeSettings.MusicVolume : 1f;
            _sfxVolume = runtimeSettings != null ? runtimeSettings.SfxVolume : 1f;
            _uiVolume = runtimeSettings != null ? runtimeSettings.UiVolume : 1f;

            AudioListener.volume = Mathf.Clamp01(Mathf.Max(_musicVolume, Mathf.Max(_sfxVolume, _uiVolume)));
        }
    }
}
