using Corebound.UI;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    [CreateAssetMenu(menuName = "Corebound/Main Menu/Continue State Data", fileName = "ContinueStateData")]
    public sealed class ContinueStateData : ScriptableObject
    {
        [SerializeField] private string _continueLabel = "Continue";
        [SerializeField] private string _continueLabelDe = "Fortsetzen";
        [SerializeField] private string _emptyStateLabel = "No Active Run";
        [SerializeField] private string _emptyStateLabelDe = "Kein aktiver Run";
        [SerializeField] private bool _showLastRunSummary = true;

        public string ContinueLabel => LocalizationService.Resolve(_continueLabel, _continueLabelDe);

        public string EmptyStateLabel => LocalizationService.Resolve(_emptyStateLabel, _emptyStateLabelDe);

        public bool ShowLastRunSummary => _showLastRunSummary;
    }
}
