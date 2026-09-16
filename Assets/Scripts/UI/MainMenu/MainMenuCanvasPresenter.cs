using Corebound.Input;
using System.Collections.Generic;
using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.MainMenu
{
    public sealed class MainMenuCanvasPresenter : MonoBehaviour
    {
        private const float HeaderLeft = 92f;
        private const float HeaderTop = 42f;
        private const float LogoWidth = 620f;
        private const float LogoHeight = 220f;
        private const float PromptTop = 280f;
        private const float PromptHeight = 96f;
        private const float PromptWidth = 700f;
        private const float MenuTop = 404f;
        private const float MenuWidth = 648f;
        private const float EntryHeight = 76f;
        private const float EntrySpacing = 12f;
        private const float StatusHeight = 92f;
        private const float StatusWidth = 920f;
        private const float FooterLeft = 92f;
        private const float FooterBottom = 28f;
        private const float GlyphHintRight = 92f;
        private const float GlyphHintBottom = 28f;
        private const float GlyphHintWidth = 624f;
        private const float GlyphHintHeight = 76f;
        private const int PromptFontSize = 26;
        private const int StatusFontSize = 22;

        private readonly List<MainMenuEntryView> _entryViews = new List<MainMenuEntryView>();

        [SerializeField] private MainMenuController _mainMenuController;
        [SerializeField] private IntroFlowData _introFlowData;
        [SerializeField] private ContinueStateData _continueStateData;
        [SerializeField] private MainMenuSkinData _skinData;
        [SerializeField] private UnityEngine.Object _inputReaderSource;
        [SerializeField] private MainMenuInputHandler _inputHandler;

        private Canvas _canvas;
        private RectTransform _rootRect;
        private Image _logoImage;
        private Text _promptText;
        private Text _statusText;
        private MainMenuGlyphHintBar _glyphHintBar;
        private VerticalLayoutGroup _menuLayout;
        private Font _font;
        private Sprite _backgroundSprite;
        private Sprite _logoSprite;
        private Sprite _buttonFrameSprite;
        private Sprite[] _focusMarkerSprite;
        private UIManagedPanel _managedPanel;

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private void Awake()
        {
            if (_mainMenuController == null)
            {
                enabled = false;
                return;
            }

            _font = ResolveFont();
            CreateRuntimeSprites();
            BuildCanvas();
            RegisterWithUIManager();
            RebuildEntries();
            Refresh();
        }

        private void OnEnable()
        {
            if (_mainMenuController == null)
            {
                return;
            }

            _mainMenuController.StateChanged += Refresh;
            _mainMenuController.RouteConfirmed += OnRouteConfirmed;
            Refresh();
        }

        private void OnDisable()
        {
            if (_mainMenuController == null)
            {
                return;
            }

            _mainMenuController.StateChanged -= Refresh;
            _mainMenuController.RouteConfirmed -= OnRouteConfirmed;
        }

        private void OnDestroy()
        {
            DestroyRuntimeSprites();
            UIManager.UnregisterPanel(_managedPanel);
        }

        private void BuildCanvas()
        {
            _canvas = gameObject.GetComponent<Canvas>();

            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            if (gameObject.GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            _rootRect = gameObject.GetComponent<RectTransform>();

            if (_rootRect == null)
            {
                _rootRect = gameObject.AddComponent<RectTransform>();
            }

            _rootRect.anchorMin = Vector2.zero;
            _rootRect.anchorMax = Vector2.one;
            _rootRect.offsetMin = Vector2.zero;
            _rootRect.offsetMax = Vector2.zero;

            var background = CreateImage("Background", _rootRect, _backgroundSprite, ResolveBackgroundTint(), Image.Type.Simple);
            StretchToParent(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var shade = CreateImage("Shade", _rootRect, null, ResolveBackgroundShadeColor(), Image.Type.Simple);
            StretchToParent(shade.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _logoImage = CreateImage("Logo", _rootRect, _logoSprite, Color.white, Image.Type.Simple);
            SetAnchoredBox(_logoImage.rectTransform, new Vector2(0f, 1f), new Vector2(HeaderLeft, -HeaderTop), new Vector2(LogoWidth, LogoHeight));
            _logoImage.preserveAspect = true;

            var promptPanel = CreateImage("PromptPanel", _rootRect, _buttonFrameSprite, ResolveIdleFrameTint(), ResolvePanelImageType(_buttonFrameSprite));
            SetAnchoredBox(promptPanel.rectTransform, new Vector2(0f, 1f), new Vector2(HeaderLeft, -PromptTop), new Vector2(PromptWidth, PromptHeight));

            _promptText = CreateText("Prompt", promptPanel.rectTransform, PromptFontSize, TextAnchor.MiddleLeft, ResolvePromptTextColor());
            StretchToParent(_promptText.rectTransform, Vector2.zero, Vector2.one, new Vector2(56f, 22f), new Vector2(-48f, -22f));

            var menuContainer = new GameObject("MenuContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            menuContainer.transform.SetParent(_rootRect, false);
            var menuRect = menuContainer.GetComponent<RectTransform>();
            StretchToParent(menuRect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(HeaderLeft, FooterBottom + StatusHeight + 20f), new Vector2(HeaderLeft + MenuWidth, -MenuTop));

            _menuLayout = menuContainer.GetComponent<VerticalLayoutGroup>();
            _menuLayout.spacing = EntrySpacing;
            _menuLayout.childAlignment = TextAnchor.UpperLeft;
            _menuLayout.childControlHeight = true;
            _menuLayout.childForceExpandHeight = false;
            _menuLayout.childControlWidth = true;
            _menuLayout.childForceExpandWidth = true;

            var statusPanel = CreateImage("StatusPanel", _rootRect, _buttonFrameSprite, ResolveIdleFrameTint(), ResolvePanelImageType(_buttonFrameSprite));
            SetAnchoredBox(statusPanel.rectTransform, new Vector2(0f, 0f), new Vector2(FooterLeft, FooterBottom), new Vector2(StatusWidth, StatusHeight));

            _statusText = CreateText("Status", statusPanel.rectTransform, StatusFontSize, TextAnchor.MiddleLeft, ResolveStatusTextColor());
            StretchToParent(_statusText.rectTransform, Vector2.zero, Vector2.one, new Vector2(56f, 18f), new Vector2(-56f, -18f));

            var glyphHintObject = new GameObject("GlyphHints", typeof(RectTransform), typeof(MainMenuGlyphHintBar));
            glyphHintObject.transform.SetParent(_rootRect, false);
            var glyphHintRect = glyphHintObject.GetComponent<RectTransform>();
            SetAnchoredBox(glyphHintRect, new Vector2(1f, 0f), new Vector2(-GlyphHintRight, GlyphHintBottom), new Vector2(GlyphHintWidth, GlyphHintHeight));
            _glyphHintBar = glyphHintObject.GetComponent<MainMenuGlyphHintBar>();
            _glyphHintBar.Initialize(_font, _buttonFrameSprite, _skinData);
        }

        private void RebuildEntries()
        {
            _entryViews.Clear();

            if (_menuLayout == null)
            {
                return;
            }

            for (var index = _menuLayout.transform.childCount - 1; index >= 0; index--)
            {
                var child = _menuLayout.transform.GetChild(index);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            foreach (var entry in _mainMenuController.VisibleEntries)
            {
                var entryIndex = _entryViews.Count;
                var entryObject = new GameObject(entry != null ? entry.Label : "Entry", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(MainMenuEntryView));
                var layoutElement = entryObject.GetComponent<LayoutElement>();
                layoutElement.preferredHeight = EntryHeight;
                layoutElement.minHeight = EntryHeight;

                var entryView = entryObject.GetComponent<MainMenuEntryView>();
                entryView.Initialize(_menuLayout.transform as RectTransform, _font, _buttonFrameSprite, _focusMarkerSprite, _skinData, entryIndex, OnEntryPointerEnter, OnEntryPointerClick);
                _entryViews.Add(entryView);
            }
        }

        private void Refresh()
        {
            if (_mainMenuController == null)
            {
                return;
            }

            if (_entryViews.Count != _mainMenuController.VisibleEntries.Count)
            {
                RebuildEntries();
            }

            var introVisible = _mainMenuController.IsIntroVisible;
            _logoImage.enabled = _logoSprite != null && (!introVisible || (_introFlowData != null && _introFlowData.ShowLogoOnIntro));
            _promptText.text = introVisible
                ? (_introFlowData != null ? _introFlowData.PromptText : "Press Start")
                : "Choose the next route for your Corebound run.";

            for (var index = 0; index < _entryViews.Count; index++)
            {
                var isSelected = index == _mainMenuController.SelectedIndex;
                _entryViews[index].Apply(_mainMenuController.VisibleEntries[index], isSelected, introVisible, _skinData);
            }

            _statusText.text = ResolveStatusText(introVisible);

            if (_glyphHintBar != null)
            {
                var isUsingGamepad = InputReader != null && InputReader.IsUsingGamepad;
                _glyphHintBar.Refresh(isUsingGamepad, introVisible, _skinData != null ? _skinData.ControllerGlyphData : null, _skinData);
            }

            UIManager.NotifyPanelChanged(_managedPanel);
        }

        private void OnEntryPointerEnter(int index)
        {
            _inputHandler?.RequestSelectIndex(index);
        }

        private void OnEntryPointerClick(int index)
        {
            _inputHandler?.RequestSelectIndex(index);
            _inputHandler?.RequestConfirm();
        }

        private void OnRouteConfirmed(MainMenuRoute route)
        {
            _statusText.text = ResolveRouteConfirmedText(route);
        }

        private Text CreateText(string name, RectTransform parent, int fontSize, TextAnchor anchor, Color color)
        {
            return ProceduralUIFactory.CreateText(name, parent, _font, fontSize, FontStyle.Normal, color, anchor);
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color, Image.Type imageType)
        {
            return ProceduralUIFactory.CreateImage(name, parent, sprite, color, imageType);
        }

        // button_primary_frame.png is a 1088x606 canvas but the actual chamfered-panel artwork
        // only occupies a 836x454 content rect inside it (measured directly: alpha bbox (126,77)-
        // (962,531)) - the rest is transparent padding. A first fix attempt applied a small border
        // (50,16,50,16) directly against the FULL padded canvas, but 16px from the top/bottom edge
        // of a 606-tall canvas whose content doesn't start until y=77 is border sitting entirely
        // inside the transparent padding - every 9-slice corner sampled pure transparency, so only
        // a thin sliver of the actual (heavily squeezed) artwork was visible anywhere, with the
        // label text (sized/centered for the full entry box) sticking out above it (playtest
        // 2026-09-14: "the Btn name is still not inside the background"). Cropping to the content
        // rect first and sizing the border against THAT (not the padded canvas) fixes it the same
        // way CardRewardPresenter/ShopPresenter's card frames were fixed earlier - Unity still
        // proportionally scales the border down to fit a 76px-tall entry (fine, it stays a sane,
        // if simplified, corner), but every corner now samples real artwork instead of empty space.
        private static readonly Rect ButtonFrameContentRect = new Rect(126f, 75f, 836f, 454f);
        private static readonly Vector4 ButtonFrameBorder = new Vector4(75f, 65f, 75f, 65f);

        private void CreateRuntimeSprites()
        {
            _backgroundSprite = CreateRuntimeSprite(_skinData != null ? _skinData.BackgroundTexture : null, Vector4.zero);
            _logoSprite = CreateRuntimeSprite(_skinData != null ? _skinData.LogoTexture : null, Vector4.zero);
            _buttonFrameSprite = CreateRuntimeSpriteRect(_skinData != null ? _skinData.ButtonFrameTexture : null, ButtonFrameContentRect, ButtonFrameBorder);
            _focusMarkerSprite = CreateFocusMarkerSprites(_skinData != null ? _skinData.FocusMarkerTexture : null);
        }

        private void DestroyRuntimeSprites()
        {
            DestroyRuntimeSprite(_backgroundSprite);
            DestroyRuntimeSprite(_logoSprite);
            DestroyRuntimeSprite(_buttonFrameSprite);

            if (_focusMarkerSprite != null)
            {
                for (var index = 0; index < _focusMarkerSprite.Length; index++)
                {
                    DestroyRuntimeSprite(_focusMarkerSprite[index]);
                }
            }
        }

        // focus_marker.png is not a single 9-sliceable frame - it's a sheet of four disconnected
        // corner-bracket glyphs (confirmed by cropping it: each bracket is a clean ~234x227 blob
        // with huge transparent gaps between them, not a continuous frame). Stretching the whole
        // sheet across an entry via Image.Type.Sliced (the old approach) smeared those four
        // disconnected brackets into a single distorted bar. Crop each bracket to its own sprite
        // instead, so MainMenuEntryView can place all four as small, unstretched corner accents.
        private static readonly Rect[] FocusMarkerCornerRects =
        {
            new Rect(136f, 321f, 234f, 227f), // top-left
            new Rect(719f, 321f, 234f, 227f), // top-right
            new Rect(133f, 67f, 232f, 225f),  // bottom-left
            new Rect(723f, 68f, 233f, 225f),  // bottom-right
        };

        private static Sprite[] CreateFocusMarkerSprites(Texture2D texture)
        {
            var sprites = new Sprite[FocusMarkerCornerRects.Length];

            if (texture == null)
            {
                return sprites;
            }

            for (var index = 0; index < FocusMarkerCornerRects.Length; index++)
            {
                sprites[index] = Sprite.Create(texture, FocusMarkerCornerRects[index], new Vector2(0.5f, 0.5f), 100f);
                sprites[index].hideFlags = HideFlags.DontSaveInEditor;
            }

            return sprites;
        }

        private string ResolveStatusText(bool introVisible)
        {
            if (introVisible)
            {
                return "Confirm or Start opens the command routes.";
            }

            if (_mainMenuController.VisibleEntries.Count == 0)
            {
                return _continueStateData != null ? _continueStateData.EmptyStateLabel : "No Active Run";
            }

            var selectedEntry = _mainMenuController.SelectedEntry;

            if (selectedEntry == null)
            {
                return "Select a route.";
            }

            switch (selectedEntry.Route)
            {
                case MainMenuRoute.Continue:
                    return (_continueStateData != null ? _continueStateData.ContinueLabel : selectedEntry.Label) + " resumes the current run.";
                case MainMenuRoute.NewRun:
                    return "Start a fresh run from the first active route.";
                case MainMenuRoute.Tutorial:
                    return "Review the guided tutorial stage.";
                case MainMenuRoute.Settings:
                    return "Adjust controls, audio and presentation.";
                case MainMenuRoute.MetaProgression:
                    return "Inspect permanent upgrades and unlocks.";
                case MainMenuRoute.Quit:
                    return "Close the current session.";
                default:
                    return selectedEntry.Label;
            }
        }

        private static string ResolveRouteConfirmedText(MainMenuRoute route)
        {
            switch (route)
            {
                case MainMenuRoute.Continue:
                    return "Continuing the current run.";
                case MainMenuRoute.NewRun:
                    return "Preparing a new run.";
                case MainMenuRoute.Tutorial:
                    return "Opening the tutorial route.";
                case MainMenuRoute.Settings:
                    return "Opening settings.";
                case MainMenuRoute.MetaProgression:
                    return "Opening meta progression.";
                case MainMenuRoute.Quit:
                    return "Closing the current session.";
                default:
                    return "Route confirmed.";
            }
        }

        private static Image.Type ResolvePanelImageType(Sprite sprite)
        {
            return sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        private static Sprite CreateRuntimeSprite(Texture2D texture, Vector4 border)
        {
            if (texture == null)
            {
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                border);
            sprite.hideFlags = HideFlags.DontSaveInEditor;
            return sprite;
        }

        // Same as CreateRuntimeSprite but sliced from an explicit pixelRect within the texture
        // instead of the whole thing - for textures (like button_primary_frame.png) with a large
        // transparent canvas margin around the actual content, so the 9-slice border can be
        // measured against the real artwork instead of the padded canvas.
        private static Sprite CreateRuntimeSpriteRect(Texture2D texture, Rect pixelRect, Vector4 border)
        {
            if (texture == null)
            {
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                pixelRect,
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                border);
            sprite.hideFlags = HideFlags.DontSaveInEditor;
            return sprite;
        }

        private static void DestroyRuntimeSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(sprite);
            }
            else
            {
                DestroyImmediate(sprite);
            }
        }

        private static void StretchToParent(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void SetAnchoredBox(RectTransform rectTransform, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(anchor.x, anchor.y);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        private Color ResolveBackgroundTint()
        {
            return _skinData != null ? _skinData.BackgroundTint : Color.white;
        }

        private Color ResolveBackgroundShadeColor()
        {
            return _skinData != null ? _skinData.BackgroundShadeColor : new Color(0.03f, 0.05f, 0.08f, 0.45f);
        }

        private Color ResolveIdleFrameTint()
        {
            return _skinData != null ? _skinData.IdleFrameTint : Color.white;
        }

        private Color ResolvePromptTextColor()
        {
            return _skinData != null ? _skinData.PromptLabelColor : Color.white;
        }

        private Color ResolveStatusTextColor()
        {
            return _skinData != null ? _skinData.StatusLabelColor : Color.white;
        }

        private static Font ResolveFont()
        {
            return UIFontResolver.Resolve();
        }

        private void RegisterWithUIManager()
        {
            // Background, not Menu: this is the main menu's own base screen, and Settings/Meta/
            // the route confirmation dialog all register as BlockingOverlay on top of it - at
            // Menu (which sorts above BlockingOverlay) this canvas drew over all of them, making
            // every sub-panel open correctly but render fully hidden behind the menu artwork.
            _managedPanel = UIManager.RegisterPanel(_rootRect, "main_menu", UIPanelLayer.Background, false, false);
        }
    }
}