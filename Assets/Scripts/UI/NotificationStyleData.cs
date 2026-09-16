using UnityEngine;

namespace Corebound.UI
{
    [CreateAssetMenu(menuName = "Corebound/UI/Notification Style Data", fileName = "NotificationStyleData")]
    public sealed class NotificationStyleData : ScriptableObject
    {
        [SerializeField] private Color _backgroundColor = new Color(0.04f, 0.08f, 0.12f, 0.9f);
        [SerializeField] private Color _foregroundColor = Color.white;
        [SerializeField] private float _displayDurationSeconds = 2.25f;
        [SerializeField] private float _fadeInSeconds = 0.12f;
        [SerializeField] private float _fadeOutSeconds = 0.16f;
        [SerializeField] private string _saveFailedText = "Could not save your progress. Check available disk space and try again.";
        [SerializeField] private string _saveFailedTextDe = "Fortschritt konnte nicht gespeichert werden. Bitte freien Speicherplatz prüfen und erneut versuchen.";
        [SerializeField] private string _loadFailedText = "A save file could not be read and was skipped.";
        [SerializeField] private string _loadFailedTextDe = "Eine Speicherdatei konnte nicht gelesen werden und wurde übersprungen.";

        public Color BackgroundColor => _backgroundColor;

        public Color ForegroundColor => _foregroundColor;

        public float DisplayDurationSeconds => _displayDurationSeconds;

        public float FadeInSeconds => _fadeInSeconds;

        public float FadeOutSeconds => _fadeOutSeconds;

        public string SaveFailedText => LocalizationService.Resolve(_saveFailedText, _saveFailedTextDe);

        public string LoadFailedText => LocalizationService.Resolve(_loadFailedText, _loadFailedTextDe);
    }
}