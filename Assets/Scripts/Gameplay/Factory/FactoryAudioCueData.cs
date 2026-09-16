using UnityEngine;

namespace Corebound.Gameplay.Factory
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Factory/Factory Audio Cue Data", fileName = "FactoryAudioCueData")]
    public sealed class FactoryAudioCueData : ScriptableObject
    {
        [SerializeField] private AudioClip _resourceGeneratedClip;
        [SerializeField] private float _resourceGeneratedFrequency = 587.33f;
        [SerializeField] private float _resourceGeneratedDurationSeconds = 0.08f;
        [SerializeField] private float _resourceGeneratedVolume = 0.08f;

        // Factory.md: "Hochfrequente Produktion muss gruppiert oder priorisiert werden, um
        // Audio-Spam zu verhindern." EnemyKilled nodes can fire many times a second in a dense
        // wave, so cues are rate-limited to at most one every this many seconds.
        [SerializeField] private float _minCueIntervalSeconds = 0.35f;

        public AudioClip ResourceGeneratedClip => _resourceGeneratedClip;

        public bool HasRealResourceGeneratedClip => _resourceGeneratedClip != null;

        public float ResourceGeneratedFrequency => _resourceGeneratedFrequency;

        public float ResourceGeneratedDurationSeconds => _resourceGeneratedDurationSeconds;

        public float ResourceGeneratedVolume => _resourceGeneratedVolume;

        public float MinCueIntervalSeconds => Mathf.Max(0f, _minCueIntervalSeconds);
    }
}
