using UnityEngine;

namespace Corebound.UI
{
    [CreateAssetMenu(menuName = "Corebound/UI/UI Theme Data", fileName = "UIThemeData")]
    public sealed class UIThemeData : ScriptableObject
    {
        [SerializeField] private Font _font;
        [SerializeField] private Color _primaryTextColor = Color.white;
        [SerializeField] private Color _secondaryTextColor = new Color(0.82f, 0.9f, 1f, 1f);
        [SerializeField] private Color _accentColor = new Color(1f, 0.82f, 0.32f, 1f);
        [SerializeField] private Sprite _panelFrameSprite;

        public Font Font => _font;

        public Color PrimaryTextColor => _primaryTextColor;

        public Color SecondaryTextColor => _secondaryTextColor;

        public Color AccentColor => _accentColor;

        public Sprite PanelFrameSprite => _panelFrameSprite;
    }
}