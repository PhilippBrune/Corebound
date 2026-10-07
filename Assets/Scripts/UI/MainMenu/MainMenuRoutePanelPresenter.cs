using System.Collections.Generic;
using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.MainMenu
{
    public sealed class MainMenuRoutePanelPresenter : MonoBehaviour
    {
        private const float PanelWidth = 1240f;
        // button_primary_frame.png has a 220px/140px (horiz/vert) 9-slice border baked into the
        // art itself; 650 left too little safe vertical space once title+subtitle+summary+detail
        // lines+footer are all accounted for, so title through footer are laid out below with
        // insets that clear that border instead of rendering across it.
        private const float PanelHeight = 900f;
        private const int TitleFontSize = 42;
        private const int SubtitleFontSize = 24;
        private const int SummaryFontSize = 24;
        private const int DetailFontSize = 22;
        private const int FooterFontSize = 20;

        private readonly List<Text> _detailTexts = new List<Text>();

        [SerializeField] private MainMenuSkinData _skinData;
        [SerializeField] private UnityEngine.Object _routeTargetControllerSource;

        private RectTransform _presenterRoot;
        private CanvasGroup _canvasGroup;
        private RectTransform _overlayRect;
        private RectTransform _panelRect;
        private Text _titleText;
        private Text _subtitleText;
        private Text _summaryText;
        private Text _footerText;
        private VerticalLayoutGroup _detailLayout;
        private Sprite _frameSprite;
        private UIManagedPanel _managedPanel;
        private Image _panelImage;
        private RectTransform _detailRect;
        private RecordsPanelView _recordsView;
        private bool _recordsLayoutApplied;

        // Records (playtest 2026-09-29) needs room for a card grid, so it takes almost the whole
        // screen and draws the frame's border thinner; every other panel keeps the sizes above.
        private static readonly Vector2 RecordsPanelSize = new Vector2(1800f, 1020f);
        private const float RecordsFrameScale = 1.6f;
        private const float RecordsSideInset = 165f;

        private IMainMenuRouteTargetController RouteTargetController => _routeTargetControllerSource as IMainMenuRouteTargetController;

        private void Awake()
        {
            // The route panel frame (Asset 29) when the skin has it, else the button frame it borrowed.
            _frameSprite = _skinData != null && _skinData.RoutePanelFrameTexture != null
                ? CreateRuntimeSprite(_skinData.RoutePanelFrameTexture, _skinData.RoutePanelFrameBorder)
                : CreateRuntimeSprite(_skinData != null ? _skinData.ButtonFrameTexture : null, _skinData != null ? _skinData.ButtonFrameBorder : Vector4.zero);
            BuildPanel();
            RegisterWithUIManager();
            Refresh();
        }

        private void OnEnable()
        {
            if (RouteTargetController != null)
            {
                RouteTargetController.RouteTargetChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (RouteTargetController != null)
            {
                RouteTargetController.RouteTargetChanged -= Refresh;
            }
        }

        private void OnDestroy()
        {
            if (_frameSprite == null)
            {
                UIManager.UnregisterPanel(_managedPanel);
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_frameSprite);
            }
            else
            {
                DestroyImmediate(_frameSprite);
            }

            UIManager.UnregisterPanel(_managedPanel);
        }

        private void BuildPanel()
        {
            _presenterRoot = EnsurePresenterRoot("RoutePanelPresenterRoot");
            _canvasGroup = _presenterRoot.GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = _presenterRoot.gameObject.AddComponent<CanvasGroup>();
            }

            var overlay = CreateImage("RouteOverlay", _presenterRoot, null, new Color(0.02f, 0.04f, 0.06f, 0.78f), Image.Type.Simple);
            _overlayRect = overlay.rectTransform;
            StretchToParent(_overlayRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panelTint = _skinData == null ? Color.white : (_skinData.RoutePanelFrameTexture != null ? _skinData.RoutePanelFrameTint : _skinData.SelectedFrameTint);
            var panel = CreateImage("RoutePanel", _presenterRoot, _frameSprite, panelTint, ResolvePanelImageType(_frameSprite));
            _panelImage = panel;
            _panelRect = panel.rectTransform;
            SetAnchoredBox(_panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, PanelHeight));

            _titleText = CreateText("Title", _panelRect, TitleFontSize, FontStyle.Bold, ProceduralUIBuilder.ResolveAccentColor(), TextAnchor.UpperLeft);
            StretchToParent(_titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, -235f), new Vector2(-230f, -165f));

            _subtitleText = CreateText("Subtitle", _panelRect, SubtitleFontSize, FontStyle.Normal, ProceduralUIBuilder.ResolvePrimaryTextColor(), TextAnchor.UpperLeft);
            StretchToParent(_subtitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, -280f), new Vector2(-230f, -245f));

            _summaryText = CreateText("Summary", _panelRect, SummaryFontSize, FontStyle.Normal, ProceduralUIBuilder.ResolveSecondaryTextColor(), TextAnchor.UpperLeft);
            // Summary strings here run long enough to wrap 2-3 lines (e.g. the New Run body) -
            // the previous 55px band was sized for one line and crowded straight into the detail
            // list below it with no visible gap.
            // The summary runs 2-3 lines on the New Run panel; the old band was one line tall and
            // the rows below it drew straight through the overflow (playtest 2026-09-23).
            StretchToParent(_summaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, -430f), new Vector2(-230f, -300f));

            var detailContainer = new GameObject("DetailContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            detailContainer.transform.SetParent(_panelRect, false);
            var detailRect = detailContainer.GetComponent<RectTransform>();
            _detailRect = detailRect;
            StretchToParent(detailRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(230f, 215f), new Vector2(-230f, -445f));

            _detailLayout = detailContainer.GetComponent<VerticalLayoutGroup>();
            _detailLayout.spacing = 14f;
            _detailLayout.childAlignment = TextAnchor.UpperLeft;
            _detailLayout.childControlHeight = true;
            _detailLayout.childControlWidth = true;
            _detailLayout.childForceExpandHeight = false;
            _detailLayout.childForceExpandWidth = true;

            _footerText = CreateText("Footer", _panelRect, FooterFontSize, FontStyle.Italic, ProceduralUIBuilder.ResolveSecondaryTextColor(), TextAnchor.LowerRight);
            StretchToParent(_footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(230f, 140f), new Vector2(-230f, 195f));

            _recordsView = new RecordsPanelView(_panelRect);
            ApplyDefaultLayout();
        }

        // The layout every panel used before Records existed. Re-applied when Records closes so
        // the next panel is exactly as it was.
        private void ApplyDefaultLayout()
        {
            SetAnchoredBox(_panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, PanelHeight));
            _panelImage.pixelsPerUnitMultiplier = 1f;
            StretchToParent(_titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, -235f), new Vector2(-230f, -165f));
            StretchToParent(_subtitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, -280f), new Vector2(-230f, -245f));
            _summaryText.gameObject.SetActive(true);
            StretchToParent(_detailRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(230f, 215f), new Vector2(-230f, -445f));
            StretchToParent(_footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(230f, 140f), new Vector2(-230f, 195f));
            _recordsLayoutApplied = false;
        }

        // Title, subtitle, a tab strip, then either the run rows or the card grid with one line
        // describing the selected card beneath it. Distances are from the panel's top edge.
        private void ApplyRecordsLayout()
        {
            SetAnchoredBox(_panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, RecordsPanelSize);
            _panelImage.pixelsPerUnitMultiplier = RecordsFrameScale;
            var side = RecordsSideInset;
            StretchToParent(_titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(side, -165f), new Vector2(-side, -105f));
            StretchToParent(_subtitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(side, -202f), new Vector2(-side, -168f));
            _summaryText.gameObject.SetActive(false);
            StretchToParent(_detailRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(side, 150f), new Vector2(-side, -280f));
            StretchToParent(_footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(side, 100f), new Vector2(-side, 142f));
            _recordsView.Layout(side, 212f, 262f, 282f, 815f, 822f, 890f);
            _recordsLayoutApplied = true;
        }

        private void Refresh()
        {
            var panelData = RouteTargetController != null ? RouteTargetController.ActivePanelData : null;
            var isVisible = panelData != null;

            EnsureForegroundOrder();

            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = isVisible;
            _canvasGroup.interactable = isVisible;

            if (!isVisible)
            {
                return;
            }

            var records = RouteTargetController as IRecordsViewSource;
            var isRecords = records != null && records.ActivePanelRoute == MainMenuRoute.Records;

            if (isRecords && !_recordsLayoutApplied)
            {
                ApplyRecordsLayout();
            }
            else if (!isRecords && _recordsLayoutApplied)
            {
                ApplyDefaultLayout();
            }

            _titleText.text = RouteTargetController.ActivePanelTitleOverride ?? panelData.Title;
            _subtitleText.text = RouteTargetController.ActivePanelSubtitleOverride ?? panelData.Subtitle;
            _summaryText.text = RouteTargetController.ActivePanelSummaryOverride ?? panelData.Summary;
            _footerText.text = panelData.FooterHint;
            RebuildDetailLines(RouteTargetController.ActivePanelDetailLinesOverride ?? panelData.DetailLines);

            if (isRecords)
            {
                _recordsView.Show(records);
            }
            else
            {
                _recordsView.Hide();
            }

            UIManager.NotifyPanelChanged(_managedPanel);
        }

        // Mouse on the panel rows: hovering selects, clicking confirms. The main menu scene has an
        // EventSystem, but this canvas has no raycaster of its own, so the rows are hit-tested here
        // exactly as the reward screen does it (playtest 2026-09-23: "profiles cannot be selected
        // via mouse click").
        private Vector2 _lastPointer;

        private void Update()
        {
            var controller = RouteTargetController;

            if (controller == null || controller.ActivePanelData == null)
            {
                return;
            }

            UIPanelFit.Fit(_panelRect, _recordsLayoutApplied ? RecordsPanelSize : new Vector2(PanelWidth, PanelHeight));
            var mouse = UnityEngine.InputSystem.Mouse.current;

            if (mouse == null)
            {
                return;
            }

            var pointer = mouse.position.ReadValue();
            var moved = (pointer - _lastPointer).sqrMagnitude > 4f;
            _lastPointer = pointer;
            var clicked = mouse.leftButton.wasPressedThisFrame;

            if (!moved && !clicked)
            {
                return;
            }

            if (controller is IRecordsViewSource records && records.ActivePanelRoute == MainMenuRoute.Records)
            {
                // The Logs tab is a list of rows like any other panel; the other tabs are not.
                if (_recordsView.HandlePointer(records, pointer, moved, clicked) || records.RecordsTab != 2)
                {
                    return;
                }
            }

            if (controller.ActivePanelSelectedIndex < 0)
            {
                return;
            }

            for (var index = 0; index < _detailTexts.Count; index++)
            {
                var row = _detailTexts[index];

                if (row == null || !RectTransformUtility.RectangleContainsScreenPoint(row.rectTransform, pointer, null))
                {
                    continue;
                }

                controller.TrySelectPanelRow(index);

                if (clicked)
                {
                    controller.TryHandleConfirm();
                }

                return;
            }
        }

        private void EnsureForegroundOrder()
        {
            if (_overlayRect != null)
            {
                _overlayRect.SetAsLastSibling();
            }

            if (_panelRect != null)
            {
                _panelRect.SetAsLastSibling();
            }
        }

        private void RebuildDetailLines(string[] detailLines)
        {
            _detailTexts.Clear();

            if (_detailLayout == null)
            {
                return;
            }

            for (var index = _detailLayout.transform.childCount - 1; index >= 0; index--)
            {
                var child = _detailLayout.transform.GetChild(index);

                if (Application.isPlaying)
                {
                    // Destroy is deferred and ProceduralUIFactory reuses a child it finds by name,
                    // so a row rebuilt in the same frame attaches to the doomed one and vanishes
                    // with it. Playtest 2026-09-22: "pressing down once the content of the menus is
                    // empty; pressing it again shows all and moves two slots" - the first press did
                    // move the selection, the screen just rendered nothing.
                    child.name = child.name + "_Discarded";
                    child.gameObject.SetActive(false);
                    child.SetParent(null, false);
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            if (detailLines == null)
            {
                return;
            }

            var selectedIndex = RouteTargetController != null ? RouteTargetController.ActivePanelSelectedIndex : -1;

            for (var index = 0; index < detailLines.Length; index++)
            {
                var isSelected = index == selectedIndex;
                var color = isSelected ? ProceduralUIBuilder.ResolveAccentColor() : ProceduralUIBuilder.ResolveSecondaryTextColor();
                var detailText = CreateText("Detail" + index, _detailLayout.transform as RectTransform, DetailFontSize, isSelected ? FontStyle.Bold : FontStyle.Normal, color, TextAnchor.UpperLeft);
                // A blank line is a spacer; a bullet on it read as a stray dash (Records, 2026-09-29).
                detailText.text = string.IsNullOrEmpty(detailLines[index]) ? string.Empty : (isSelected ? "> " : "- ") + detailLines[index];
                detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
                detailText.verticalOverflow = VerticalWrapMode.Overflow;

                // No LayoutElement: with childControlHeight the group then uses the text's own
                // preferred height. A fixed 44 px made a wrapped row overlap its neighbours, which
                // is what made the New Run panel unreadable (playtest 2026-09-22).
                _detailTexts.Add(detailText);
            }
        }

        // Open Topics 3.1 (2026-10-07): the route panel is the fourth presenter on UIThemeData. Its
        // text takes the theme's font and colours - accent for the title and the selected row,
        // primary for the subtitle, secondary for everything else - which are the menu skin's
        // selected, prompt and idle colours to within a few percent. The frame stays the skin's.
        private static Text CreateText(string name, RectTransform parent, int fontSize, FontStyle fontStyle, Color color, TextAnchor anchor)
        {
            return ProceduralUIBuilder.CreateText(name, parent, fontSize, fontStyle, anchor, color);
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color, Image.Type imageType)
        {
            return ProceduralUIBuilder.CreateImage(name, parent, sprite, color, imageType);
        }

        private static Image.Type ResolvePanelImageType(Sprite sprite)
        {
            return sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        private RectTransform EnsurePresenterRoot(string childName)
        {
            var childTransform = transform.Find(childName);

            if (childTransform == null)
            {
                var childObject = new GameObject(childName, typeof(RectTransform));
                childObject.transform.SetParent(transform, false);
                childTransform = childObject.transform;
            }

            var rectTransform = childTransform as RectTransform;

            if (rectTransform == null)
            {
                rectTransform = childTransform.gameObject.AddComponent<RectTransform>();
            }

            StretchToParent(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rectTransform;
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

        private void RegisterWithUIManager()
        {
            _managedPanel = UIManager.RegisterPanel(_presenterRoot, "main_menu_route_panel", UIPanelLayer.BlockingOverlay, true, true, _canvasGroup);
        }
    }
}