using System.Collections.Generic;
using Corebound.UI;
using Corebound.UI.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.Settings
{
    public sealed class SettingsRouteTargetPresenter : MonoBehaviour
    {
        // Same frame treatment as Progression and Records (2026-09-29): larger panel, thinner
        // border, so there is room for clickable tabs and the Apply / Cancel buttons.
        private const float PanelWidth = 1500f;
        private const float PanelHeight = 940f;
        private const float FrameScale = 1.6f;
        private const float SideInset = 165f;
        private const float WheelScrollStep = 44f;
        private static readonly Color ActiveTabColor = new Color(1f, 0.78f, 0.35f, 1f);
        private static readonly Color ButtonTint = new Color(0.16f, 0.2f, 0.26f, 0.92f);
        private static readonly Color ButtonHoverTint = new Color(0.45f, 0.3f, 0.12f, 0.95f);
        private const int TitleFontSize = 42;
        private const int SummaryFontSize = 22;
        private const int FooterFontSize = 18;
        private const int CategoryFontSize = 20;
        private const int OptionFontSize = 24;

        private readonly List<Text> _rowTexts = new List<Text>();

        [SerializeField] private MainMenuSkinData _skinData;
        [SerializeField] private SettingsRouteTargetController _controller;

        private RectTransform _presenterRoot;
        private CanvasGroup _canvasGroup;
        private RectTransform _overlayRect;
        private RectTransform _panelRect;
        private VerticalLayoutGroup _rowLayout;
        private ScrollRect _scrollRect;
        private Text _titleText;
        private Text _summaryText;
        private Text _footerText;
        private Font _font;
        private Sprite _frameSprite;
        private UIManagedPanel _managedPanel;
        private RectTransform _tabsRoot;
        private Image _tabUnderline;
        private readonly List<Text> _tabLabels = new List<Text>();
        private Image _applyButton;
        private Image _cancelButton;
        private Vector2 _lastPointer;

        // Mouse (playtest 2026-09-29: "check that I can click with the mouse everywhere"). The
        // menu canvas has no raycaster reaching this panel, so everything is hit-tested here, the
        // same way the route panel and Progression do it. Left click on an option steps it
        // forward, right click steps it back - the two directions Left/Right give the keyboard.
        private void Update()
        {
            if (_controller == null || !_controller.IsOpen)
            {
                return;
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;

            if (mouse == null)
            {
                return;
            }

            var pointer = mouse.position.ReadValue();
            HandleWheel(mouse.scroll.ReadValue().y, pointer);
            var moved = (pointer - _lastPointer).sqrMagnitude > 4f;
            _lastPointer = pointer;
            var leftClick = mouse.leftButton.wasPressedThisFrame;
            var rightClick = mouse.rightButton.wasPressedThisFrame;

            UpdateButtonHover(pointer);

            if (!moved && !leftClick && !rightClick)
            {
                return;
            }

            if (leftClick)
            {
                if (Contains(_applyButton, pointer))
                {
                    _controller.TryHandleConfirm();
                    return;
                }

                if (Contains(_cancelButton, pointer))
                {
                    _controller.TryHandleCancel();
                    return;
                }

                for (var index = 0; index < _tabLabels.Count; index++)
                {
                    if (_tabLabels[index].gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(_tabLabels[index].rectTransform, pointer, null))
                    {
                        _controller.SelectTab(index);
                        return;
                    }
                }
            }

            if (_scrollRect == null || _scrollRect.viewport == null
                || !RectTransformUtility.RectangleContainsScreenPoint(_scrollRect.viewport, pointer, null))
            {
                return;
            }

            for (var index = 0; index < _rowTexts.Count; index++)
            {
                if (_rowTexts[index] == null || !RectTransformUtility.RectangleContainsScreenPoint(_rowTexts[index].rectTransform, pointer, null))
                {
                    continue;
                }

                _controller.TrySelectOption(index);

                if ((leftClick || rightClick) && _controller.SelectedOptionIndex == index)
                {
                    _controller.TryAdjustSelected(leftClick ? 1 : -1);
                }

                return;
            }
        }

        private void HandleWheel(float scroll, Vector2 pointer)
        {
            if (Mathf.Abs(scroll) < 0.01f || _scrollRect == null || _scrollRect.content == null || _scrollRect.viewport == null
                || !RectTransformUtility.RectangleContainsScreenPoint(_scrollRect.viewport, pointer, null))
            {
                return;
            }

            var content = _scrollRect.content;
            var maxScroll = Mathf.Max(0f, content.rect.height - _scrollRect.viewport.rect.height);
            var position = content.anchoredPosition;
            position.y = Mathf.Clamp(position.y - Mathf.Sign(scroll) * WheelScrollStep, 0f, maxScroll);
            content.anchoredPosition = position;
        }

        private void UpdateButtonHover(Vector2 pointer)
        {
            if (_applyButton != null)
            {
                _applyButton.color = Contains(_applyButton, pointer) ? ButtonHoverTint : ButtonTint;
            }

            if (_cancelButton != null)
            {
                _cancelButton.color = Contains(_cancelButton, pointer) ? ButtonHoverTint : ButtonTint;
            }
        }

        private static bool Contains(Image image, Vector2 pointer)
        {
            return image != null && image.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, pointer, null);
        }

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
            _presenterRoot = EnsurePresenterRoot("SettingsRoutePresenterRoot");
            _canvasGroup = _presenterRoot.GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = _presenterRoot.gameObject.AddComponent<CanvasGroup>();
            }

            var overlay = CreateImage("Overlay", _presenterRoot, null, new Color(0.02f, 0.04f, 0.06f, 0.78f), Image.Type.Simple);
            _overlayRect = overlay.rectTransform;
            StretchToParent(_overlayRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = CreateImage("Panel", _presenterRoot, _frameSprite, _skinData != null ? _skinData.SelectedFrameTint : Color.white, ResolvePanelType(_frameSprite));
            panel.pixelsPerUnitMultiplier = FrameScale;
            _panelRect = panel.rectTransform;
            SetAnchoredBox(_panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, PanelHeight));

            _titleText = CreateText("Title", _panelRect, TitleFontSize, FontStyle.Bold, _skinData != null ? _skinData.SelectedLabelColor : Color.white, TextAnchor.UpperLeft);
            // button_primary_frame.png has a 220px/140px (horiz/vert) 9-slice border baked into
            // the art itself - insets below (230/165/140) keep content clear of that border
            // instead of rendering across it, which is what the previous much-smaller insets did.
            StretchToParent(_titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(SideInset, -160f), new Vector2(-SideInset, -102f));

            _summaryText = CreateText("Summary", _panelRect, 20, FontStyle.Normal, _skinData != null ? _skinData.StatusLabelColor : Color.white, TextAnchor.UpperLeft);
            _summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
            StretchToParent(_summaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(SideInset, -210f), new Vector2(-SideInset, -164f));

            var tabsObject = new GameObject("Tabs", typeof(RectTransform));
            tabsObject.transform.SetParent(_panelRect, false);
            _tabsRoot = tabsObject.GetComponent<RectTransform>();
            StretchToParent(_tabsRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(SideInset, -258f), new Vector2(-SideInset, -220f));
            _tabUnderline = CreateImage("TabUnderline", _tabsRoot, null, ActiveTabColor, Image.Type.Simple);
            _tabUnderline.raycastTarget = false;

            // 4 categories + 13 options (17 rows at ~34-42px plus 10px spacing) need roughly 840px
            // of vertical space, but this viewport is only ~430px - without scrolling the list
            // simply ran past the bottom of the panel. Viewport (RectMask2D, clips) + ScrollRect
            // wrapping a content object that's allowed to grow (ContentSizeFitter) is the standard
            // fix instead of enlarging the panel indefinitely as more options get added later.
            var viewportObject = new GameObject("RowsViewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(_panelRect, false);
            var viewportRect = viewportObject.GetComponent<RectTransform>();
            StretchToParent(viewportRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(SideInset, 215f), new Vector2(-SideInset, -272f));

            var listObject = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listObject.transform.SetParent(viewportRect, false);
            var listRect = listObject.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = Vector2.zero;

            _rowLayout = listObject.GetComponent<VerticalLayoutGroup>();
            _rowLayout.spacing = 10f;
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

            _footerText = CreateText("Footer", _panelRect, FooterFontSize, FontStyle.Italic, _skinData != null ? _skinData.IdleLabelColor : Color.white, TextAnchor.UpperLeft);
            _footerText.horizontalOverflow = HorizontalWrapMode.Wrap;
            StretchToParent(_footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(SideInset, 100f), new Vector2(-(SideInset + 400f), 200f));

            _applyButton = CreateButton("ApplyButton", Corebound.UI.LocalizationService.Resolve("Apply", "Übernehmen"), -(SideInset + 200f));
            _cancelButton = CreateButton("CancelButton", Corebound.UI.LocalizationService.Resolve("Cancel", "Abbrechen"), -SideInset);
        }

        private Image CreateButton(string name, string label, float right)
        {
            var button = CreateImage(name, _panelRect, null, ButtonTint, Image.Type.Simple);
            button.raycastTarget = false;
            var rect = button.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(184f, 52f);
            rect.anchoredPosition = new Vector2(right, 128f);
            var text = CreateText(name + "Label", rect, 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            text.text = label;
            StretchToParent(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private void RefreshTabs()
        {
            var names = _controller.TabNames;

            while (_tabLabels.Count < names.Count)
            {
                var label = CreateText("Tab" + _tabLabels.Count, _tabsRoot, 22, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                var rect = label.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                _tabLabels.Add(label);
            }

            var x = 0f;
            var idle = _skinData != null ? _skinData.IdleLabelColor : Color.white;

            for (var index = 0; index < _tabLabels.Count; index++)
            {
                var label = _tabLabels[index];
                var visible = index < names.Count;
                label.gameObject.SetActive(visible);

                if (!visible)
                {
                    continue;
                }

                var active = index == _controller.ActiveTabIndex;
                label.text = names[index];
                label.color = active ? ActiveTabColor : idle;
                var width = label.preferredWidth + 8f;
                label.rectTransform.sizeDelta = new Vector2(width, 0f);
                label.rectTransform.anchoredPosition = new Vector2(x, 0f);

                if (active)
                {
                    var underline = _tabUnderline.rectTransform;
                    underline.anchorMin = Vector2.zero;
                    underline.anchorMax = Vector2.zero;
                    underline.pivot = Vector2.zero;
                    underline.sizeDelta = new Vector2(width - 8f, 3f);
                    underline.anchoredPosition = new Vector2(x, -2f);
                }

                x += width + 30f;
            }
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
            _summaryText.text = _controller.Summary;
            _footerText.text = _controller.SelectedDescription;
            RefreshTabs();
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

            var rows = _controller.Rows;
            RectTransform selectedRowRect = null;

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];

                // The category row is the old one-line tab strip; the clickable tab labels above
                // the list replace it, and skipping it keeps _rowTexts index == option index.
                if (row.IsCategory)
                {
                    continue;
                }

                var label = CreateText(row.PrimaryText, _rowLayout.transform as RectTransform, row.IsCategory ? CategoryFontSize : OptionFontSize, row.IsCategory ? FontStyle.Bold : FontStyle.Normal, ResolveRowColor(row), row.IsCategory ? TextAnchor.MiddleLeft : TextAnchor.MiddleLeft);
                label.text = row.IsCategory ? row.PrimaryText : row.PrimaryText + "    " + row.SecondaryText;
                label.gameObject.AddComponent<LayoutElement>().preferredHeight = row.IsCategory ? 34f : 42f;
                _rowTexts.Add(label);

                if (row.IsSelected)
                {
                    selectedRowRect = label.rectTransform;
                }
            }

            if (selectedRowRect != null)
            {
                ScrollToShow(selectedRowRect);
            }
        }

        // The list can be taller than its viewport (see BuildPanel), and keyboard navigation has
        // no other way to bring an off-screen selected row into view - a mouse-drag scroll only
        // helps a pointer-driven player, not someone navigating by Up/Down.
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

        private Color ResolveRowColor(SettingsRouteTargetController.DisplayRow row)
        {
            if (row.IsCategory)
            {
                return _skinData != null ? _skinData.PromptLabelColor : Color.white;
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
            _managedPanel = UIManager.RegisterPanel(_presenterRoot, "settings_panel", UIPanelLayer.BlockingOverlay, true, true, _canvasGroup);
        }
    }
}