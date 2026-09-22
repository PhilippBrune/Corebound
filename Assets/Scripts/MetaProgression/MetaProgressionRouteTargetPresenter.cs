using System.Collections.Generic;
using Corebound.UI;
using Corebound.UI.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.MetaProgression
{
    public sealed class MetaProgressionRouteTargetPresenter : MonoBehaviour
    {
        // Widened 2026-09-22 for the tabbed Progression screen: seven tab names and an upgrade
        // row with a ten-pip track and a "12.0 -> 12.7" value have to fit on one line.
        private const float PanelWidth = 1500f;
        private const float PanelHeight = 900f;
        private const int TabFontSize = 22;

        private static readonly Color UnaffordableRowColor = new Color(0.95f, 0.55f, 0.45f, 1f);
        private static readonly Color LockedRowColor = new Color(0.5f, 0.52f, 0.57f, 1f);
        private static readonly Color HeaderRowColor = new Color(1f, 0.82f, 0.45f, 1f);
        private const int TitleFontSize = 42;
        private const int CurrencyFontSize = 26;
        private const int SummaryFontSize = 22;
        private const int RowFontSize = 24;

        private readonly List<Text> _rowTexts = new List<Text>();

        [SerializeField] private MainMenuSkinData _skinData;
        [SerializeField] private MetaProgressionRouteTargetController _controller;

        private RectTransform _presenterRoot;
        private CanvasGroup _canvasGroup;
        private RectTransform _overlayRect;
        private RectTransform _panelRect;
        private VerticalLayoutGroup _rowLayout;
        private ScrollRect _scrollRect;
        private Text _titleText;
        private Text _currencyText;
        private Text _skillPointText;
        private Text _ascensionText;
        private Image _branchEmblem;

        // Assets 89, 92, 93 (delivered 2026-09-22). The branch sheet is a 2 x 2 of Combat,
        // Construction, Industry, Command; rects measured on the 911 px sheet (bottom-left origin).
        private const string AlloyIconPath = "Assets/Art/Resources/resource_alloy_icon.png";
        private const string SkillPointIconPath = "Assets/Art/Resources/resource_skill_point_icon.png";
        private const string BranchSheetPath = "Assets/Art/UI/SkillTree/skill_branch_icon_sheet.png";
        private const float HeaderIconSize = 34f;
        private static readonly Rect[] BranchIconRects =
        {
            new Rect(55f, 485f, 370f, 370f),
            new Rect(510f, 485f, 320f, 365f),
            new Rect(75f, 70f, 330f, 350f),
            new Rect(545f, 70f, 260f, 350f),
        };
        private readonly Sprite[] _branchSprites = new Sprite[4];
        private Text _summaryText;
        private Text _tabText;
        private Text _footerText;
        private Font _font;
        private Sprite _frameSprite;
        private UIManagedPanel _managedPanel;

        private void Awake()
        {
            _font = ResolveFont();
            _frameSprite = CreateRuntimeSprite(_skinData != null ? _skinData.ButtonFrameTexture : null, _skinData != null ? _skinData.ButtonFrameBorder : Vector4.zero);
            BuildPanel();
            RegisterWithUIManager();
            Refresh();
        }

        private void OnEnable()
        {
            if (_controller != null)
            {
                _controller.TargetChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.TargetChanged -= Refresh;
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
            _presenterRoot = EnsurePresenterRoot("MetaProgressionPresenterRoot");
            _canvasGroup = _presenterRoot.GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = _presenterRoot.gameObject.AddComponent<CanvasGroup>();
            }

            var overlay = CreateImage("Overlay", _presenterRoot, null, new Color(0.02f, 0.04f, 0.06f, 0.78f), Image.Type.Simple);
            _overlayRect = overlay.rectTransform;
            StretchToParent(_overlayRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = CreateImage("Panel", _presenterRoot, _frameSprite, _skinData != null ? _skinData.SelectedFrameTint : Color.white, ResolvePanelType(_frameSprite));
            _panelRect = panel.rectTransform;
            SetAnchoredBox(_panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, PanelHeight));

            _titleText = CreateText("Title", _panelRect, TitleFontSize, FontStyle.Bold, _skinData != null ? _skinData.SelectedLabelColor : Color.white, TextAnchor.UpperLeft);
            // button_primary_frame.png has a 220px/140px (horiz/vert) 9-slice border baked into
            // the art itself - insets below keep content clear of that border instead of
            // rendering across it, which is what the previous much-smaller insets did.
            StretchToParent(_titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, -235f), new Vector2(-230f, -165f));

            _tabText = CreateText("Tabs", _panelRect, TabFontSize, FontStyle.Bold, _skinData != null ? _skinData.IdleLabelColor : Color.white, TextAnchor.UpperLeft);
            _tabText.supportRichText = true;
            StretchToParent(_tabText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, -275f), new Vector2(-230f, -240f));

            // Header: [Alloy icon] Alloy: 19    [SP icon] Skill Points: 3    Ascension 2
            var headerColor = _skinData != null ? _skinData.PromptLabelColor : Color.white;
            CreateHeaderIcon("AlloyIcon", AlloyIconPath, 230f);
            _currencyText = CreateText("Currency", _panelRect, CurrencyFontSize, FontStyle.Bold, headerColor, TextAnchor.MiddleLeft);
            PlaceHeaderText(_currencyText, 230f + HeaderIconSize + 8f, 260f);
            CreateHeaderIcon("SkillPointIcon", SkillPointIconPath, 530f);
            _skillPointText = CreateText("SkillPoints", _panelRect, CurrencyFontSize, FontStyle.Bold, headerColor, TextAnchor.MiddleLeft);
            PlaceHeaderText(_skillPointText, 530f + HeaderIconSize + 8f, 320f);
            _ascensionText = CreateText("Ascension", _panelRect, CurrencyFontSize, FontStyle.Bold, headerColor, TextAnchor.MiddleLeft);
            PlaceHeaderText(_ascensionText, 900f, 300f);

            for (var index = 0; index < BranchIconRects.Length; index++)
            {
                _branchSprites[index] = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSpriteRect(BranchSheetPath, BranchIconRects[index]);
            }

            _branchEmblem = CreateImage("BranchEmblem", _panelRect, null, Color.white, Image.Type.Simple);
            _branchEmblem.preserveAspect = true;
            _branchEmblem.raycastTarget = false;
            var emblemRect = _branchEmblem.rectTransform;
            emblemRect.anchorMin = new Vector2(1f, 1f);
            emblemRect.anchorMax = new Vector2(1f, 1f);
            emblemRect.pivot = new Vector2(1f, 1f);
            emblemRect.anchoredPosition = new Vector2(-240f, -150f);
            emblemRect.sizeDelta = new Vector2(110f, 110f);

            _summaryText = CreateText("Summary", _panelRect, SummaryFontSize, FontStyle.Normal, _skinData != null ? _skinData.StatusLabelColor : Color.white, TextAnchor.UpperLeft);
            _summaryText.supportRichText = true;
            StretchToParent(_summaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, -400f), new Vector2(-230f, -320f));

            _footerText = CreateText("Footer", _panelRect, 18, FontStyle.Normal, _skinData != null ? _skinData.PromptLabelColor : Color.white, TextAnchor.MiddleLeft);
            // Clear of the frame art: at x 230 / y 110 the footer sat on the orange corner piece and
            // was unreadable (playtest 2026-09-22).
            StretchToParent(_footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(300f, 150f), new Vector2(-300f, 182f));

            // Viewport (RectMask2D, clips) + ScrollRect wrapping a content object that's allowed to
            // grow (ContentSizeFitter) - same treatment as SettingsRouteTargetPresenter, so the
            // unlock list keeps working as more unlocks get added instead of silently overflowing
            // the fixed panel height like Settings' option list did.
            var viewportObject = new GameObject("RowsViewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(_panelRect, false);
            var viewportRect = viewportObject.GetComponent<RectTransform>();
            StretchToParent(viewportRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(230f, 195f), new Vector2(-230f, -410f));

            var listObject = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listObject.transform.SetParent(viewportRect, false);
            var listRect = listObject.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = Vector2.zero;

            _rowLayout = listObject.GetComponent<VerticalLayoutGroup>();
            _rowLayout.spacing = 12f;
            _rowLayout.childAlignment = TextAnchor.UpperLeft;
            _rowLayout.childControlHeight = true;
            _rowLayout.childControlWidth = true;
            _rowLayout.childForceExpandHeight = false;
            _rowLayout.childForceExpandWidth = true;

            var contentSizeFitter = listObject.GetComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _scrollRect = viewportObject.AddComponent<ScrollRect>();
            _scrollRect.content = listRect;
            _scrollRect.viewport = viewportRect;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 24f;
        }

        private void Refresh()
        {
            var isVisible = _controller != null && _controller.IsOpen;
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = isVisible;
            _canvasGroup.interactable = isVisible;

            if (!isVisible)
            {
                return;
            }

            if (_overlayRect != null)
            {
                _overlayRect.SetAsLastSibling();
            }

            _panelRect.SetAsLastSibling();
            _titleText.text = _controller.Title;
            _currencyText.text = _controller.AlloyText;
            _skillPointText.text = _controller.SkillPointText;
            _ascensionText.text = _controller.AscensionText;
            var branch = _controller.ActiveBranchIndex;
            var emblem = branch >= 0 && branch < _branchSprites.Length ? _branchSprites[branch] : null;
            _branchEmblem.sprite = emblem;
            _branchEmblem.enabled = emblem != null;
            _tabText.text = _controller.TabStrip;
            _summaryText.text = _controller.Summary;
            _footerText.text = _controller.FooterHint;
            RebuildRows();
            UIManager.NotifyPanelChanged(_managedPanel);
        }

        private void RebuildRows()
        {
            _rowTexts.Clear();

            for (var index = _rowLayout.transform.childCount - 1; index >= 0; index--)
            {
                var child = _rowLayout.transform.GetChild(index);

                if (Application.isPlaying)
                {
                    // Destroy is deferred to the end of the frame, and ProceduralUIFactory reuses a
                    // child it finds by name - so without this the rebuilt rows attach to the doomed
                    // ones and vanish with them (the Progression screen rendered no rows at all).
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

            var rows = _controller.Rows;
            RectTransform selectedRowRect = null;

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var rowText = CreateText("Row" + index, _rowLayout.transform as RectTransform, RowFontSize, row.IsUnlocked ? FontStyle.Italic : FontStyle.Normal, ResolveRowColor(row), TextAnchor.MiddleLeft);
                rowText.supportRichText = true;
                rowText.text = (row.IsSelected ? "> " : "   ") + row.Label;
                rowText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

                // Status (price, rank, Locked/Owned) in its own right-aligned column, so prices
                // line up and a long label can never push the price off the row.
                var statusText = CreateText("Status", rowText.rectTransform, RowFontSize, FontStyle.Bold, ResolveRowColor(row), TextAnchor.MiddleRight);
                StretchToParent(statusText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                statusText.text = row.StatusLabel;
                _rowTexts.Add(rowText);

                if (row.IsSelected)
                {
                    selectedRowRect = rowText.rectTransform;
                }
            }

            if (selectedRowRect != null)
            {
                ScrollToShow(selectedRowRect);
            }
        }

        private void ScrollToShow(RectTransform rowRect)
        {
            if (_scrollRect == null || _scrollRect.viewport == null || _scrollRect.content == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            var content = _scrollRect.content;
            var viewportHeight = _scrollRect.viewport.rect.height;
            var rowTop = -rowRect.anchoredPosition.y;
            var rowBottom = rowTop + rowRect.rect.height;
            var currentOffset = -content.anchoredPosition.y;

            var minOffset = rowBottom - viewportHeight;
            var maxOffset = rowTop;
            var targetOffset = Mathf.Clamp(currentOffset, minOffset, maxOffset);
            var maxScrollable = Mathf.Max(0f, content.rect.height - viewportHeight);
            targetOffset = Mathf.Clamp(targetOffset, 0f, maxScrollable);

            content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetOffset);
        }

        private Color ResolveRowColor(MetaProgressionRouteTargetController.DisplayRow row)
        {
            // SkillTree.md UI: "Unaffordable nodes are visibly distinct from locked ones."
            switch (row.State)
            {
                case MetaProgressionRouteTargetController.RowState.Header:
                    return HeaderRowColor;
                case MetaProgressionRouteTargetController.RowState.Locked:
                    return LockedRowColor;
                case MetaProgressionRouteTargetController.RowState.Unaffordable:
                    return row.IsSelected ? Color.Lerp(UnaffordableRowColor, Color.white, 0.35f) : UnaffordableRowColor;
            }

            if (row.IsUnlocked)
            {
                return _skinData != null ? _skinData.StatusLabelColor : Color.white;
            }

            return row.IsSelected
                ? (_skinData != null ? _skinData.SelectedLabelColor : Color.white)
                : (_skinData != null ? _skinData.IdleLabelColor : Color.white);
        }

        private Text CreateText(string name, RectTransform parent, int fontSize, FontStyle fontStyle, Color color, TextAnchor anchor)
        {
            return ProceduralUIFactory.CreateText(name, parent, _font, fontSize, fontStyle, color, anchor);
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color, Image.Type imageType)
        {
            return ProceduralUIFactory.CreateImage(name, parent, sprite, color, imageType);
        }

        private static Sprite CreateRuntimeSprite(Texture2D texture, Vector4 border)
        {
            if (texture == null)
            {
                return null;
            }

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, border);
            sprite.hideFlags = HideFlags.DontSaveInEditor;
            return sprite;
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

        private void CreateHeaderIcon(string name, string path, float left)
        {
            var icon = CreateImage(name, _panelRect, Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(path), Color.white, Image.Type.Simple);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var rect = icon.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(left, -297f);
            rect.sizeDelta = new Vector2(HeaderIconSize, HeaderIconSize);
        }

        private static void PlaceHeaderText(Text text, float left, float width)
        {
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(left, -297f);
            rect.sizeDelta = new Vector2(width, 36f);
        }

        private static Image.Type ResolvePanelType(Sprite sprite)
        {
            return sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        private static Font ResolveFont()
        {
            return Corebound.UI.UIFontResolver.Resolve();
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
            _managedPanel = UIManager.RegisterPanel(_presenterRoot, "meta_progression_panel", UIPanelLayer.BlockingOverlay, true, true, _canvasGroup);
        }
    }
}