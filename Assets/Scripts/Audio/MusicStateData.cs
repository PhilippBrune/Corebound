using UnityEngine;

namespace Corebound.Audio
{
    // The MusicStateData Audio.md's Data Model section calls for. Real composed loops can be
    // assigned to Clip once they exist; until then, ProceduralFrequency drives a long, near-seamless
    // looping tone via ProceduralToneFactory (its attack/release envelope is sample-capped, so at
    // multi-second durations it is effectively a sustained tone with no audible loop seam) so each
    // state is already audibly distinct rather than silent.
    [CreateAssetMenu(menuName = "Corebound/Audio/Music State Data", fileName = "MusicStateData")]
    public sealed class MusicStateData : ScriptableObject
    {
        [SerializeField] private MusicState _state;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private float _proceduralFrequency = 110f;
        [SerializeField] private float _proceduralLoopSeconds = 4f;
        [SerializeField] private float _volume = 0.05f;
        [SerializeField] private float _crossfadeSeconds = 1.5f;

        public MusicState State => _state;

        public AudioClip Clip => _clip;

        public bool HasRealClip => _clip != null;

        public float ProceduralFrequency => Mathf.Max(20f, _proceduralFrequency);

        public float ProceduralLoopSeconds => Mathf.Max(1f, _proceduralLoopSeconds);

        public float Volume => Mathf.Max(0f, _volume);

        public float CrossfadeSeconds => Mathf.Max(0.1f, _crossfadeSeconds);
    }
}
