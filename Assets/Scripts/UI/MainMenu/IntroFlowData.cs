using Corebound.UI;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    [CreateAssetMenu(menuName = "Corebound/Main Menu/Intro Flow Data", fileName = "IntroFlowData")]
    public sealed class IntroFlowData : ScriptableObject
    {
        [SerializeField] private string _promptText = "Press Start";
        [SerializeField] private string _promptTextDe = "Start drücken";
        [SerializeField] private bool _allowSubmitToAdvance = true;
        [SerializeField] private bool _showLogoOnIntro = true;

        public string PromptText => LocalizationService.Resolve(_promptText, _promptTextDe);

        public bool AllowSubmitToAdvance => _allowSubmitToAdvance;

        public bool ShowLogoOnIntro => _showLogoOnIntro;
    }
}
