using Corebound.Audio;
using UnityEngine;

namespace Corebound.Gameplay.Shop
{
    // The subscriber ShopEvents never had. Before this, all five shop events were raised into an
    // empty delegate, so Shop.md's Audio section was unimplemented purely for want of a listener.
    public sealed class ShopAudioCuePlayer : MonoBehaviour
    {
        [SerializeField] private ShopAudioCueData _audioCueData;
        [SerializeField] private AudioManager _audioManager;

        private AudioSource _audioSource;
        private AudioClip _openedClip;
        private AudioClip _offerSelectedClip;
        private AudioClip _purchaseSucceededClip;
        private AudioClip _purchaseDeclinedClip;
        private AudioClip _closedClip;

        private void Awake()
        {
            EnsureAudioSource();
            RebuildClips();
        }

        private void OnEnable()
        {
            ShopEvents.ShopOpened += OnShopOpened;
            ShopEvents.ShopOfferSelected += OnShopOfferSelected;
            ShopEvents.PurchaseSucceeded += OnPurchaseSucceeded;
            ShopEvents.PurchaseDeclined += OnPurchaseDeclined;
            ShopEvents.ShopClosed += OnShopClosed;
        }

        private void OnDisable()
        {
            ShopEvents.ShopOpened -= OnShopOpened;
            ShopEvents.ShopOfferSelected -= OnShopOfferSelected;
            ShopEvents.PurchaseSucceeded -= OnPurchaseSucceeded;
            ShopEvents.PurchaseDeclined -= OnPurchaseDeclined;
            ShopEvents.ShopClosed -= OnShopClosed;
        }

        private void OnDestroy()
        {
            DestroyProceduralClips();
        }

        private void OnShopOpened(ShopEventArgs eventArgs)
        {
            PlayCue(_openedClip, _audioCueData != null ? _audioCueData.OpenedVolume : 0f);
        }

        private void OnShopOfferSelected(ShopEventArgs eventArgs)
        {
            PlayCue(_offerSelectedClip, _audioCueData != null ? _audioCueData.OfferSelectedVolume : 0f);
        }

        private void OnPurchaseSucceeded(ShopEventArgs eventArgs)
        {
            PlayCue(_purchaseSucceededClip, _audioCueData != null ? _audioCueData.PurchaseSucceededVolume : 0f);
        }

        private void OnPurchaseDeclined(ShopEventArgs eventArgs)
        {
            PlayCue(_purchaseDeclinedClip, _audioCueData != null ? _audioCueData.PurchaseDeclinedVolume : 0f);
        }

        // ShopClosed fires on a successful purchase too, right after PurchaseSucceeded - two
        // one-shots in the same frame is exactly the doubled-sound problem already found on the
        // Main Menu, so the close cue is skipped when the shop closed because something was bought.
        private void OnShopClosed(ShopEventArgs eventArgs)
        {
            if (eventArgs.Purchased)
            {
                return;
            }

            PlayCue(_closedClip, _audioCueData != null ? _audioCueData.ClosedVolume : 0f);
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

        private void RebuildClips()
        {
            DestroyProceduralClips();

            if (_audioCueData == null)
            {
                return;
            }

            _openedClip = _audioCueData.OpenedClip != null
                ? _audioCueData.OpenedClip
                : ProceduralToneFactory.BuildToneClip("ShopOpenedCue", _audioCueData.OpenedFrequency, _audioCueData.OpenedDurationSeconds);

            _offerSelectedClip = _audioCueData.OfferSelectedClip != null
                ? _audioCueData.OfferSelectedClip
                : ProceduralToneFactory.BuildToneClip("ShopOfferSelectedCue", _audioCueData.OfferSelectedFrequency, _audioCueData.OfferSelectedDurationSeconds);

            _purchaseSucceededClip = _audioCueData.PurchaseSucceededClip != null
                ? _audioCueData.PurchaseSucceededClip
                : ProceduralToneFactory.BuildToneClip("ShopPurchaseSucceededCue", _audioCueData.PurchaseSucceededFrequency, _audioCueData.PurchaseSucceededDurationSeconds);

            _purchaseDeclinedClip = _audioCueData.PurchaseDeclinedClip != null
                ? _audioCueData.PurchaseDeclinedClip
                : ProceduralToneFactory.BuildToneClip("ShopPurchaseDeclinedCue", _audioCueData.PurchaseDeclinedFrequency, _audioCueData.PurchaseDeclinedDurationSeconds);

            _closedClip = _audioCueData.ClosedClip != null
                ? _audioCueData.ClosedClip
                : ProceduralToneFactory.BuildToneClip("ShopClosedCue", _audioCueData.ClosedFrequency, _audioCueData.ClosedDurationSeconds);
        }

        private void PlayCue(AudioClip clip, float volume)
        {
            if (_audioSource == null || clip == null || volume <= 0f)
            {
                return;
            }

            var scaledVolume = _audioManager != null ? volume * _audioManager.UiVolume : volume;
            _audioSource.PlayOneShot(clip, scaledVolume);
        }

        // Only tones this player generated are destroyed; an authored clip belongs to the project.
        private void DestroyProceduralClips()
        {
            if (_audioCueData == null)
            {
                return;
            }

            if (_audioCueData.OpenedClip == null)
            {
                ProceduralToneFactory.DestroyClip(_openedClip);
            }

            if (_audioCueData.OfferSelectedClip == null)
            {
                ProceduralToneFactory.DestroyClip(_offerSelectedClip);
            }

            if (_audioCueData.PurchaseSucceededClip == null)
            {
                ProceduralToneFactory.DestroyClip(_purchaseSucceededClip);
            }

            if (_audioCueData.PurchaseDeclinedClip == null)
            {
                ProceduralToneFactory.DestroyClip(_purchaseDeclinedClip);
            }

            if (_audioCueData.ClosedClip == null)
            {
                ProceduralToneFactory.DestroyClip(_closedClip);
            }
        }
    }
}
