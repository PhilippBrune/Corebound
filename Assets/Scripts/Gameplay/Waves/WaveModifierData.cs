using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Waves/Wave Modifier Data", fileName = "WaveModifierData")]
    public sealed class WaveModifierData : ScriptableObject
    {
        [SerializeField] private string _encounterLabelOverride = string.Empty;
        [SerializeField] [Range(0.25f, 2f)] private float _spawnIntervalMultiplier = 1f;
        [SerializeField] [Range(0.25f, 2f)] private float _initialDelayMultiplier = 1f;
        [SerializeField] private bool _isEliteEncounter;

        public string EncounterLabelOverride => _encounterLabelOverride;

        public float SpawnIntervalMultiplier => Mathf.Max(0.25f, _spawnIntervalMultiplier);

        public float InitialDelayMultiplier => Mathf.Max(0.25f, _initialDelayMultiplier);

        public bool IsEliteEncounter => _isEliteEncounter;
    }
}