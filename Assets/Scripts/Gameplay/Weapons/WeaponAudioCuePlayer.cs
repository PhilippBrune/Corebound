using System.Collections.Generic;
using Corebound.Audio;
using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    // Weapons.md, Audio: "Jede Weapon besitzt eindeutige Feuer-, Treffer- und kritische
    // Feedback-Cues" and "Unterschiedliche Weapon-Archetypen muessen akustisch unterscheidbar
    // bleiben, auch wenn mehrere Tower gleichzeitig feuern." Both depend on WeaponEvents, which is
    // why this had no data source before and the spec's Audio section was unimplemented.
    //
    // Cues are procedural tones keyed off each weapon's WeaponPresentationData frequency until real
    // clips exist, matching FactoryAudioCuePlayer/WaveAudioCuePlayer.
    public sealed class WeaponAudioCuePlayer : MonoBehaviour
    {
        [SerializeField] private AudioManager _audioManager;

        // Clips are cached per presentation asset rather than per weapon: two weapons sharing a
        // presentation intentionally sound the same, and rebuilding a tone per shot would allocate
        // an AudioClip several times a second.
        private readonly Dictionary<WeaponPresentationData, AudioClip> _fireClips = new Dictionary<WeaponPresentationData, AudioClip>();
        private readonly Dictionary<WeaponPresentationData, AudioClip> _criticalClips = new Dictionary<WeaponPresentationData, AudioClip>();

        private AudioSource _audioSource;
        private float _fireCueCooldownTimer;

        private void Awake()
        {
            EnsureAudioSource();
        }

        private void OnEnable()
        {
            WeaponEvents.WeaponFired += OnWeaponFired;
        }

        private void OnDisable()
        {
            WeaponEvents.WeaponFired -= OnWeaponFired;
        }

        private void OnDestroy()
        {
            DestroyProceduralClips(_fireClips);
            DestroyProceduralClips(_criticalClips);
        }

        private void Update()
        {
            if (_fireCueCooldownTimer > 0f)
            {
                _fireCueCooldownTimer -= Time.deltaTime;
            }
        }

        private void OnWeaponFired(WeaponFiredEventArgs eventArgs)
        {
            var presentation = eventArgs.Weapon != null ? eventArgs.Weapon.Presentation : null;

            if (presentation == null)
            {
                return;
            }

            // The critical cue is the whole point of a crit landing, so it is never rate-limited -
            // it also replaces the fire cue for that shot rather than layering on top of it.
            if (eventArgs.IsCritical)
            {
                PlayCue(ResolveClip(presentation, true), presentation.CriticalVolume);
                _fireCueCooldownTimer = presentation.MinFireCueIntervalSeconds;
                return;
            }

            if (_fireCueCooldownTimer > 0f)
            {
                return;
            }

            _fireCueCooldownTimer = presentation.MinFireCueIntervalSeconds;
            PlayCue(ResolveClip(presentation, false), presentation.FireVolume);
        }

        private AudioClip ResolveClip(WeaponPresentationData presentation, bool isCritical)
        {
            var cache = isCritical ? _criticalClips : _fireClips;

            if (cache.TryGetValue(presentation, out var cachedClip) && cachedClip != null)
            {
                return cachedClip;
            }

            AudioClip clip;

            if (isCritical)
            {
                clip = presentation.HasRealCriticalClip
                    ? presentation.CriticalClip
                    : ProceduralToneFactory.BuildToneClip(presentation.PresentationId + "_CriticalCue", presentation.CriticalFrequency, presentation.CriticalDurationSeconds);
            }
            else
            {
                clip = presentation.HasRealFireClip
                    ? presentation.FireClip
                    : ProceduralToneFactory.BuildToneClip(presentation.PresentationId + "_FireCue", presentation.FireFrequency, presentation.FireDurationSeconds);
            }

            cache[presentation] = clip;
            return clip;
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

        // Only tones this player generated are destroyed; an authored AudioClip asset belongs to
        // the project, not to this component.
        private void DestroyProceduralClips(Dictionary<WeaponPresentationData, AudioClip> cache)
        {
            foreach (var pair in cache)
            {
                var presentation = pair.Key;

                if (presentation == null)
                {
                    continue;
                }

                var isAuthored = cache == _criticalClips ? presentation.HasRealCriticalClip : presentation.HasRealFireClip;

                if (!isAuthored)
                {
                    ProceduralToneFactory.DestroyClip(pair.Value);
                }
            }

            cache.Clear();
        }
    }
}
