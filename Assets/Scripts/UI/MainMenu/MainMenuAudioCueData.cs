using UnityEngine;

namespace Corebound.UI.MainMenu
{
    [CreateAssetMenu(menuName = "Corebound/Main Menu/Main Menu Audio Cue Data", fileName = "MainMenuAudioCueData")]
    public sealed class MainMenuAudioCueData : ScriptableObject
    {
        [SerializeField] private AudioClip _openClip;
        [SerializeField] private float _openFrequency = 392f;
        [SerializeField] private float _openDurationSeconds = 0.14f;
        [SerializeField] private float _openVolume = 0.1f;
        [SerializeField] private AudioClip _navigateClip;
        [SerializeField] private float _navigateFrequency = 523.25f;
        [SerializeField] private float _navigateDurationSeconds = 0.07f;
        [SerializeField] private float _navigateVolume = 0.075f;
        [SerializeField] private AudioClip _confirmClip;
        [SerializeField] private float _confirmFrequency = 659.25f;
        [SerializeField] private float _confirmDurationSeconds = 0.12f;
        [SerializeField] private float _confirmVolume = 0.11f;
        [SerializeField] private AudioClip _cancelClip;
        [SerializeField] private float _cancelFrequency = 311.13f;
        [SerializeField] private float _cancelDurationSeconds = 0.1f;
        [SerializeField] private float _cancelVolume = 0.09f;

        public AudioClip OpenClip => _openClip;

        public bool HasRealOpenClip => _openClip != null;

        public float OpenFrequency => _openFrequency;

        public float OpenDurationSeconds => _openDurationSeconds;

        public float OpenVolume => _openVolume;

        public AudioClip NavigateClip => _navigateClip;

        public bool HasRealNavigateClip => _navigateClip != null;

        public float NavigateFrequency => _navigateFrequency;

        public float NavigateDurationSeconds => _navigateDurationSeconds;

        public float NavigateVolume => _navigateVolume;

        public AudioClip ConfirmClip => _confirmClip;

        public bool HasRealConfirmClip => _confirmClip != null;

        public float ConfirmFrequency => _confirmFrequency;

        public float ConfirmDurationSeconds => _confirmDurationSeconds;

        public float ConfirmVolume => _confirmVolume;

        public AudioClip CancelClip => _cancelClip;

        public bool HasRealCancelClip => _cancelClip != null;

        public float CancelFrequency => _cancelFrequency;

        public float CancelDurationSeconds => _cancelDurationSeconds;

        public float CancelVolume => _cancelVolume;
    }
}
