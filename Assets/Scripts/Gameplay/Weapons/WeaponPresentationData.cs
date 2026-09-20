using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    // Weapons.md's fifth data asset: "WeaponPresentationData: Audio, Animation, Sprite, Muzzle-
    // oder Impact-Referenzen", and the asset the Audio section's "Jede Weapon besitzt eindeutige
    // Feuer-, Treffer- und kritische Feedback-Cues" hangs off. Frequencies rather than clips are
    // the primary authoring path because every other cue asset in this project ships procedural
    // tones (ProceduralToneFactory) until real audio exists - a real clip, when assigned, wins.
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Weapons/Weapon Presentation Data", fileName = "WeaponPresentationData")]
    public sealed class WeaponPresentationData : ScriptableObject
    {
        [SerializeField] private string _presentationId = "weapon_presentation_default";

        [Header("Identity")]
        [SerializeField] private Sprite _iconSprite;
        [SerializeField] private string _iconSpritePath = string.Empty;
        [SerializeField] private Color _accentColor = new Color(1f, 0.72f, 0.35f, 1f);

        [Header("Muzzle")]
        [SerializeField] private Color _muzzleColor = new Color(1f, 0.85f, 0.5f, 0.9f);
        [SerializeField] private float _muzzleRadius = 0.28f;
        [SerializeField] private float _muzzleDurationSeconds = 0.06f;

        [Header("Impact")]
        [SerializeField] private Color _impactColor = new Color(1f, 0.55f, 0.2f, 0.9f);

        [Header("Fire cue")]
        [SerializeField] private AudioClip _fireClip;
        [SerializeField] private float _fireFrequency = 392f;
        [SerializeField] private float _fireDurationSeconds = 0.05f;
        [SerializeField] private float _fireVolume = 0.07f;

        [Header("Critical cue")]
        [SerializeField] private AudioClip _criticalClip;
        [SerializeField] private float _criticalFrequency = 880f;
        [SerializeField] private float _criticalDurationSeconds = 0.09f;
        [SerializeField] private float _criticalVolume = 0.11f;

        // A Hold-to-Fire weapon at a 0.12s interval would otherwise stack eight one-shots a second
        // on top of every tower firing at once, which Audio's "akustisch unterscheidbar bleiben"
        // rules out. The fire cue is rate-limited; the rarer critical cue is not.
        [SerializeField] private float _minFireCueIntervalSeconds = 0.09f;

        public string PresentationId => _presentationId;

        public Color AccentColor => _accentColor;

        public Color MuzzleColor => _muzzleColor;

        public float MuzzleRadius => Mathf.Max(0.01f, _muzzleRadius);

        public float MuzzleDurationSeconds => Mathf.Max(0.01f, _muzzleDurationSeconds);

        public Color ImpactColor => _impactColor;

        public AudioClip FireClip => _fireClip;

        public bool HasRealFireClip => _fireClip != null;

        public float FireFrequency => _fireFrequency;

        public float FireDurationSeconds => Mathf.Max(0.02f, _fireDurationSeconds);

        public float FireVolume => Mathf.Max(0f, _fireVolume);

        public AudioClip CriticalClip => _criticalClip;

        public bool HasRealCriticalClip => _criticalClip != null;

        public float CriticalFrequency => _criticalFrequency;

        public float CriticalDurationSeconds => Mathf.Max(0.02f, _criticalDurationSeconds);

        public float CriticalVolume => Mathf.Max(0f, _criticalVolume);

        public float MinFireCueIntervalSeconds => Mathf.Max(0f, _minFireCueIntervalSeconds);

        // Mirrors ProjectileData.ResolveSprite: an explicit Sprite reference wins, otherwise the
        // path is loaded through the same StreamingAssets-first factory the rest of the art uses.
        public Sprite ResolveIconSprite()
        {
            if (_iconSprite != null)
            {
                return _iconSprite;
            }

            if (string.IsNullOrWhiteSpace(_iconSpritePath))
            {
                return null;
            }

            return Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(_iconSpritePath);
        }
    }
}
