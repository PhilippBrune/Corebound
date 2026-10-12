using Corebound.Audio;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // Core.md's documented Data Model: "CoreData: Basis-Health, Praesentationsreferenzen und
    // Kernregeln". Health/presentation are already fully covered by CombatTargetData (used
    // identically for enemies, bosses and the Core), so this wraps a reference to that asset
    // instead of duplicating its fields, and adds the one Core-specific rule the spec's Technical
    // Design section actually needs: the health fraction at which OnCoreCritical fires.
    [CreateAssetMenu(fileName = "CoreData", menuName = "Corebound/Combat/Core Data")]
    public sealed class CoreData : ScriptableObject
    {
        [SerializeField] private string _displayName = "Core";
        [SerializeField] private CombatTargetData _combatTargetData;
        [SerializeField, Range(0f, 1f)] private float _criticalHealthThreshold = 0.35f;

        // Docs/Specs/DIY/CORE_PROTECTION_FEEL_2026-10-07.md, owner decisions P1-1, P2-1, P3-1, P4-1:
        // the player should defend a thing, not a health bar - see the threat coming, feel the hit,
        // act on it, be rewarded for keeping it whole. Every number of that layer lives here.
        [Header("See the threat coming (P1-1)")]
        [Tooltip("A1: an enemy that turns toward the Core draws a line to it for this long...")]
        [SerializeField] private float _threatLineSeconds = 1f;
        [Tooltip("...and again while it is this close to the Core.")]
        [SerializeField] private float _threatLineNearDistance = 4f;
        [Tooltip("A2: the off-screen marker shows while an enemy is this close to an off-screen Core.")]
        [SerializeField] private float _edgeAlertRadius = 3f;
        [Tooltip("A3: below this the Core's glow turns amber and starts to flicker.")]
        [SerializeField, Range(0f, 1f)] private float _dangerAmberThreshold = 0.6f;
        [Tooltip("A3: below this it turns red and flickers hard.")]
        [SerializeField, Range(0f, 1f)] private float _dangerRedThreshold = 0.3f;

        [Header("Feel the hit (P2-1)")]
        [Tooltip("B4: played on every Core hit, at most once per interval so a swarm reads as an alarm.")]
        [SerializeField] private AudioCueData _hitCue;
        [SerializeField] private float _hitCueMinInterval = 0.4f;
        [Tooltip("B6: the Core speaks once per stage when it falls below each of these.")]
        [SerializeField, Range(0f, 1f)] private float _hurtLineFirstThreshold = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _hurtLineSecondThreshold = 0.25f;

        [Header("Act on it (P3-1)")]
        [Tooltip("C7: hits a raised Guard blocks before it releases a counter-pulse.")]
        [SerializeField] private int _guardPulseHits = 5;
        [SerializeField] private float _guardPulseRadius = 4f;
        [SerializeField] private float _guardPulseDamage = 2f;
        [SerializeField] private float _guardPulseImpulse = 6f;
        [Tooltip("C9: an enemy killed this close to the Core while it was going for it is an intercept.")]
        [SerializeField] private float _interceptRadius = 5f;
        [Tooltip("How recently it must have been heading for the Core: a shot enemy turns on the player.")]
        [SerializeField] private float _interceptRecentSeconds = 3f;
        [SerializeField] private int _interceptScrap = 1;

        [Header("Be rewarded for keeping it whole (P4-1)")]
        [Tooltip("D10: Scrap for a wave cleared without a Core hit.")]
        [SerializeField] private int _untouchedWaveScrap = 3;
        [Tooltip("From this many untouched waves in a row, the bonus is the streak amount instead.")]
        [SerializeField] private int _untouchedStreakLength = 3;
        [SerializeField] private int _untouchedStreakScrap = 6;

        private static CoreData _fallback;

        // For a scene without a CoreData asset: the same numbers as the C# defaults above.
        public static CoreData Fallback => _fallback != null ? _fallback : (_fallback = CreateInstance<CoreData>());

        public string DisplayName => _displayName;

        public CombatTargetData CombatTargetData => _combatTargetData;

        public float CriticalHealthThreshold => _criticalHealthThreshold;

        public float ThreatLineSeconds => Mathf.Max(0f, _threatLineSeconds);

        public float ThreatLineNearDistance => Mathf.Max(0f, _threatLineNearDistance);

        public float EdgeAlertRadius => Mathf.Max(0f, _edgeAlertRadius);

        public float DangerAmberThreshold => _dangerAmberThreshold;

        public float DangerRedThreshold => Mathf.Min(_dangerRedThreshold, _dangerAmberThreshold);

        public AudioCueData HitCue => _hitCue;

        public float HitCueMinInterval => Mathf.Max(0.05f, _hitCueMinInterval);

        public float HurtLineFirstThreshold => _hurtLineFirstThreshold;

        public float HurtLineSecondThreshold => Mathf.Min(_hurtLineSecondThreshold, _hurtLineFirstThreshold);

        public int GuardPulseHits => Mathf.Max(1, _guardPulseHits);

        public float GuardPulseRadius => Mathf.Max(0.5f, _guardPulseRadius);

        public float GuardPulseDamage => Mathf.Max(0f, _guardPulseDamage);

        public float GuardPulseImpulse => Mathf.Max(0f, _guardPulseImpulse);

        public float InterceptRadius => Mathf.Max(0f, _interceptRadius);

        public float InterceptRecentSeconds => Mathf.Max(0f, _interceptRecentSeconds);

        public int InterceptScrap => Mathf.Max(0, _interceptScrap);

        public int UntouchedWaveScrap => Mathf.Max(0, _untouchedWaveScrap);

        public int UntouchedStreakLength => Mathf.Max(1, _untouchedStreakLength);

        public int UntouchedStreakScrap => Mathf.Max(0, _untouchedStreakScrap);
    }
}
