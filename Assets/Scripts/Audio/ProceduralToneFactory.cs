using UnityEngine;

namespace Corebound.Audio
{
    // Shared home for the short synthesized-tone generation previously copy-pasted identically
    // across MainMenuAudioCuePlayer, BossAudioCuePlayer and WaveAudioCuePlayer. Procedural tones
    // remain the project's accepted stand-in for UI/event audio until real clips are authored and
    // imported (no .wav/.mp3/.ogg assets exist in the project yet); centralizing the synthesis
    // here means that swap only has to happen in one place.
    public static class ProceduralToneFactory
    {
        private const int DefaultSampleRate = 44100;

        public static AudioClip BuildToneClip(string clipName, float frequency, float durationSeconds, int sampleRate = DefaultSampleRate)
        {
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * Mathf.Max(0.02f, durationSeconds)));
            var samples = new float[sampleCount];
            var attackSamples = Mathf.Min(sampleCount / 6, 128);
            var releaseSamples = Mathf.Min(sampleCount / 5, 192);

            for (var index = 0; index < sampleCount; index++)
            {
                var time = index / (float)sampleRate;
                var envelope = 1f;

                if (index < attackSamples)
                {
                    envelope = index / (float)Mathf.Max(1, attackSamples);
                }
                else if (index > sampleCount - releaseSamples)
                {
                    envelope = (sampleCount - index) / (float)Mathf.Max(1, releaseSamples);
                }

                samples[index] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static void DestroyClip(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(clip);
            }
            else
            {
                Object.DestroyImmediate(clip);
            }
        }
    }
}
