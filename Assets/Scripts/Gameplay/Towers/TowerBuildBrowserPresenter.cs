using Corebound.Gameplay.Shared;
using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.Gameplay.Towers
{
    // UI proposal 2 (playtest 2026-09-21): "i dont like the current selection how to chose to build a
    // tower. I miss e.g. information what the towers are able of and what are there pro/cons."
    //
    // The comparison line answered "what can it do"; this answers "which one", by showing the whole
    // roster at once whenever the player stands on an empty slot during an Intermission. The
    // selected tower is drawn on the large card of tower_info_card_frame.png with its strengths and
    // weaknesses; the others on the small card. Previous/Next Tower Type moves the selection and
    // Interact builds - no new input.
    //
    // It owns its own screen-space canvas rather than living in HUDCanvas.unity, because it is a
    // transient overlay with no authored layout to respect, and it must not be parented onto
    // CombatRuntime itself (see the shared-GameObject Canvas gotcha).
    public sealed class TowerBuildBrowserPresenter : MonoBehaviour
    {
        private const string CardSheetPath = "Assets/Art/UI/HUD/tower_info_card_frame.png";

        // Pixel rects in tower_info_card_frame.png (911x911), measured by connected-component
        // analysis on 2026-09-22. Unity rects are bottom-left origin: y = 911 - (top + height).
        private static readonly Rect LargeCardRect = new Rect(44f, 342f, 525f, 525f);
        private static readonly Rect SmallCardRect = new Rect(626f, 601f, 242f, 255f);

        // Playtest 2026-09-27: "the slots are overlaid by the tower card - find a better position".
        // The browser floated 150px above the bottom edge at 300px tall, which put it squarely over
        // the arena's tower-slot row - so the one thing the player is choosing *for* was hidden by
        // the thing they were choosing *from*. Dropping it onto the bottom edge and shrinking the
        // large card frees about 190px of arena height, which clears the slot row on every stage
        // without moving the browser somewhere the eye does not already go during a build.
        //
        // The bottom-centre strip is free: the weapon-slot HUD sits bottom-left and the wave HUD
        // top-right, so this does not trade one overlap for another.
        private const float LargeCardSize = 230f;
        private static readonly Vector2 SmallCardSize = new Vector2(140f, 147f);
        private const float CardGap = 12f;
        private const float BottomOffset = 18f;
        private const int MaxCards = 6;

        private static readonly Color LockedTint = new Color(0.42f, 0.44f, 0.48f, 1f);
        private static readonly Color PlateTextColor = new Color(0.93f, 0.95f, 0.98f, 1f);
        // Playtest 2026-09-28: "green is hard to read, choose a better colour." These sit on the
        // tower card's pale parchment, where a 0.62/0.95/0.62 green is close enough in luminance to
        // the background to wash out entirely - the strength line was the least readable text on
        // the screen despite being the reason the card exists. Both are now dark enough to carry
        // their own contrast against the light plate, and still read as green and red.
        private static readonly Color StrengthColor = new Color(0.09f, 0.42f, 0.16f, 1f);
        private static readonly Color WeaknessColor = new Color(0.62f, 0.13f, 0.09f, 1f);
        private static readonly Color UnaffordableColor = new Color(1f, 0.45f, 0.4f, 1f);
        private static readonly Color DamageBarColor = new Color(1f, 0.62f, 0.25f, 0.95f);
        private static readonly Color RangeBarColor = new Color(0.35f, 0.78f, 1f, 0.95f);

        [SerializeField] private TowerService _towerService;

        private readonly CardView[] _cards = new CardView[MaxCards];
        private CanvasGroup _canvasGroup;
        private RectTransform _row;
        private Text _hintText;
        private Sprite _largeCardSprite;
        private Sprite _smallCardSprite;
        private Sprite _barSprite;
        private Font _font;
        private bool _built;
        private float _refreshTimer;

        // Affordability changes while the browser is open (pickups land during an Intermission),
        // so the cards are refreshed on a short timer as well as on selection changes.
        private const float RefreshIntervalSeconds = 0.25f;
        private TowerData _lastSelected;
        private TowerRuntimeController _lastUpgradeTarget;

        public void Bind(TowerService towerService)
        {
            _towerService = towerService;
        }

        private void Start()
        {
            if (_towerService == null)
            {
                _towerService = GetComponent<TowerService>();
            }
        }

        private void LateUpdate()
        {
            var isActive = _towerService != null && _towerService.IsBuildBrowserActive;
            var upgradeTarget = !isActive && _towerService != null ? _towerService.UpgradeCandidate : null;

            if (isActive || upgradeTarget != null)
            {
                HandleCardClick(upgradeTarget != null);
            }

            // Built tower under the player: the same card, showing the upgrade instead of the roster.
            if (upgradeTarget != null)
            {
                EnsureBuilt();
                _canvasGroup.alpha = 1f;
                _lastSelected = null;
                _refreshTimer -= Time.unscaledDeltaTime;

                if (upgradeTarget != _lastUpgradeTarget || _refreshTimer <= 0f)
                {
                    _lastUpgradeTarget = upgradeTarget;
                    _refreshTimer = RefreshIntervalSeconds;
                    RefreshUpgrade(upgradeTarget);
                }

                return;
            }

            _lastUpgradeTarget = null;

            if (!isActive)
            {
                if (_canvasGroup != null && _canvasGroup.alpha > 0f)
                {
                    _canvasGroup.alpha = 0f;
                    _lastSelected = null;
                }

                return;
            }

            EnsureBuilt();
            _canvasGroup.alpha = 1f;

            var selected = _towerService.SelectedBuildTower;
            _refreshTimer -= Time.unscaledDeltaTime;

            if (selected != _lastSelected || _refreshTimer <= 0f)
            {
                _lastSelected = selected;
                _refreshTimer = RefreshIntervalSeconds;
                Refresh(selected);
            }
        }

        // Playtest 2026-09-29: "check that I can click with the mouse everywhere." Clicking a card
        // selects that tower; clicking the card that is already selected builds it, as Interact
        // does. On a built tower the one card is the upgrade, and clicking it upgrades. The
        // gameplay scene has no EventSystem, so the cards are hit-tested here.
        private void HandleCardClick(bool upgradeMode)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;

            if (!_built || mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var pointer = mouse.position.ReadValue();
            var towers = _towerService.CatalogTowers;

            for (var index = 0; index < _cards.Length; index++)
            {
                var card = _cards[index];

                if (card == null || !card.Root.gameObject.activeInHierarchy
                    || !RectTransformUtility.RectangleContainsScreenPoint(card.Root, pointer, null))
                {
                    continue;
                }

                var tower = !upgradeMode && towers != null && index < towers.Length ? towers[index] : null;

                if (upgradeMode || tower == _towerService.SelectedBuildTower)
                {
                    if (_towerService.CanHandleInteract)
                    {
                        _towerService.HandleInteract();
                    }
                }
                else
                {
                    _towerService.TrySelectBuildTower(tower);
                }

                return;
            }
        }

        private void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            _built = true;
            _font = UIFontResolver.Resolve();
            _largeCardSprite = DebugSpriteFactory.LoadProjectSpriteRect(CardSheetPath, LargeCardRect);
            _smallCardSprite = DebugSpriteFactory.LoadProjectSpriteRect(CardSheetPath, SmallCardRect);
            _barSprite = DebugSpriteFactory.CreateSquare(100f);

            var canvasObject = new GameObject("TowerBuildBrowserCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Above the HUD, below the relic callout (450) and the modal overlays.
            canvas.sortingOrder = 300;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var rootObject = new GameObject("TowerBuildBrowser", typeof(RectTransform), typeof(CanvasGroup));
            rootObject.transform.SetParent(canvasObject.transform, false);
            var root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, BottomOffset);
            root.sizeDelta = new Vector2(1200f, LargeCardSize + 40f);
            _canvasGroup = rootObject.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            var rowObject = new GameObject("Cards", typeof(RectTransform));
            rowObject.transform.SetParent(root, false);
            _row = rowObject.GetComponent<RectTransform>();
            _row.anchorMin = new Vector2(0.5f, 0f);
            _row.anchorMax = new Vector2(0.5f, 0f);
            _row.pivot = new Vector2(0.5f, 0f);
            _row.anchoredPosition = new Vector2(0f, 34f);
            _row.sizeDelta = new Vector2(1200f, LargeCardSize);

            _hintText = CreateText("Hint", root, 20, TextAnchor.MiddleCenter, PlateTextColor);
            var hintRect = _hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = Vector2.zero;
            hintRect.sizeDelta = new Vector2(0f, 30f);
            var hintShadow = _hintText.gameObject.AddComponent<Shadow>();
            hintShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            hintShadow.effectDistance = new Vector2(2f, -2f);

            for (var index = 0; index < MaxCards; index++)
            {
                _cards[index] = CreateCard(index);
            }
        }

        private void Refresh(TowerData selected)
        {
            var towers = _towerService.CatalogTowers;
            var count = towers != null ? Mathf.Min(towers.Length, MaxCards) : 0;
            var maxDps = 0.01f;
            var maxRange = 0.01f;

            for (var index = 0; index < count; index++)
            {
                if (towers[index] == null)
                {
                    continue;
                }

                maxDps = Mathf.Max(maxDps, towers[index].DamagePerSecond);
                maxRange = Mathf.Max(maxRange, towers[index].Range);
            }

            var totalWidth = 0f;

            for (var index = 0; index < count; index++)
            {
                totalWidth += (towers[index] == selected ? LargeCardSize : SmallCardSize.x) + (index > 0 ? CardGap : 0f);
            }

            var cursor = -totalWidth * 0.5f;

            for (var index = 0; index < _cards.Length; index++)
            {
                var card = _cards[index];
                var tower = index < count ? towers[index] : null;
                card.Root.gameObject.SetActive(tower != null);

                if (tower == null)
                {
                    continue;
                }

                var isSelected = tower == selected;
                var isUnlocked = _towerService.IsTowerUnlocked(tower);
                var size = isSelected ? new Vector2(LargeCardSize, LargeCardSize) : SmallCardSize;
                card.Root.sizeDelta = size;
                card.Root.anchoredPosition = new Vector2(cursor + size.x * 0.5f, 0f);
                cursor += size.x + CardGap;

                ApplyCard(card, tower, isSelected, isUnlocked, tower.DamagePerSecond / maxDps, tower.Range / maxRange);
            }

            _hintText.text = LocalizationService.Resolve(
                _towerService.PreviousTowerTypeKeyLabel + " / " + _towerService.NextTowerTypeKeyLabel + " or click  choose tower     " + _towerService.BuildInteractKeyLabel + " or click again  build     " + _towerService.LoadoutKeyLabel + "  weapons",
                _towerService.PreviousTowerTypeKeyLabel + " / " + _towerService.NextTowerTypeKeyLabel + " oder Klick  Turm wählen     " + _towerService.BuildInteractKeyLabel + " oder erneut klicken  bauen     " + _towerService.LoadoutKeyLabel + "  Waffen");
        }

        private void ApplyCard(CardView card, TowerData tower, bool isSelected, bool isUnlocked, float damageFraction, float rangeFraction)
        {
            card.Frame.sprite = isSelected ? _largeCardSprite : _smallCardSprite;
            card.Frame.color = isUnlocked ? Color.white : LockedTint;

            // Region coordinates are fractions of the card art, so the same layout works on both
            // card sizes. Large card (525 px): plate y 45-130, stat band y 165-285 with three
            // squares, text rows y 300-400, labelled boxes y 415-470. Small card (242x255): plate
            // y 12-62, stat band y 75-125, lower panel y 135-240.
            if (isSelected)
            {
                Place(card.Name.rectTransform, 525f, 525f, 80f, 50f, 445f, 98f);
                Place(card.Cost.rectTransform, 525f, 525f, 80f, 94f, 445f, 128f);
                Place(card.Icon.rectTransform, 525f, 525f, 93f, 190f, 165f, 262f);
                Place(card.DamageSlot, 525f, 525f, 230f, 190f, 302f, 262f);
                Place(card.RangeSlot, 525f, 525f, 366f, 190f, 438f, 262f);
                Place(card.Strengths.rectTransform, 525f, 525f, 80f, 304f, 450f, 352f);
                Place(card.Weaknesses.rectTransform, 525f, 525f, 80f, 356f, 450f, 402f);
                Place(card.StrongMark.rectTransform, 525f, 525f, 72f, 415f, 128f, 470f);
                Place(card.WeakMark.rectTransform, 525f, 525f, 282f, 415f, 338f, 470f);
                Place(card.StrongLabel.rectTransform, 525f, 525f, 136f, 415f, 274f, 470f);
                Place(card.WeakLabel.rectTransform, 525f, 525f, 346f, 415f, 450f, 470f);
            }
            else
            {
                Place(card.Name.rectTransform, 242f, 255f, 22f, 14f, 220f, 60f);
                Place(card.Cost.rectTransform, 242f, 255f, 22f, 140f, 220f, 186f);
                Place(card.Icon.rectTransform, 242f, 255f, 40f, 83f, 72f, 117f);
                Place(card.DamageSlot, 242f, 255f, 105f, 83f, 137f, 117f);
                Place(card.RangeSlot, 242f, 255f, 170f, 83f, 202f, 117f);
            }

            card.Strengths.gameObject.SetActive(isSelected);
            card.Weaknesses.gameObject.SetActive(isSelected);
            card.StrongMark.gameObject.SetActive(isSelected);
            card.WeakMark.gameObject.SetActive(isSelected);
            card.StrongLabel.gameObject.SetActive(isSelected);
            card.WeakLabel.gameObject.SetActive(isSelected);
            card.StrongLabel.text = LocalizationService.Resolve("Strong against", "Stark gegen");
            card.WeakLabel.text = LocalizationService.Resolve("Weak against", "Schwach gegen");
            card.DamageValue.gameObject.SetActive(isSelected);
            card.RangeValue.gameObject.SetActive(isSelected);

            card.Name.fontSize = isSelected ? 26 : 16;
            card.Name.text = tower.DisplayName;

            var icon = TowerRuntimeController.ResolveSprite(tower);
            card.Icon.sprite = icon;
            card.Icon.enabled = icon != null;
            card.Icon.color = isUnlocked ? Color.white : LockedTint;

            SetBar(card.DamageFill, card.DamageValue, damageFraction, tower.DamagePerSecond.ToString("0.#"), isUnlocked ? DamageBarColor : LockedTint);
            SetBar(card.RangeFill, card.RangeValue, rangeFraction, tower.Range.ToString("0.#"), isUnlocked ? RangeBarColor : LockedTint);

            if (!isUnlocked)
            {
                card.Cost.text = LocalizationService.Resolve("Locked - unlock in Progression", "Gesperrt - in Fortschritt freischalten");
                card.Cost.color = LockedTint;
                return;
            }

            var cost = _towerService.ResolveBuildCost(tower);
            var canAfford = _towerService.CanAffordBuild(tower);
            card.Cost.text = LocalizationService.Resolve("Cost ", "Kosten ") + cost
                + (isSelected ? "   ·   " + LocalizationService.Resolve("DPS ", "SPS ") + tower.DamagePerSecond.ToString("0.#") + "   ·   " + LocalizationService.Resolve("Range ", "Reichweite ") + tower.Range.ToString("0.#") : string.Empty);
            card.Cost.color = canAfford ? PlateTextColor : UnaffordableColor;
            card.Strengths.text = tower.Strengths;
            card.Weaknesses.text = tower.Weaknesses;
            card.Strengths.color = StrengthColor;
            card.Weaknesses.color = WeaknessColor;
            card.StrongMark.text = "+";
            card.WeakMark.text = "-";
        }

        // One large card: current level -> next level, cost, and each stat before -> after, so
        // the choice to upgrade is made on numbers instead of a label like "Level 2".
        private void RefreshUpgrade(TowerRuntimeController tower)
        {
            for (var index = 0; index < _cards.Length; index++)
            {
                _cards[index].Root.gameObject.SetActive(index == 0);
            }

            var card = _cards[0];
            var data = tower.Data;
            card.Root.sizeDelta = new Vector2(LargeCardSize, LargeCardSize);
            card.Root.anchoredPosition = new Vector2(0f, 0f);
            ApplyCard(card, data, true, true, 1f, 1f);

            var level = tower.CurrentUpgradeLevel;
            var canUpgrade = tower.CanUpgrade;
            var next = canUpgrade ? level + 1 : level;
            var dpsNow = tower.DpsAtLevel(level);
            var dpsNext = tower.DpsAtLevel(next);
            var rangeNow = tower.RangeAtLevel(level);
            var rangeNext = tower.RangeAtLevel(next);
            var intervalNow = tower.FireIntervalAtLevel(level);
            var intervalNext = tower.FireIntervalAtLevel(next);
            var maxRange = Mathf.Max(rangeNext, 0.01f);
            var maxDps = Mathf.Max(dpsNext, 0.01f);

            card.Name.text = data.DisplayName + "  " + LocalizationService.Resolve("Lv ", "St ") + (level + 1) + (canUpgrade ? " → " + (next + 1) : string.Empty);

            if (!canUpgrade)
            {
                card.Cost.text = LocalizationService.Resolve("Max level", "Maximalstufe");
                card.Cost.color = PlateTextColor;
                card.Strengths.text = LocalizationService.Resolve("DPS ", "SPS ") + dpsNow.ToString("0.#") + "   ·   " + LocalizationService.Resolve("Range ", "Reichweite ") + rangeNow.ToString("0.#");
                card.Weaknesses.text = LocalizationService.Resolve("Fires every ", "Schuss alle ") + intervalNow.ToString("0.00") + "s";
            }
            else
            {
                var cost = _towerService.ResolveUpgradeCost(tower);
                card.Cost.text = tower.UpgradeLabel + "   ·   " + LocalizationService.Resolve("Cost ", "Kosten ") + cost;
                card.Cost.color = _towerService.CanAffordUpgrade(tower) ? PlateTextColor : UnaffordableColor;
                card.Strengths.text = LocalizationService.Resolve("DPS ", "SPS ") + dpsNow.ToString("0.#") + " → " + dpsNext.ToString("0.#")
                    + "  (" + FormatDelta(dpsNow, dpsNext) + ")";
                card.Weaknesses.text = LocalizationService.Resolve("Range ", "Reichweite ") + rangeNow.ToString("0.#") + " → " + rangeNext.ToString("0.#")
                    + "   ·   " + intervalNow.ToString("0.00") + "s → " + intervalNext.ToString("0.00") + "s";
            }

            // Both text rows are data here, not pros and cons, so both read in the neutral colour.
            card.Strengths.color = StrengthColor;
            card.Weaknesses.color = PlateTextColor;
            SetBar(card.DamageFill, card.DamageValue, dpsNow / maxDps, dpsNow.ToString("0.#"), DamageBarColor);
            SetBar(card.RangeFill, card.RangeValue, rangeNow / maxRange, rangeNow.ToString("0.#"), RangeBarColor);
            card.StrongMark.text = "+";
            card.WeakMark.text = "·";

            // The boxes are decoration on the upgrade card: its rows are numbers, not pros and cons.
            card.StrongLabel.gameObject.SetActive(false);
            card.WeakLabel.gameObject.SetActive(false);

            _hintText.text = canUpgrade
                ? _towerService.BuildInteractKeyLabel + LocalizationService.Resolve("  upgrade     ", "  verbessern     ") + _towerService.LoadoutKeyLabel + LocalizationService.Resolve("  weapons", "  Waffen")
                : _towerService.LoadoutKeyLabel + LocalizationService.Resolve("  weapons", "  Waffen");
        }

        private static string FormatDelta(float before, float after)
        {
            if (before <= Mathf.Epsilon)
            {
                return "+" + after.ToString("0.#");
            }

            var percent = (after / before - 1f) * 100f;
            return (percent >= 0f ? "+" : string.Empty) + percent.ToString("0") + "%";
        }

        private static void SetBar(Image fill, Text value, float fraction, string label, Color color)
        {
            fraction = Mathf.Clamp01(fraction);
            var rect = fill.rectTransform;
            rect.anchorMin = new Vector2(0.12f, 0.1f);
            rect.anchorMax = new Vector2(0.88f, 0.1f + 0.8f * Mathf.Max(0.06f, fraction));
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            fill.color = color;
            value.text = label;
        }

        // Places a child by the source-pixel box it should cover in the card art (top-left origin).
        private static void Place(RectTransform rect, float artWidth, float artHeight, float left, float top, float right, float bottom)
        {
            rect.anchorMin = new Vector2(left / artWidth, 1f - bottom / artHeight);
            rect.anchorMax = new Vector2(right / artWidth, 1f - top / artHeight);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private CardView CreateCard(int index)
        {
            var rootObject = new GameObject("TowerCard" + index, typeof(RectTransform));
            rootObject.transform.SetParent(_row, false);
            var root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);

            var frame = ProceduralUIFactory.CreateImage("Frame", root, _largeCardSprite, Color.white, Image.Type.Simple);
            frame.raycastTarget = false;
            frame.rectTransform.anchorMin = Vector2.zero;
            frame.rectTransform.anchorMax = Vector2.one;
            frame.rectTransform.offsetMin = Vector2.zero;
            frame.rectTransform.offsetMax = Vector2.zero;

            var name = CreateText("Name", root, 26, TextAnchor.MiddleCenter, PlateTextColor);
            name.fontStyle = FontStyle.Bold;
            var cost = CreateText("Cost", root, 16, TextAnchor.MiddleCenter, PlateTextColor);

            var icon = ProceduralUIFactory.CreateImage("Icon", root, null, Color.white, Image.Type.Simple);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var damageSlot = CreateSlot("DamageSlot", root, DamageBarColor, out var damageFill, out var damageValue);
            var rangeSlot = CreateSlot("RangeSlot", root, RangeBarColor, out var rangeFill, out var rangeValue);

            var strengths = CreateText("Strengths", root, 16, TextAnchor.MiddleLeft, StrengthColor);
            var weaknesses = CreateText("Weaknesses", root, 16, TextAnchor.MiddleLeft, WeaknessColor);
            var strongMark = CreateText("StrongMark", root, 30, TextAnchor.MiddleCenter, StrengthColor);
            strongMark.text = "+";
            strongMark.fontStyle = FontStyle.Bold;
            var weakMark = CreateText("WeakMark", root, 30, TextAnchor.MiddleCenter, WeaknessColor);
            weakMark.text = "-";
            weakMark.fontStyle = FontStyle.Bold;

            // The card art used to bake "Strong against / Weak against" in English beside the two
            // boxes (Open Topics 2.2); the art is clean since 2026-10-07 and the words come from here.
            return new CardView(root, frame, name, cost, icon, damageSlot, damageFill, damageValue, rangeSlot, rangeFill, rangeValue, strengths, weaknesses, strongMark, weakMark)
            {
                StrongLabel = CreateText("StrongLabel", root, 17, TextAnchor.MiddleLeft, PlateTextColor),
                WeakLabel = CreateText("WeakLabel", root, 17, TextAnchor.MiddleLeft, PlateTextColor),
            };
        }

        // One of the card's three square wells: a vertical fill for the stat, its number on top.
        private RectTransform CreateSlot(string name, RectTransform parent, Color color, out Image fill, out Text value)
        {
            var slotObject = new GameObject(name, typeof(RectTransform));
            slotObject.transform.SetParent(parent, false);
            var slot = slotObject.GetComponent<RectTransform>();
            fill = ProceduralUIFactory.CreateImage("Fill", slot, _barSprite, color, Image.Type.Simple);
            fill.raycastTarget = false;
            value = CreateText("Value", slot, 18, TextAnchor.MiddleCenter, PlateTextColor);
            value.fontStyle = FontStyle.Bold;
            value.rectTransform.anchorMin = Vector2.zero;
            value.rectTransform.anchorMax = Vector2.one;
            value.rectTransform.offsetMin = Vector2.zero;
            value.rectTransform.offsetMax = Vector2.zero;
            var shadow = value.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            return slot;
        }

        private Text CreateText(string name, RectTransform parent, int fontSize, TextAnchor anchor, Color color)
        {
            var text = ProceduralUIFactory.CreateText(name, parent, _font, fontSize, FontStyle.Normal, color, anchor);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private sealed class CardView
        {
            public CardView(RectTransform root, Image frame, Text name, Text cost, Image icon, RectTransform damageSlot, Image damageFill, Text damageValue, RectTransform rangeSlot, Image rangeFill, Text rangeValue, Text strengths, Text weaknesses, Text strongMark, Text weakMark)
            {
                Root = root;
                Frame = frame;
                Name = name;
                Cost = cost;
                Icon = icon;
                DamageSlot = damageSlot;
                DamageFill = damageFill;
                DamageValue = damageValue;
                RangeSlot = rangeSlot;
                RangeFill = rangeFill;
                RangeValue = rangeValue;
                Strengths = strengths;
                Weaknesses = weaknesses;
                StrongMark = strongMark;
                WeakMark = weakMark;
            }

            public RectTransform Root { get; }

            public Image Frame { get; }

            public Text Name { get; }

            public Text Cost { get; }

            public Image Icon { get; }

            public RectTransform DamageSlot { get; }

            public Image DamageFill { get; }

            public Text DamageValue { get; }

            public RectTransform RangeSlot { get; }

            public Image RangeFill { get; }

            public Text RangeValue { get; }

            public Text Strengths { get; }

            public Text Weaknesses { get; }

            public Text StrongMark { get; }

            public Text WeakMark { get; }

            public Text StrongLabel { get; set; }

            public Text WeakLabel { get; set; }
        }
    }
}
