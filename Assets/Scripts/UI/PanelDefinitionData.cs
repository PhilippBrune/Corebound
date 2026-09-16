using UnityEngine;

namespace Corebound.UI
{
    [CreateAssetMenu(menuName = "Corebound/UI/Panel Definition Data", fileName = "PanelDefinitionData")]
    public sealed class PanelDefinitionData : ScriptableObject
    {
        [SerializeField] private string _panelId = "panel";
        [SerializeField] private string _displayName = "Panel";
        [SerializeField] private UIPanelLayer _defaultLayer = UIPanelLayer.Overlay;
        [SerializeField] private bool _blocksInput;
        [SerializeField] private bool _isModal;
        [SerializeField] private bool _defaultVisible;

        public string PanelId => _panelId;

        public string DisplayName => _displayName;

        public UIPanelLayer DefaultLayer => _defaultLayer;

        public bool BlocksInput => _blocksInput;

        public bool IsModal => _isModal;

        public bool DefaultVisible => _defaultVisible;
    }
}