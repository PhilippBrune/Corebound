using Corebound.Audio;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    public sealed class MainMenuAudioCuePlayer : MonoBehaviour
    {
        [SerializeField] private MainMenuInputHandler _inputHandler;
        [SerializeField] private MainMenuSkinData _skinData;
        [SerializeField] private AudioManager _audioManager;

        private AudioSource _audioSource;
        private AudioClip _openClip;
        private AudioClip _navigateClip;
        private AudioClip _confirmClip;
        private AudioClip _cancelClip;

        private MainMenuAudioCueData AudioCueData => _skinData != null ? _skinData.AudioCueData : null;

        private void Awake()
        {
            EnsureAudioSource();
            RebuildClips();
        }

        private void OnEnable()
        {
            if (_inputHandler != null)
            {
                _inputHandler.NavigateSubmitted += OnNavigateSubmitted;
                _inputHandler.ConfirmSubmitted += OnConfirmSubmitted;
                _inputHandler.CancelSubmitted += OnCancelSubmitted;
            }

            // Playtest feedback (2026-09-17): "After a run the main menu plays two soundfiles" -
            // this used to fire _openClip the instant MainMenu.unity loads, landing in the same
            // frame as MusicStateController.OnEnable()'s SwitchToState(MusicState.Calm) (also
            // scene-load-triggered) and audibly overlapping it. _openClip is a UI "panel appeared"
            // cue; the whole scene loading isn't a navigation action the way opening Settings or a
            // route panel is, so it no longer plays here - Navigate/Confirm/Cancel cues (genuine
            // player-initiated actions) are unaffected.
        }

        private void OnDisable()
        {
            if (_inputHandler == null)
            {
                return;
            }

            _inputHandler.NavigateSubmitted -= OnNavigateSubmitted;
            _inputHandler.ConfirmSubmitted -= OnConfirmSubmitted;
            _inputHandler.CancelSubmitted -= OnCancelSubmitted;
        }

        private void OnDestroy()
        {
            var cueData = AudioCueData;

            if (cueData == null || !cueData.HasRealOpenClip)
            {
                ProceduralToneFactory.DestroyClip(_openClip);
            }

            if (cueData == null || !cueData.HasRealNavigateClip)
            {
                ProceduralToneFactory.DestroyClip(_navigateClip);
            }

            if (cueData == null || !cueData.HasRealConfirmClip)
            {
                ProceduralToneFactory.DestroyClip(_confirmClip);
            }

            if (cueData == null || !cueData.HasRealCancelClip)
            {
                ProceduralToneFactory.DestroyClip(_cancelClip);
            }
        }

        private void OnNavigateSubmitted()
        {
            PlayCue(_navigateClip, AudioCueData != null ? AudioCueData.NavigateVolume : 0f);
        }

        private void OnConfirmSubmitted()
        {
            PlayCue(_confirmClip, AudioCueData != null ? AudioCueData.ConfirmVolume : 0f);
        }

        private void OnCancelSubmitted()
        {
            PlayCue(_cancelClip, AudioCueData != null ? AudioCueData.CancelVolume : 0f);
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
            var cueData = AudioCueData;

            if (cueData == null)
            {
                return;
            }

            _openClip = cueData.HasRealOpenClip ? cueData.OpenClip : ProceduralToneFactory.BuildToneClip("MainMenuOpenCue", cueData.OpenFrequency, cueData.OpenDurationSeconds);
            _navigateClip = cueData.HasRealNavigateClip ? cueData.NavigateClip : ProceduralToneFactory.BuildToneClip("MainMenuNavigateCue", cueData.NavigateFrequency, cueData.NavigateDurationSeconds);
            _confirmClip = cueData.HasRealConfirmClip ? cueData.ConfirmClip : ProceduralToneFactory.BuildToneClip("MainMenuConfirmCue", cueData.ConfirmFrequency, cueData.ConfirmDurationSeconds);
            _cancelClip = cueData.HasRealCancelClip ? cueData.CancelClip : ProceduralToneFactory.BuildToneClip("MainMenuCancelCue", cueData.CancelFrequency, cueData.CancelDurationSeconds);
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
    }
}
