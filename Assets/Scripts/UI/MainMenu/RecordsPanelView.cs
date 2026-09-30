using System.Collections.Generic;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.MainMenu
{
    // The Records screen's tab strip and card collection grid (playtest 2026-09-29: "an overview
    // of all cards, blurred if not collected, clear if collected once").
    //
    // uGUI has no blur, and a render-texture pass is a lot of machinery for one menu page. A card
    // here is only a frame, an icon and a name, so an uncollected one is drawn as a stack of
    // faint, slightly offset copies of its icon and name over a dimmed frame. That reads as out of
    // focus: the rarity and the shape of the thing are visible, the name is not.
    //
    // Owned by MainMenuRoutePanelPresenter, which decides when Records is showing. Everything the
    // view draws comes from IRecordsViewSource, so keyboard, pad and mouse share one selection.
    public sealed class RecordsPanelView
    {
        private const string CommonFramePath = "Assets/Art/Cards/Frames/card_frame_common.png";
        private const string RareFramePath = "Assets/Art/Cards/Frames/card_frame_rare.png";
        private const string EpicFramePath = "Assets/Art/Cards/Frames/card_frame_epic.png";
        private const string LegendaryFramePath = "Assets/Art/Cards/Frames/card_frame_legendary.png";

        private const float TabWidth = 260f;
        private const float MaxCellHeight = 150f;
        private const float CardAspect = 0.82f;
        private const int GhostCopies = 10;

        private static readonly Color UncollectedFrameTint = new Color(0.42f, 0.45f, 0.5f, 0.55f);
        private static readonly Color GhostTint = new Color(0.82f, 0.86f, 0.9f, 1f);
        private static readonly Color CollectedNameColor = new Color(0.16f, 0.18f, 0.22f, 1f);

        private readonly RectTransform _root;
        private readonly RectTransform _tabsRoot;
        private readonly RectTransform _gridRoot;
        private readonly Text[] _tabLabels = new Text[4];
        private readonly Image[] _tabUnderlines = new Image[4];
        private readonly Text _cardDetail;
        private readonly Font _font;
        private readonly Color _selectedColor;
        private readonly Color _idleColor;
        private readonly List<RectTransform> _cells = new List<RectTransform>();
        private readonly Sprite[] _frames = new Sprite[4];

        private RectTransform _selectionHalo;
        private int _builtCount = -1;
        private int _builtCollected = -1;

        public RecordsPanelView(RectTransform panel, Font font, Color selectedColor, Color idleColor)
        {
            _font = font;
            _selectedColor = selectedColor;
            _idleColor = idleColor;

            _root = new GameObject("RecordsView", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.SetParent(panel, false);
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;

            _tabsRoot = new GameObject("Tabs", typeof(RectTransform)).GetComponent<RectTransform>();
            _tabsRoot.SetParent(_root, false);

            for (var index = 0; index < _tabLabels.Length; index++)
            {
                var label = ProceduralUIFactory.CreateText("Tab" + index, _tabsRoot, _font, 26, FontStyle.Bold, _idleColor, TextAnchor.MiddleCenter);
                var rect = label.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(TabWidth, 0f);
                rect.anchoredPosition = new Vector2(index * (TabWidth + 16f), 0f);
                _tabLabels[index] = label;

                var underline = ProceduralUIFactory.CreateImage("TabUnderline" + index, _tabsRoot, null, _selectedColor, Image.Type.Simple);
                underline.raycastTarget = false;
                var underlineRect = underline.rectTransform;
                underlineRect.anchorMin = new Vector2(0f, 0f);
                underlineRect.anchorMax = new Vector2(0f, 0f);
                underlineRect.pivot = new Vector2(0f, 0f);
                underlineRect.sizeDelta = new Vector2(TabWidth, 3f);
                underlineRect.anchoredPosition = new Vector2(index * (TabWidth + 16f), 0f);
                _tabUnderlines[index] = underline;
            }

            _gridRoot = new GameObject("CardGrid", typeof(RectTransform)).GetComponent<RectTransform>();
            _gridRoot.SetParent(_root, false);

            _cardDetail = ProceduralUIFactory.CreateText("CardDetail", _root, _font, 21, FontStyle.Normal, _idleColor, TextAnchor.MiddleLeft);
            _cardDetail.horizontalOverflow = HorizontalWrapMode.Wrap;
            _cardDetail.verticalOverflow = VerticalWrapMode.Overflow;
            _cardDetail.supportRichText = true;

            _frames[0] = LoadFrame(CommonFramePath);
            _frames[1] = LoadFrame(RareFramePath);
            _frames[2] = LoadFrame(EpicFramePath);
            _frames[3] = LoadFrame(LegendaryFramePath);

            _root.gameObject.SetActive(false);
        }

        // Positions are passed in rather than hard-coded so the presenter keeps the one place
        // where the Records layout is described. Every value is a distance from the panel's top.
        public void Layout(float sideInset, float tabsTop, float tabsBottom, float gridTop, float gridBottom, float detailTop, float detailBottom)
        {
            SetBand(_tabsRoot, sideInset, tabsTop, tabsBottom);
            SetBand(_gridRoot, sideInset, gridTop, gridBottom);
            SetBand(_cardDetail.rectTransform, sideInset, detailTop, detailBottom);
            _builtCount = -1;
        }

        public void Hide()
        {
            if (_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }

        public void Show(IRecordsViewSource source)
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();

            var cards = source.RecordsCards;
            var collected = source.RecordsCollectedCount;
            _tabLabels[0].text = LocalizationService.Resolve("Runs", "Runs");
            _tabLabels[1].text = LocalizationService.Resolve("Cards  ", "Karten  ") + collected + " / " + cards.Count;
            _tabLabels[2].text = LocalizationService.Resolve("Core Logs  ", "Kern-Logs  ") + source.RecordsLogUnlockedCount + " / " + source.RecordsLogCount;
            _tabLabels[3].text = LocalizationService.Resolve("Credits", "Credits");

            for (var index = 0; index < _tabLabels.Length; index++)
            {
                var active = index == source.RecordsTab;
                _tabLabels[index].color = active ? _selectedColor : _idleColor;
                _tabUnderlines[index].gameObject.SetActive(active);
            }

            var showCards = source.RecordsTab == 1;
            var showLogs = source.RecordsTab == 2;
            _gridRoot.gameObject.SetActive(showCards);
            _cardDetail.gameObject.SetActive(showCards || showLogs);

            if (showLogs)
            {
                _cardDetail.text = source.RecordsSelectedLogText;
                return;
            }

            if (!showCards)
            {
                return;
            }

            if (_builtCount != cards.Count || _builtCollected != collected)
            {
                RebuildGrid(cards);
                _builtCount = cards.Count;
                _builtCollected = collected;
            }

            var selected = source.RecordsSelectedCard;
            PlaceHalo(selected);
            _cardDetail.text = DescribeSelected(cards, selected);
        }

        // Tabs and cards answer the mouse directly: the menu canvas has no raycaster on this
        // panel, the same reason the route panel hit-tests its own rows.
        public bool HandlePointer(IRecordsViewSource source, Vector2 pointer, bool moved, bool clicked)
        {
            if (!_root.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (clicked)
            {
                for (var index = 0; index < _tabLabels.Length; index++)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(_tabLabels[index].rectTransform, pointer, null))
                    {
                        source.TrySelectRecordsTab(index);
                        return true;
                    }
                }
            }

            if (source.RecordsTab != 1 || (!moved && !clicked))
            {
                return false;
            }

            for (var index = 0; index < _cells.Count; index++)
            {
                if (_cells[index] != null && RectTransformUtility.RectangleContainsScreenPoint(_cells[index], pointer, null))
                {
                    source.TrySelectRecordsCard(index);
                    return true;
                }
            }

            return false;
        }

        private void RebuildGrid(IReadOnlyList<RecordsCardEntry> cards)
        {
            for (var index = _gridRoot.childCount - 1; index >= 0; index--)
            {
                // Rename, deactivate and detach before Destroy: ProceduralUIFactory reuses a child
                // it finds by name, and Destroy is deferred to the end of the frame.
                var child = _gridRoot.GetChild(index);
                child.name += "_Discarded";
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                Object.Destroy(child.gameObject);
            }

            _cells.Clear();
            _selectionHalo = null;

            var columns = MainMenuRouteTargetController.RecordsCardColumns;
            var rows = Mathf.Max(1, Mathf.CeilToInt(cards.Count / (float)columns));
            var area = _gridRoot.rect.size;
            var cellWidth = area.x / columns;
            var cellHeight = Mathf.Min(MaxCellHeight, area.y / rows);
            var cardHeight = Mathf.Min(cellHeight - 8f, (cellWidth - 10f) / CardAspect);
            var cardSize = new Vector2(cardHeight * CardAspect, cardHeight);

            // An outline, not a fill: an uncollected card is translucent, and a solid highlight
            // behind it turned the selected card into a yellow block.
            _selectionHalo = new GameObject("SelectionHalo", typeof(RectTransform)).GetComponent<RectTransform>();
            _selectionHalo.SetParent(_gridRoot, false);
            _selectionHalo.sizeDelta = cardSize + new Vector2(10f, 10f);
            SetTopLeft(_selectionHalo);
            AddHaloEdge("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), Vector2.zero);
            AddHaloEdge("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 3f));
            AddHaloEdge("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(3f, 0f));
            AddHaloEdge("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-3f, 0f), Vector2.zero);

            for (var index = 0; index < cards.Count; index++)
            {
                var column = index % columns;
                var row = index / columns;
                var cell = new GameObject("Card" + index, typeof(RectTransform)).GetComponent<RectTransform>();
                cell.SetParent(_gridRoot, false);
                SetTopLeft(cell);
                cell.sizeDelta = cardSize;
                cell.anchoredPosition = new Vector2(
                    column * cellWidth + (cellWidth - cardSize.x) * 0.5f,
                    -(row * cellHeight + (cellHeight - cardSize.y) * 0.5f));
                BuildCard(cell, cards[index], cardSize);
                _cells.Add(cell);
            }
        }

        private void AddHaloEdge(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var edge = ProceduralUIFactory.CreateImage(name, _selectionHalo, null, _selectedColor, Image.Type.Simple);
            edge.raycastTarget = false;
            var rect = edge.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void BuildCard(RectTransform cell, RecordsCardEntry entry, Vector2 size)
        {
            var card = entry.Card;
            var frame = ProceduralUIFactory.CreateImage("Frame", cell, _frames[Mathf.Clamp((int)card.Rarity, 0, 3)],
                entry.Collected ? Color.white : UncollectedFrameTint, Image.Type.Sliced);
            frame.raycastTarget = false;
            // The frame's 27 px border was sized for a 282 px reward card; at a fifth of that size
            // it would swallow the card.
            frame.pixelsPerUnitMultiplier = 3.2f;
            Stretch(frame.rectTransform, Vector2.zero, Vector2.zero);

            var icon = ResolveIcon(card);
            var iconSize = size.x * 0.52f;
            var iconCentre = new Vector2(0f, size.y * 0.1f);
            var nameBand = new Vector2(size.x - 14f, size.y * 0.36f);
            var nameCentre = new Vector2(0f, -size.y * 0.26f);

            if (entry.Collected)
            {
                AddIcon(cell, "Icon", icon, iconCentre, iconSize, Color.white);
                // Dark ink on the frame's cream panel, as on the reward cards.
                AddName(cell, "Name", card.DisplayName, nameCentre, nameBand, CollectedNameColor, FontStyle.Bold);
                return;
            }

            for (var copy = 0; copy < GhostCopies; copy++)
            {
                var angle = copy * Mathf.PI * 2f / GhostCopies;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddIcon(cell, "IconGhost" + copy, icon, iconCentre + offset * 4.5f, iconSize, new Color(GhostTint.r, GhostTint.g, GhostTint.b, 0.13f));
                AddName(cell, "NameGhost" + copy, card.DisplayName, nameCentre + offset * 3f, nameBand,
                    new Color(GhostTint.r, GhostTint.g, GhostTint.b, 0.16f), FontStyle.Bold);
            }
        }

        private void AddIcon(RectTransform cell, string name, Sprite sprite, Vector2 centre, float size, Color color)
        {
            if (sprite == null)
            {
                return;
            }

            var image = ProceduralUIFactory.CreateImage(name, cell, sprite, color, Image.Type.Simple);
            image.preserveAspect = true;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = centre;
        }

        private void AddName(RectTransform cell, string name, string text, Vector2 centre, Vector2 band, Color color, FontStyle style)
        {
            var label = ProceduralUIFactory.CreateText(name, cell, _font, 13, style, color, TextAnchor.MiddleCenter);
            label.text = text;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 13;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = band;
            rect.anchoredPosition = centre;
        }

        private void PlaceHalo(int selected)
        {
            if (_selectionHalo == null)
            {
                return;
            }

            var visible = selected >= 0 && selected < _cells.Count;
            _selectionHalo.gameObject.SetActive(visible);

            if (!visible)
            {
                return;
            }

            _selectionHalo.anchoredPosition = _cells[selected].anchoredPosition + new Vector2(-5f, 5f);
            // On top: it is only an outline, so it hides nothing of the card.
            _selectionHalo.SetAsLastSibling();
        }

        private static string DescribeSelected(IReadOnlyList<RecordsCardEntry> cards, int selected)
        {
            if (selected < 0 || selected >= cards.Count)
            {
                return string.Empty;
            }

            var entry = cards[selected];
            var rarity = RarityName(entry.Card.Rarity);

            if (!entry.Collected)
            {
                return rarity + "   ·   " + LocalizationService.Resolve(
                    "Not collected yet - take it from a reward or the shop once to reveal it.",
                    "Noch nicht gesammelt - einmal aus einer Belohnung oder dem Shop nehmen, um sie aufzudecken.");
            }

            var numbers = CardEffectDescriber.Describe(entry.Card);
            var description = entry.Card.Description;
            var body = string.IsNullOrWhiteSpace(description) ? numbers
                : string.IsNullOrWhiteSpace(numbers) ? description
                : description + "   " + numbers;
            return "<b>" + entry.Card.DisplayName + "</b>   ·   " + rarity + "   ·   " + body;
        }

        private static string RarityName(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Rare:
                    return LocalizationService.Resolve("Rare", "Selten");
                case CardRarity.Epic:
                    return LocalizationService.Resolve("Epic", "Episch");
                case CardRarity.Legendary:
                    return LocalizationService.Resolve("Legendary", "Legendär");
                default:
                    return LocalizationService.Resolve("Common", "Gewöhnlich");
            }
        }

        // A weapon card shows its weapon, as on the reward screen; any other card its first tag.
        private static Sprite ResolveIcon(CardData card)
        {
            if (card.IsWeaponCard && card.GrantedWeapon != null && card.GrantedWeapon.Presentation != null)
            {
                var weaponIcon = card.GrantedWeapon.Presentation.ResolveIconSprite();

                if (weaponIcon != null)
                {
                    return weaponIcon;
                }
            }

            var tags = card.Tags;
            return tags != null && tags.Length > 0 ? CardTagIconAtlas.ResolveIcon(tags[0]) : null;
        }

        private static Sprite LoadFrame(string path)
        {
            return DebugSpriteFactory.LoadProjectSpriteRect(path, CardRewardPresenter.CardFrameContentRect, 100f, CardRewardPresenter.CardFrameBorder);
        }

        private static void SetBand(RectTransform rect, float sideInset, float top, float bottom)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(sideInset, -bottom);
            rect.offsetMax = new Vector2(-sideInset, -top);
        }

        private static void SetTopLeft(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
