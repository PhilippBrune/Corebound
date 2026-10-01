using Corebound.Input;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    [CreateAssetMenu(menuName = "Corebound/Main Menu/Main Menu Skin Data", fileName = "MainMenuSkinData")]
    public sealed class MainMenuSkinData : ScriptableObject
    {
        [SerializeField] private Texture2D _backgroundTexture;
        [SerializeField] private Texture2D _logoTexture;
        [SerializeField] private Texture2D _buttonFrameTexture;

        // Open Topics 2.1 (2026-10-01): Assets 29 and 30. The route panels (New Run, Records,
        // Profile, Quit) and the glyph hints had borrowed the button frame since the menu was
        // built. Left empty, both fall back to it.
        [SerializeField] private Texture2D _routePanelFrameTexture;
        [SerializeField] private Vector4 _routePanelFrameBorder = new Vector4(140f, 130f, 140f, 130f);

        // The frame's steel-blue body is lighter than the panels it replaces; darkened so the light
        // label colours keep their contrast on it.
        [SerializeField] private Color _routePanelFrameTint = new Color(0.62f, 0.68f, 0.78f, 1f);
        [SerializeField] private Texture2D _glyphHintCapsuleTexture;

        // One of the three small capsules on the sheet (bottom left), bottom-left pixel origin.
        [SerializeField] private Rect _glyphHintCapsuleRect = new Rect(47f, 37f, 404f, 132f);
        [SerializeField] private Texture2D _focusMarkerTexture;
        [SerializeField] private ControllerGlyphData _controllerGlyphData;
        [SerializeField] private MainMenuAudioCueData _audioCueData;
        [SerializeField] private Vector4 _buttonFrameBorder = new Vector4(220f, 140f, 220f, 140f);
        [SerializeField] private Vector4 _focusMarkerBorder = new Vector4(240f, 132f, 240f, 132f);
        [SerializeField] private Color _backgroundTint = Color.white;
        [SerializeField] private Color _backgroundShadeColor = new Color(0.03f, 0.05f, 0.08f, 0.45f);
        [SerializeField] private Color _idleFrameTint = new Color(0.86f, 0.9f, 0.96f, 0.92f);
        [SerializeField] private Color _selectedFrameTint = Color.white;
        [SerializeField] private Color _introLockedFrameTint = new Color(0.74f, 0.79f, 0.86f, 0.76f);
        [SerializeField] private Color _focusMarkerTint = Color.white;
        [SerializeField] private Color _idleLabelColor = new Color(0.84f, 0.89f, 0.95f, 1f);
        [SerializeField] private Color _selectedLabelColor = new Color(1f, 0.87f, 0.46f, 1f);
        [SerializeField] private Color _promptLabelColor = new Color(0.99f, 0.95f, 0.88f, 1f);
        [SerializeField] private Color _statusLabelColor = new Color(0.78f, 0.84f, 0.91f, 1f);
        [SerializeField] private Color _glyphHintFrameTint = new Color(0.84f, 0.9f, 0.97f, 0.88f);
        [SerializeField] private Color _glyphHintActionColor = new Color(0.08f, 0.12f, 0.17f, 0.95f);
        [SerializeField] private Color _glyphHintKeyColor = new Color(1f, 0.87f, 0.46f, 1f);

        public Texture2D BackgroundTexture => _backgroundTexture;

        public Texture2D LogoTexture => _logoTexture;

        public Texture2D ButtonFrameTexture => _buttonFrameTexture;

        public Texture2D RoutePanelFrameTexture => _routePanelFrameTexture;

        public Vector4 RoutePanelFrameBorder => _routePanelFrameBorder;

        public Color RoutePanelFrameTint => _routePanelFrameTint;

        public Texture2D GlyphHintCapsuleTexture => _glyphHintCapsuleTexture;

        public Rect GlyphHintCapsuleRect => _glyphHintCapsuleRect;

        public Texture2D FocusMarkerTexture => _focusMarkerTexture;

        public ControllerGlyphData ControllerGlyphData => _controllerGlyphData;

        public MainMenuAudioCueData AudioCueData => _audioCueData;

        public Vector4 ButtonFrameBorder => _buttonFrameBorder;

        public Vector4 FocusMarkerBorder => _focusMarkerBorder;

        public Color BackgroundTint => _backgroundTint;

        public Color BackgroundShadeColor => _backgroundShadeColor;

        public Color IdleFrameTint => _idleFrameTint;

        public Color SelectedFrameTint => _selectedFrameTint;

        public Color IntroLockedFrameTint => _introLockedFrameTint;

        public Color FocusMarkerTint => _focusMarkerTint;

        public Color IdleLabelColor => _idleLabelColor;

        public Color SelectedLabelColor => _selectedLabelColor;

        public Color PromptLabelColor => _promptLabelColor;

        public Color StatusLabelColor => _statusLabelColor;

        public Color GlyphHintFrameTint => _glyphHintFrameTint;

        public Color GlyphHintActionColor => _glyphHintActionColor;

        public Color GlyphHintKeyColor => _glyphHintKeyColor;
    }
}