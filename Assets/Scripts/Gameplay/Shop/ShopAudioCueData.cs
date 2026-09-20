using UnityEngine;

namespace Corebound.Gameplay.Shop
{
    // Shop.md, Audio: distinct open / hover / buy / insufficient-funds cues. Wave, Boss, MainMenu
    // and Factory all had an audio cue asset; the Shop did not, which is why its five events had no
    // subscriber anywhere and were dead API.
    //
    // Frequencies rather than clips are the primary authoring path, matching every other cue asset
    // in this project; an assigned AudioClip wins when one exists.
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Shop/Shop Audio Cue Data", fileName = "ShopAudioCueData")]
    public sealed class ShopAudioCueData : ScriptableObject
    {
        [Header("Shop opened")]
        [SerializeField] private AudioClip _openedClip;
        [SerializeField] private float _openedFrequency = 349.23f;
        [SerializeField] private float _openedDurationSeconds = 0.16f;
        [SerializeField] private float _openedVolume = 0.12f;

        [Header("Offer selected")]
        [SerializeField] private AudioClip _offerSelectedClip;
        [SerializeField] private float _offerSelectedFrequency = 587.33f;
        [SerializeField] private float _offerSelectedDurationSeconds = 0.05f;
        [SerializeField] private float _offerSelectedVolume = 0.06f;

        [Header("Purchase succeeded")]
        [SerializeField] private AudioClip _purchaseSucceededClip;
        [SerializeField] private float _purchaseSucceededFrequency = 880f;
        [SerializeField] private float _purchaseSucceededDurationSeconds = 0.18f;
        [SerializeField] private float _purchaseSucceededVolume = 0.14f;

        // Deliberately the lowest, shortest and flattest of the four. An unaffordable press has to
        // read as a refusal at a glance-equivalent for the ear, not as another confirmation.
        [Header("Purchase declined")]
        [SerializeField] private AudioClip _purchaseDeclinedClip;
        [SerializeField] private float _purchaseDeclinedFrequency = 155.56f;
        [SerializeField] private float _purchaseDeclinedDurationSeconds = 0.13f;
        [SerializeField] private float _purchaseDeclinedVolume = 0.11f;

        [Header("Shop closed")]
        [SerializeField] private AudioClip _closedClip;
        [SerializeField] private float _closedFrequency = 261.63f;
        [SerializeField] private float _closedDurationSeconds = 0.12f;
        [SerializeField] private float _closedVolume = 0.09f;

        public AudioClip OpenedClip => _openedClip;

        public float OpenedFrequency => _openedFrequency;

        public float OpenedDurationSeconds => Mathf.Max(0.02f, _openedDurationSeconds);

        public float OpenedVolume => Mathf.Max(0f, _openedVolume);

        public AudioClip OfferSelectedClip => _offerSelectedClip;

        public float OfferSelectedFrequency => _offerSelectedFrequency;

        public float OfferSelectedDurationSeconds => Mathf.Max(0.02f, _offerSelectedDurationSeconds);

        public float OfferSelectedVolume => Mathf.Max(0f, _offerSelectedVolume);

        public AudioClip PurchaseSucceededClip => _purchaseSucceededClip;

        public float PurchaseSucceededFrequency => _purchaseSucceededFrequency;

        public float PurchaseSucceededDurationSeconds => Mathf.Max(0.02f, _purchaseSucceededDurationSeconds);

        public float PurchaseSucceededVolume => Mathf.Max(0f, _purchaseSucceededVolume);

        public AudioClip PurchaseDeclinedClip => _purchaseDeclinedClip;

        public float PurchaseDeclinedFrequency => _purchaseDeclinedFrequency;

        public float PurchaseDeclinedDurationSeconds => Mathf.Max(0.02f, _purchaseDeclinedDurationSeconds);

        public float PurchaseDeclinedVolume => Mathf.Max(0f, _purchaseDeclinedVolume);

        public AudioClip ClosedClip => _closedClip;

        public float ClosedFrequency => _closedFrequency;

        public float ClosedDurationSeconds => Mathf.Max(0.02f, _closedDurationSeconds);

        public float ClosedVolume => Mathf.Max(0f, _closedVolume);
    }
}
