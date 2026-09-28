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

        // Playtest 2026-09-22 (fullscreen, 2550 x 1246): with Continue available the seven entries
        // ran past the status bar and Quit was cut off. At match 0.5 a wide screen gives the canvas
        // about 1006 units of height, leaving ~460 for the list, which needs 604 at full size.
        // Entries now shrink to fit (down to this height), recomputed when the screen changes.
        private const float MinEntryHeight = 46f;
        // Top-right corner, level with the logo - it sat far too low before (playtest 2026-09-23).
        private const float ProfileBadgeTop = 60f;
        private const float ProfileBadgeWidth = 360f;
        private const float ProfileBadgeHeight = 76f;
        private const float SecondaryRowBottom = 118f;
        private const float SecondaryRowHeight = 96f;
        private const float SecondaryRowSpacing = 12f;
        private const float SecondaryEntryHeight = 44f;
        private const float SecondaryMinWidth = 150f;
        private const float MinEntrySpacing = 6f;
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
        private GridLayoutGroup _secondaryLayout;
        private Text _profileText;
        private RectTransform _secondaryRect;
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

            // B4: the global routes live in their own row along the bottom. A grid rather than a
            // horizontal group, because it wraps to a second line when the entries get too narrow.
            var secondaryContainer = new GameObject("SecondaryRow", typeof(RectTransform), typeof(GridLayoutGroup));
            secondaryContainer.transform.SetParent(_rootRect, false);
            _secondaryRect = secondaryContainer.GetComponent<RectTransform>();
            StretchToParent(_secondaryRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(HeaderLeft, SecondaryRowBottom), new Vector2(-HeaderLeft, SecondaryRowBottom + SecondaryRowHeight));
            _secondaryLayout = secondaryContainer.GetComponent<GridLayoutGroup>();
            _secondaryLayout.spacing = new Vector2(SecondaryRowSpacing, SecondaryRowSpacing);
            _secondaryLayout.childAlignment = TextAnchor.MiddleLeft;
            _secondaryLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _secondaryLayout.constraintCount = 4;

            var statusPanel = CreateImage("StatusPanel", _rootRect, _buttonFrameSprite, ResolveIdleFrameTint(), ResolvePanelImageType(_buttonFrameSprite));
            SetAnchoredBox(statusPanel.rectTransform, new Vector2(0f, 0f), new Vector2(FooterLeft, FooterBottom), new Vector2(StatusWidth, StatusHeight));

            _statusText = CreateText("Status", statusPanel.rectTransform, StatusFontSize, TextAnchor.MiddleLeft, ResolveStatusTextColor());
            StretchToParent(_statusText.rectTransform, Vector2.zero, Vector2.one, new Vector2(56f, 18f), new Vector2(-56f, -18f));

            // Which profile you are about to play on, always visible (playtest 2026-09-22).
            var profilePanel = CreateImage("ProfilePanel", _rootRect, _buttonFrameSprite, ResolveIdleFrameTint(), ResolvePanelImageType(_buttonFrameSprite));
            SetAnchoredBox(profilePanel.rectTransform, new Vector2(1f, 1f), new Vector2(-HeaderLeft - ProfileBadgeWidth, -ProfileBadgeTop), new Vector2(ProfileBadgeWidth, ProfileBadgeHeight));
            _profileText = CreateText("ProfileLabel", profilePanel.rectTransform, StatusFontSize, TextAnchor.MiddleCenter, ResolvePromptTextColor());
            StretchToParent(_profileText.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 16f), new Vector2(-40f, -16f));

            var glyphHintObject = new GameObject("GlyphHints", typeof(RectTransform), typeof(MainMenuGlyphHintBar));
            glyphHintObject.transform.SetParent(_rootRect, false);
            var glyphHintRect = glyphHintObject.GetComponent<RectTransform>();
            SetAnchoredBox(glyphHintRect, new Vector2(1f, 0f), new Vector2(-GlyphHintRight, GlyphHintBottom), new Vector2(GlyphHintWidth, GlyphHintHeight));
            _glyphHintBar = glyphHintObject.GetComponent<MainMenuGlyphHintBar>();
            _glyphHintBar.Initialize(_font, _buttonFrameSprite, _skinData);
        }

        private Vector2Int _sizedForScreen;
        private int _sizedForEntries = -1;

        private void LateUpdate()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);

            if (_menuLayout == null || (screen == _sizedForScreen && _entryViews.Count == _sizedForEntries))
            {
                return;
            }

            _sizedForScreen = screen;
            _sizedForEntries = _entryViews.Count;
            ApplyEntrySizing();
        }

        private void ApplyEntrySizing()
        {
            ApplySecondaryRowSizing();
            var menuRect = _menuLayout.transform as RectTransform;
            var count = _menuLayout.transform.childCount;

            if (menuRect == null || count == 0)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            var available = menuRect.rect.height;
            var spacing = Mathf.Clamp((available - count * EntryHeight) / Mathf.Max(1, count - 1), MinEntrySpacing, EntrySpacing);
            var height = Mathf.Clamp((available - spacing * (count - 1)) / count, MinEntryHeight, EntryHeight);
            _menuLayout.spacing = spacing;

            for (var index = 0; index < count; index++)
            {
                var element = _menuLayout.transform.GetChild(index).GetComponent<LayoutElement>();

                if (element != null)
                {
                    element.preferredHeight = height;
                    element.minHeight = height;
                }
            }
        }

        // Equal-width cells; two rows once a cell would fall below SecondaryMinWidth.
        private void ApplySecondaryRowSizing()
        {
            if (_secondaryLayout == null || _secondaryRect == null)
            {
                return;
            }

            var count = _secondaryRect.childCount;

            if (count == 0)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            var width = _secondaryRect.rect.width;
            var columns = count;
            var cell = (width - SecondaryRowSpacing * (columns - 1)) / columns;

            if (cell < SecondaryMinWidth && count > 1)
            {
                columns = Mathf.CeilToInt(count / 2f);
                cell = (width - SecondaryRowSpacing * (columns - 1)) / columns;
            }

            _secondaryLayout.constraintCount = Mathf.Max(1, columns);
            var rows = Mathf.CeilToInt(count / (float)_secondaryLayout.constraintCount);
            var cellHeight = rows > 1 ? (SecondaryRowHeight - SecondaryRowSpacing) / 2f : SecondaryRowHeight;
            _secondaryLayout.cellSize = new Vector2(Mathf.Max(80f, cell), Mathf.Max(28f, cellHeight));
        }

        private void RebuildEntries()
        {
            _entryViews.Clear();

            if (_menuLayout == null)
            {
                return;
            }

            DestroyEntryChildren(_menuLayout.transform);
            DestroyEntryChildren(_secondaryRect);
            BuildEntryViews();
        }

        private void DestroyEntryChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);

                if (Application.isPlaying)
                {
                    // Detached first: Destroy is deferred, and a doomed entry still in the layout
                    // would be counted and sized by ApplyEntrySizing for the rest of the frame.
                    child.gameObject.SetActive(false);
                    child.SetParent(null, false);
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void BuildEntryViews()
        {
            var primaryCount = _mainMenuController.PrimaryEntryCount;

            foreach (var entry in _mainMenuController.VisibleEntries)
            {
                var entryIndex = _entryViews.Count;
                var isSecondary = entryIndex >= primaryCount;
                var entryObject = new GameObject(entry != null ? entry.Label : "Entry", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(MainMenuEntryView));
                var layoutElement = entryObject.GetComponent<LayoutElement>();
                layoutElement.preferredHeight = isSecondary ? SecondaryEntryHeight : EntryHeight;
                layoutElement.minHeight = isSecondary ? SecondaryEntryHeight : MinEntryHeight;

                var entryView = entryObject.GetComponent<MainMenuEntryView>();
                var parent = isSecondary && _secondaryRect != null ? _secondaryRect : _menuLayout.transform as RectTransform;
                entryView.Initialize(parent, _font, _buttonFrameSprite, _focusMarkerSprite, _skinData, entryIndex, OnEntryPointerEnter, OnEntryPointerClick);
                _entryViews.Add(entryView);
            }
        }

        private bool EntryViewsMatch()
        {
            var entries = _mainMenuController.VisibleEntries;

            for (var index = 0; index < _entryViews.Count && index < entries.Count; index++)
            {
                if (_entryViews[index] == null || _entryViews[index].AppliedEntry != entries[index])
                {
                    return false;
                }
            }

            return true;
        }

        private void Refresh()
        {
            if (_mainMenuController == null)
            {
                return;
            }

            // Count alone is not enough. A profile swap can trade one entry for another - losing
            // Continue while gaining Tutorial, say - which leaves the count identical and the rows
            // showing the previous profile's menu. Comparing the entries themselves costs a handful
            // of reference checks on a list that is never longer than seven.
            if (_entryViews.Count != _mainMenuController.VisibleEntries.Count || !EntryViewsMatch())
            {
                RebuildEntries();
            }

            var introVisible = _mainMenuController.IsIntroVisible;
            _logoImage.enabled = _logoSprite != null && (!introVisible || (_introFlowData != null && _introFlowData.ShowLogoOnIntro));
            _promptText.text = introVisible
                ? (_introFlowData != null ? _introFlowData.PromptText : LocalizationService.Resolve("Press Start", "Starten druecken"))
                : LocalizationService.Resolve("Choose the next route for your Corebound run.", "Waehle die naechste Route fuer deinen Corebound-Run.");

            for (var index = 0; index < _entryViews.Count; index++)
            {
                var isSelected = index == _mainMenuController.SelectedIndex;
                _entryViews[index].Apply(_mainMenuController.VisibleEntries[index], isSelected, introVisible, _skinData);
            }

            _statusText.text = ResolveStatusText(introVisible);

            if (_profileText != null)
            {
                var index = Corebound.SaveSystem.ProfileSelection.ActiveIndex;
                var summary = Corebound.SaveSystem.ProfileSelection.Summarize(index);
                _profileText.text = LocalizationService.Resolve("Profile ", "Profil ") + index
                    + (summary.IsUsed
                        ? "  ·  " + LocalizationService.Resolve("Ascension ", "Aufstieg ") + summary.AscensionRank
                        : "  ·  " + LocalizationService.Resolve("empty", "leer"));
            }

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
                return LocalizationService.Resolve("Confirm or Start opens the command routes.", "Bestaetigen oder Start oeffnet die Kommando-Routen.");
            }

            if (_mainMenuController.VisibleEntries.Count == 0)
            {
                return _continueStateData != null ? _continueStateData.EmptyStateLabel : LocalizationService.Resolve("No Active Run", "Kein aktiver Run");
            }

            var selectedEntry = _mainMenuController.SelectedEntry;

            if (selectedEntry == null)
            {
                return LocalizationService.Resolve("Select a route.", "Waehle eine Route.");
            }

            switch (selectedEntry.Route)
            {
                case MainMenuRoute.Continue:
                    return (_continueStateData != null ? _continueStateData.ContinueLabel : selectedEntry.Label) + LocalizationService.Resolve(" resumes the current run.", " setzt den aktuellen Run fort.");
                case MainMenuRoute.NewRun:
                    return LocalizationService.Resolve("Start a fresh run from the first active route.", "Startet einen neuen Run ab der ersten aktiven Route.");
                case MainMenuRoute.Tutorial:
                    return LocalizationService.Resolve("Review the guided tutorial stage.", "Zeigt die gefuehrte Tutorial-Stage.");
                case MainMenuRoute.Settings:
                    return LocalizationService.Resolve("Adjust controls, audio and presentation.", "Steuerung, Audio und Darstellung anpassen.");
                case MainMenuRoute.MetaProgression:
                    return LocalizationService.Resolve("Inspect permanent upgrades and unlocks.", "Dauerhafte Upgrades und Freischaltungen ansehen.");
                case MainMenuRoute.Quit:
                    return LocalizationService.Resolve("Close the current session.", "Beendet die aktuelle Sitzung.");
                default:
                    return selectedEntry.Label;
            }
        }

        private static string ResolveRouteConfirmedText(MainMenuRoute route)
        {
            switch (route)
            {
                case MainMenuRoute.Continue:
                    return LocalizationService.Resolve("Continuing the current run.", "Setzt den aktuellen Run fort.");
                case MainMenuRoute.NewRun:
                    return LocalizationService.Resolve("Preparing a new run.", "Neuer Run wird vorbereitet.");
                case MainMenuRoute.Tutorial:
                    return LocalizationService.Resolve("Opening the tutorial route.", "Tutorial-Route wird geoeffnet.");
                case MainMenuRoute.Settings:
                    return LocalizationService.Resolve("Opening settings.", "Einstellungen werden geoeffnet.");
                case MainMenuRoute.MetaProgression:
                    return LocalizationService.Resolve("Opening meta progression.", "Meta-Progression wird geoeffnet.");
                case MainMenuRoute.Quit:
                    return LocalizationService.Resolve("Closing the current session.", "Sitzung wird beendet.");
                default:
                    return LocalizationService.Resolve("Route confirmed.", "Route bestaetigt.");
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