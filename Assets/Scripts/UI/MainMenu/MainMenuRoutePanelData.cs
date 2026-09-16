using Corebound.UI;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    [CreateAssetMenu(menuName = "Corebound/Main Menu/Main Menu Route Panel Data", fileName = "MainMenuRoutePanelData")]
    public sealed class MainMenuRoutePanelData : ScriptableObject
    {
        [SerializeField] private MainMenuRoute _route = MainMenuRoute.Settings;
        [SerializeField] private string _title = string.Empty;
        [SerializeField] private string _titleDe = string.Empty;
        [SerializeField] private string _subtitle = string.Empty;
        [SerializeField] private string _subtitleDe = string.Empty;
        [SerializeField] [TextArea(2, 4)] private string _summary = string.Empty;
        [SerializeField] [TextArea(2, 4)] private string _summaryDe = string.Empty;
        [SerializeField] private string[] _detailLines;
        [SerializeField] private string[] _detailLinesDe;
        [SerializeField] private string _footerHint = "Confirm or Cancel to return";
        [SerializeField] private string _footerHintDe = "Bestätigen oder Abbrechen zum Zurückkehren";

        public MainMenuRoute Route => _route;

        public string Title => LocalizationService.Resolve(_title, _titleDe);

        public string Subtitle => LocalizationService.Resolve(_subtitle, _subtitleDe);

        public string Summary => LocalizationService.Resolve(_summary, _summaryDe);

        public string[] DetailLines => LocalizationService.IsGerman && _detailLinesDe != null && _detailLinesDe.Length > 0 ? _detailLinesDe : _detailLines;

        public string FooterHint => LocalizationService.Resolve(_footerHint, _footerHintDe);
    }
}
