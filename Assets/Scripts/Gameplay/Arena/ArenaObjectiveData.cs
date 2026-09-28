using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Arena
{
    // Owner playtest 2026-09-25: "I currently don't like that it feels like there is no need to use
    // the whole arena." Two-sided waves changed where enemies come from; this is the other half -
    // something worth standing somewhere else for. Decisions recorded in
    // Docs/OPEN_TOPICS.md, owner answers of the same day.
    //
    // The payload is deliberately two independent halves (owner chose D1 "1 and 3"): a Scrap lump
    // and a timed fire-rate buff. Either may be zeroed out in data, which is also how the variants
    // in D9 (Overload Vent, Relay Uplink, Breach) would be authored later without a second runtime.
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Arena/Arena Objective Data", fileName = "ArenaObjectiveData")]
    public sealed class ArenaObjectiveData : ScriptableObject
    {
        [SerializeField] private string _objectiveId = "objective_salvage_cache";
        [SerializeField] private string _displayName = "Salvage Cache";

        // German lives in the field default, not only in the asset: a field added after the asset
        // was last saved deserializes empty and silently falls back to English (CLAUDE.md).
        [SerializeField] private string _displayNameDe = "Bergungscontainer";

        [Header("Payload — Scrap (D1 option 1)")]
        // Sized off the wave's own drop budget rather than a flat number, so it is still worth
        // crossing Stage 5's arena for and not absurd in Stage 1.
        [SerializeField] [Range(0f, 2f)] private float _scrapFractionOfWaveBudget = 0.55f;

        [Header("Payload — timed buff (D1 option 3)")]
        // A multiplier on the fire *interval*, so below 1 is faster - and the conversion is
        // reciprocal, not linear. The proposal named "+35% fire rate", which is 1/1.35 = 0.74 here;
        // the 0.65 first written in would have delivered +54%.
        [SerializeField] [Range(0.1f, 1f)] private float _fireIntervalMultiplier = 0.74f;
        [SerializeField] private float _buffDurationSeconds = 20f;

        [Header("Appearance chance (D6 option 3)")]
        // Owner chose the rising per-wave chance. Stage 1 is the floor; each later stage adds the
        // per-stage step, clamped so it never becomes a certainty - "maybe" is the whole point of
        // the option that was picked.
        [SerializeField] [Range(0f, 1f)] private float _spawnChanceStage1 = 0.35f;
        [SerializeField] [Range(0f, 0.5f)] private float _spawnChancePerStage = 0.1f;
        [SerializeField] [Range(0f, 1f)] private float _maxSpawnChance = 0.8f;

        [Header("Timing (D4 option 2 — computed lifetime)")]
        // The lifetime is crossing time at base move speed plus this slack, never a fixed number:
        // a duration that is generous in a 16x10 arena is a joke in Stage 5's.
        [SerializeField] private float _lifetimeSlackSeconds = 4f;
        [SerializeField] private float _minLifetimeSeconds = 6f;

        // Nobody walks the straight line to it: the player is mid-fight, has to disengage, and
        // steers around props and maze blockers. Straight-line time alone put Stage 1 at 1.5s and
        // collapsed every lifetime onto the minimum.
        [SerializeField] [Range(1f, 3f)] private float _travelDetourFactor = 1.6f;

        // Fraction of the wave's enemies that must have spawned before the cache appears. A clock
        // would exclude exactly the builds fast enough to go for it.
        [SerializeField] [Range(0f, 1f)] private float _spawnAtWaveProgress = 0.5f;

        [Header("Claim (D3 option 1 — walk over it)")]
        [SerializeField] private float _claimRadius = 0.75f;

        [Header("Audio")]
        // Owner 2026-09-28: the cues were created, so they are wired. The spawn cue is the
        // load-bearing one - the whole mechanic is a timed opportunity the player is meant to
        // notice while fighting somewhere else, which only works if it is heard rather than seen.
        [SerializeField] private Corebound.Audio.AudioCueData _spawnCue;
        [SerializeField] private Corebound.Audio.AudioCueData _claimedCue;
        [SerializeField] private Corebound.Audio.AudioCueData _lostCue;

        [Header("Presentation")]
        [SerializeField] private float _worldSize = 1.1f;
        [SerializeField] private Color _tint = new Color(1f, 0.78f, 0.35f, 1f);

        // The last seconds read differently, because that is when the decision to abandon a
        // position for it actually happens.
        [SerializeField] private float _urgentSeconds = 2f;

        public string ObjectiveId => _objectiveId;

        public Corebound.Audio.AudioCueData SpawnCue => _spawnCue;

        public Corebound.Audio.AudioCueData ClaimedCue => _claimedCue;

        public Corebound.Audio.AudioCueData LostCue => _lostCue;

        public string DisplayName => LocalizationService.Resolve(_displayName, _displayNameDe);

        public float ScrapFractionOfWaveBudget => Mathf.Max(0f, _scrapFractionOfWaveBudget);

        public float FireIntervalMultiplier => Mathf.Clamp(_fireIntervalMultiplier, 0.1f, 1f);

        public float BuffDurationSeconds => Mathf.Max(0f, _buffDurationSeconds);

        public float MaxSpawnChance => Mathf.Clamp01(_maxSpawnChance);

        public float LifetimeSlackSeconds => Mathf.Max(0f, _lifetimeSlackSeconds);

        public float MinLifetimeSeconds => Mathf.Max(1f, _minLifetimeSeconds);

        public float TravelDetourFactor => Mathf.Clamp(_travelDetourFactor, 1f, 3f);

        public float SpawnAtWaveProgress => Mathf.Clamp01(_spawnAtWaveProgress);

        public float ClaimRadius => Mathf.Max(0.2f, _claimRadius);

        public float WorldSize => Mathf.Max(0.2f, _worldSize);

        public Color Tint => _tint;

        public float UrgentSeconds => Mathf.Max(0f, _urgentSeconds);

        public float ResolveSpawnChance(int stageIndex)
        {
            var steps = Mathf.Max(0, stageIndex - 1);
            return Mathf.Clamp(_spawnChanceStage1 + steps * _spawnChancePerStage, 0f, MaxSpawnChance);
        }
    }
}
