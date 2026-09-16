using UnityEngine;

namespace Corebound.Audio
{
    // The shared AudioCueData/AudioEventData model Audio.md's Data Model section calls for.
    // Real AudioClip variations can be assigned once real audio content exists; until then,
    // FrequencyVariations drives ProceduralToneFactory so a cue still has genuine per-play
    // variation (Audio.md requires >=3 variations for frequent core actions) instead of one
    // repeated tone.
    [CreateAssetMenu(menuName = "Corebound/Audio/Audio Cue Data", fileName = "AudioCueData")]
    public sealed class AudioCueData : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clipVariations;
        [SerializeField] private float[] _frequencyVariations = { 440f, 466.16f, 493.88f };
        [SerializeField] private float _durationSeconds = 0.1f;
        [SerializeField] private float _volume = 0.15f;
        [SerializeField] private Vector2 _pitchRange = new Vector2(0.97f, 1.03f);

        public AudioClip[] ClipVariations => _clipVariations;

        public float[] FrequencyVariations => _frequencyVariations;

        public float DurationSeconds => Mathf.Max(0.02f, _durationSeconds);

        public float Volume => Mathf.Max(0f, _volume);

        public Vector2 PitchRange => _pitchRange;

        public bool HasRealClips => _clipVariations != null && _clipVariations.Length > 0;

        public float ResolveRandomPitch()
        {
            return Random.Range(Mathf.Min(_pitchRange.x, _pitchRange.y), Mathf.Max(_pitchRange.x, _pitchRange.y));
        }
    }
}
