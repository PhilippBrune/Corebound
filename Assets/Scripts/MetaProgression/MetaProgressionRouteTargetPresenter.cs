using System.Collections.Generic;
using Corebound.UI;
using Corebound.UI.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.MetaProgression
{
    public sealed class MetaProgressionRouteTargetPresenter : MonoBehaviour
    {
        // Playtest 2026-09-29 widened the panel again: a branch tab is now the skill graph with a
        // details column beside it, and a radial graph needs height as much as width. The frame's
        // border is drawn thinner (RecordsFrameScale-style multiplier) so the art does not eat the
        // extra room.
        private const float PanelWidth = 1800f;
        private const float PanelHeight = 1020f;
        private const float FrameScale = 1.6f;
        private const float SideInset = 165f;
        private const float DetailsColumnWidth = 440f;
        private const int TabFontSize = 22;
        private const float TabSpacing = 26f;

        private static readonly Color UnaffordableRowColor = new Color(0.95f, 0.55f, 0.45f, 1f);
        private static readonly Color LockedRowColor = new Color(0.5f, 0.52f, 0.57f, 1f);
        private static readonly Color HeaderRowColor = new Color(1f, 0.82f, 0.45f, 1f);
        private static readonly Color ActiveTabColor = new Color(1f, 0.78f, 0.35f, 1f);
        private static readonly Color RespecIdleTint = new Color(0.16f, 0.2f, 0.26f, 0.92f);
        private static readonly Color RespecSelectedTint = new Color(0.45f, 0.3f, 0.12f, 0.95f);
        private const int TitleFontSize = 42;
        private const int CurrencyFontSize = 26;
        private const int SummaryFontSize = 22;
        private const int RowFontSize = 24;

        private readonly List<Text> _rowTexts = new List<Text>();
        private readonly List<Text> _tabLabels = new List<Text>();

        [SerializeField] private MainMenuSkinData _skinData;
        [SerializeField] private MetaProgressionRouteTargetController _controller;

        private RectTransform _presenterRoot;
        private CanvasGroup _canvasGroup;
        private RectTransform _overlayRect;
        private RectTransform _panelRect;
        private VerticalLayoutGroup _rowLayout;
        private ScrollRect _scrollRect;
        private RectTransform _tabsRoot;
        private Image _tabUnderline;
        private RectTransform _graphArea;
        private RectTransform _detailsColumn;
        private Text _detailTitle;
        private Text _detailStatus;
        private Text _detailBody;
        private Image _respecButton;
        private Text _respecLabel;
        private Vector2 _lastPointer;

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
        private const float HeaderY = -238f;
        private static readonly Rect[] BranchIconRects =
        {
            new Rect(55f, 485f, 370f, 370f),
            new Rect(510f, 485f, 320f, 365f),
            new Rect(75f, 70f, 330f, 350f),
            new Rect(545f, 70f, 260f, 350f),
        };
        private readonly Sprite[] _branchSprites = new Sprite[4];
        private Text _summaryText;
        private Text _footerText;
        private Font _font;
        private Sprite _frameSprite;
        private UIManagedPanel _managedPanel;
        private SkillTree.SkillTreeGraphPresenter _skillGraph;

        // The menu scene's EventSystem never reaches this canvas (no raycaster on it), so every
        // pointer interaction here is a manual hit-test, as on the pause menu and route panel.
        private void Update()
        {
            if (_controller == null || !_controller.IsOpen)
            {
                return;
            }

            UIPanelFit.Fit(_panelRect, new Vector2(PanelWidth, PanelHeight));
            var mouse = UnityEngine.InputSystem.Mouse.current;

            if (mouse == null)
            {
                return;
            }

            var pointer = mouse.position.ReadValue();
            HandleWheel(mouse.scroll.ReadValue().y, pointer);

            var moved = (pointer - _lastPointer).sqrMagnitude > 4f;
            _lastPointer = pointer;
            var clicked = mouse.leftButton.wasPressedThisFrame;

            if (!moved && !clicked)
            {
                return;
            }

            if (clicked && TryClickTab(pointer))
            {
                return;
            }

            var row = _controller.IsSkillBranchTabActive ? HitGraphRow(pointer) : HitListRow(pointer);

            if (row < 0)
            {
                return;
            }

            _controller.TrySelectRow(row);

            if (clicked && _controller.SelectedRow == row)
            {
                _controller.TryHandleConfirm();
            }
        }

#if UNITY_EDITOR
        // Autopilot: a wheel event without a real mouse, raw value as the Input System reports it.
        public void DebugScrollWheel(float rawScroll)
        {
            UIScrollUtility.ScrollBy(_scrollRect, UIScrollUtility.WheelToPixels(rawScroll));
        }
#endif

        private void HandleWheel(float scroll, Vector2 pointer)
        {
            if (_scrollRect == null || !_scrollRect.gameObject.activeInHierarchy || _scrollRect.viewport == null
                || !RectTransformUtility.RectangleContainsScreenPoint(_scrollRect.viewport, pointer, null))
            {
                return;
            }

            UIScrollUtility.ScrollBy(_scrollRect, UIScrollUtility.WheelToPixels(scroll));
        }

        private bool TryClickTab(Vector2 pointer)
        {
            for (var index = 0; index < _tabLabels.Count; index++)
            {
                if (_tabLabels[index] != null && _tabLabels[index].gameObject.activeSelf
                    && RectTransformUtility.RectangleContainsScreenPoint(_tabLabels[index].rectTransform, pointer, null))
                {
                    _controller.SelectTab(index);
                    return true;
                }
            }

            return false;
        }

        private int HitGraphRow(Vector2 pointer)
        {
            if (_respecButton != null && _respecButton.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(_respecButton.rectTransform, pointer, null))
            {
                return _controller.RespecRowIndex;
            }

            return _skillGraph != null && _skillGraph.TryHitNode(pointer, out var keyIndex, out var subIndex)
                ? _controller.FindGraphRow(keyIndex, subIndex)
                : -1;
        }

        private int HitListRow(Vector2 pointer)
        {
            // Rows scrolled out of the viewport are still laid out; only the visible ones count.
            if (_scrollRect == null || _scrollRect.viewport == null
                || !RectTransformUtility.RectangleContainsScreenPoint(_scrollRect.viewport, pointer, null))
            {
                return -1;
            }

            for (var index = 0; index < _rowTexts.Count; index++)
            {
                if (_rowTexts[index] != null && RectTransformUtility.RectangleContainsScreenPoint(_rowTexts[index].rectTransform, pointer, null))
                {
                    return index;
                }
            }

            return -1;
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
            panel.pixelsPerUnitMultiplier = FrameScale;
            _panelRect = panel.rectTransform;
            SetAnchoredBox(_panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, PanelHeight));

            _titleText = CreateText("Title", _panelRect, TitleFontSize, FontStyle.Bold, _skinData != null ? _skinData.SelectedLabelColor : Color.white, TextAnchor.UpperLeft);
            StretchToParent(_titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(SideInset, -160f), new Vector2(-SideInset, -102f));

            var tabsObject = new GameObject("Tabs", typeof(RectTransform));
            tabsObject.transform.SetParent(_panelRect, false);
            _tabsRoot = tabsObject.GetComponent<RectTransform>();
            StretchToParent(_tabsRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(SideInset, -206f), new Vector2(-SideInset, -168f));
            _tabUnderline = CreateImage("TabUnderline", _tabsRoot, null, ActiveTabColor, Image.Type.Simple);
            _tabUnderline.raycastTarget = false;

            // Header: [Alloy icon] Alloy: 19    [SP icon] Skill Points: 3    Ascension 2
            var headerColor = _skinData != null ? _skinData.PromptLabelColor : Color.white;
            CreateHeaderIcon("AlloyIcon", AlloyIconPath, SideInset);
            _currencyText = CreateText("Currency", _panelRect, CurrencyFontSize, FontStyle.Bold, headerColor, TextAnchor.MiddleLeft);
            PlaceHeaderText(_currencyText, SideInset + HeaderIconSize + 8f, 260f);
            CreateHeaderIcon("SkillPointIcon", SkillPointIconPath, SideInset + 300f);
            _skillPointText = CreateText("SkillPoints", _panelRect, CurrencyFontSize, FontStyle.Bold, headerColor, TextAnchor.MiddleLeft);
            PlaceHeaderText(_skillPointText, SideInset + 300f + HeaderIconSize + 8f, 340f);
            _ascensionText = CreateText("Ascension", _panelRect, CurrencyFontSize, FontStyle.Bold, headerColor, TextAnchor.MiddleLeft);
            PlaceHeaderText(_ascensionText, SideInset + 700f, 300f);

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
            emblemRect.anchoredPosition = new Vector2(-SideInset, -96f);
            emblemRect.sizeDelta = new Vector2(96f, 96f);

            _summaryText = CreateText("Summary", _panelRect, SummaryFontSize, FontStyle.Normal, _skinData != null ? _skinData.StatusLabelColor : Color.white, TextAnchor.UpperLeft);
            _summaryText.supportRichText = true;
            StretchToParent(_summaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(SideInset, -345f), new Vector2(-SideInset, -266f));

            _footerText = CreateText("Footer", _panelRect, 18, FontStyle.Normal, _skinData != null ? _skinData.PromptLabelColor : Color.white, TextAnchor.MiddleLeft);
            StretchToParent(_footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(SideInset, 100f), new Vector2(-SideInset, 136f));

            BuildRowList();
            BuildGraphArea();
        }

        // Viewport (RectMask2D, clips) + ScrollRect wrapping a content object that's allowed to
        // grow (ContentSizeFitter), so the list keeps working as more entries get added.
        private void BuildRowList()
        {
            var viewportObject = new GameObject("RowsViewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(_panelRect, false);
            var viewportRect = viewportObject.GetComponent<RectTransform>();
            StretchToParent(viewportRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(SideInset, 150f), new Vector2(-SideInset, -355f));

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

        // Branch tabs: the graph on the left, what the selected node is and costs on the right,
        // Respec as a button under it.
        private void BuildGraphArea()
        {
            var areaObject = new GameObject("GraphArea", typeof(RectTransform));
            areaObject.transform.SetParent(_panelRect, false);
            _graphArea = areaObject.GetComponent<RectTransform>();
            StretchToParent(_graphArea, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(SideInset, 150f), new Vector2(-(SideInset + DetailsColumnWidth + 24f), -262f));

            var columnObject = new GameObject("NodeDetails", typeof(RectTransform));
            columnObject.transform.SetParent(_panelRect, false);
            _detailsColumn = columnObject.GetComponent<RectTransform>();
            _detailsColumn.anchorMin = new Vector2(1f, 0f);
            _detailsColumn.anchorMax = new Vector2(1f, 1f);
            _detailsColumn.pivot = new Vector2(1f, 0.5f);
            _detailsColumn.offsetMin = new Vector2(-(SideInset + DetailsColumnWidth), 150f);
            _detailsColumn.offsetMax = new Vector2(-SideInset, -262f);

            var backing = CreateImage("Backing", _detailsColumn, null, new Color(0.03f, 0.05f, 0.08f, 0.6f), Image.Type.Simple);
            backing.raycastTarget = false;
            StretchToParent(backing.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _detailTitle = CreateText("NodeTitle", _detailsColumn, 30, FontStyle.Bold, _skinData != null ? _skinData.SelectedLabelColor : Color.white, TextAnchor.UpperLeft);
            _detailTitle.supportRichText = true;
            _detailTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            StretchToParent(_detailTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -96f), new Vector2(-20f, -18f));

            _detailStatus = CreateText("NodeStatus", _detailsColumn, 24, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
            StretchToParent(_detailStatus.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -134f), new Vector2(-20f, -100f));

            _detailBody = CreateText("NodeBody", _detailsColumn, 21, FontStyle.Normal, _skinData != null ? _skinData.StatusLabelColor : Color.white, TextAnchor.UpperLeft);
            _detailBody.supportRichText = true;
            _detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailBody.verticalOverflow = VerticalWrapMode.Truncate;
            StretchToParent(_detailBody.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 96f), new Vector2(-20f, -146f));

            _respecButton = CreateImage("RespecButton", _detailsColumn, null, RespecIdleTint, Image.Type.Simple);
            _respecButton.raycastTarget = false;
            StretchToParent(_respecButton.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 18f), new Vector2(-20f, 82f));
            _respecLabel = CreateText("RespecLabel", _respecButton.rectTransform, 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _respecLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            StretchToParent(_respecLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, -4f));
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
            // The emblem is the only thing that differs between a branch tab and the other three.
            // An early return here (2026-09-22) skipped the tab strip and every row below it, which
            // is why Unlocks, Upgrades and Ascension rendered empty (playtest 2026-09-23).
            var iconPath = _controller.ActiveTabIconPath;
            Sprite emblem;

            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                // The weapon sheet's grid lives on its asset now; the emblem is its first cell.
                var weaponSheet = _controller.ActiveTabIconIsWeaponSheet
                    ? Corebound.Gameplay.Shared.SpriteSheetData.Find("SpriteSheet_WeaponIcons")
                    : null;
                emblem = weaponSheet != null
                    ? weaponSheet.LoadCell(0)
                    : Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(iconPath);
            }
            else
            {
                var branch = _controller.ActiveBranchIndex;
                emblem = branch >= 0 && branch < _branchSprites.Length ? _branchSprites[branch] : null;
            }

            _branchEmblem.sprite = emblem;
            _branchEmblem.enabled = emblem != null;
            RefreshTabs();
            _footerText.text = _controller.FooterHint;

            // Playtest 2026-09-29: "I see the old and the new system for progression. It should be
            // only one." A branch tab is the graph and nothing else; the list is for the tabs that
            // are lists (Unlocks, Upgrades, Ascension).
            var graphTab = _controller.IsSkillBranchTabActive;
            _scrollRect.gameObject.SetActive(!graphTab);
            _summaryText.gameObject.SetActive(!graphTab);
            _graphArea.gameObject.SetActive(graphTab);
            _detailsColumn.gameObject.SetActive(graphTab);

            if (graphTab)
            {
                ClearRows();
                RefreshSkillGraph();
                RefreshNodeDetails();
            }
            else
            {
                _summaryText.text = _controller.Summary;
                _skillGraph?.Clear();
                RebuildRows();
            }

            UIManager.NotifyPanelChanged(_managedPanel);
        }

        // One label per tab, laid out by its own width, so each is a click target (playtest
        // 2026-09-29: "the Progression tabs are not clickable").
        private void RefreshTabs()
        {
            var names = _controller.TabNames;

            while (_tabLabels.Count < names.Count)
            {
                var label = CreateText("Tab" + _tabLabels.Count, _tabsRoot, TabFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
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
                    underline.anchorMin = new Vector2(0f, 0f);
                    underline.anchorMax = new Vector2(0f, 0f);
                    underline.pivot = new Vector2(0f, 0f);
                    underline.sizeDelta = new Vector2(width - 8f, 3f);
                    underline.anchoredPosition = new Vector2(x, -2f);
                }

                x += width + TabSpacing;
            }
        }

        private void RefreshSkillGraph()
        {
            if (_skillGraph == null)
            {
                _skillGraph = gameObject.AddComponent<SkillTree.SkillTreeGraphPresenter>();
            }

            _controller.TryGetSelectedGraphNode(out var selectedKey, out var selectedSub);
            _skillGraph.Build(_graphArea, _controller.ActiveSkillTree, _controller.SkillSaveData,
                _controller.ActiveBranchIndex, selectedKey, selectedSub);
        }

        private void RefreshNodeDetails()
        {
            _detailTitle.text = _controller.SelectedLabel;
            _detailStatus.text = _controller.SelectedStatus;
            _detailStatus.color = ResolveStateColor(_controller.SelectedState, true, false);
            _detailBody.text = _controller.Summary;

            var respecRow = _controller.RespecRowIndex;
            var rows = _controller.Rows;
            var hasRespec = respecRow >= 0 && respecRow < rows.Count;
            _respecButton.gameObject.SetActive(hasRespec);

            if (!hasRespec)
            {
                return;
            }

            var selected = _controller.SelectedRow == respecRow;
            _respecButton.color = selected ? RespecSelectedTint : RespecIdleTint;
            _respecLabel.text = rows[respecRow].Label + "\n<size=15>" + rows[respecRow].StatusLabel + "</size>";
            _respecLabel.supportRichText = true;
            _respecLabel.color = rows[respecRow].State == MetaProgressionRouteTargetController.RowState.Locked ? LockedRowColor : Color.white;
        }

        private void ClearRows()
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
        }

        private void RebuildRows()
        {
            ClearRows();

            var rows = _controller.Rows;
            RectTransform selectedRowRect = null;

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var color = ResolveStateColor(row.State, row.IsSelected, row.IsUnlocked);
                var rowText = CreateText("Row" + index, _rowLayout.transform as RectTransform, RowFontSize, row.IsUnlocked ? FontStyle.Italic : FontStyle.Normal, color, TextAnchor.MiddleLeft);
                rowText.supportRichText = true;
                rowText.text = (row.IsSelected ? "> " : "   ") + row.Label;
                rowText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

                // Status (price, rank, Locked/Owned) in its own right-aligned column, so prices
                // line up and a long label can never push the price off the row.
                var statusText = CreateText("Status", rowText.rectTransform, RowFontSize, FontStyle.Bold, color, TextAnchor.MiddleRight);
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
            UIScrollUtility.ScrollToShow(_scrollRect, rowRect);
        }

        private Color ResolveStateColor(MetaProgressionRouteTargetController.RowState state, bool isSelected, bool isUnlocked)
        {
            // SkillTree.md UI: "Unaffordable nodes are visibly distinct from locked ones."
            switch (state)
            {
                case MetaProgressionRouteTargetController.RowState.Header:
                    return HeaderRowColor;
                case MetaProgressionRouteTargetController.RowState.Locked:
                    return LockedRowColor;
                case MetaProgressionRouteTargetController.RowState.Unaffordable:
                    return isSelected ? Color.Lerp(UnaffordableRowColor, Color.white, 0.35f) : UnaffordableRowColor;
            }

            if (isUnlocked)
            {
                return _skinData != null ? _skinData.StatusLabelColor : Color.white;
            }

            return isSelected
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
            rect.anchoredPosition = new Vector2(left, HeaderY);
            rect.sizeDelta = new Vector2(HeaderIconSize, HeaderIconSize);
        }

        private static void PlaceHeaderText(Text text, float left, float width)
        {
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(left, HeaderY);
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
