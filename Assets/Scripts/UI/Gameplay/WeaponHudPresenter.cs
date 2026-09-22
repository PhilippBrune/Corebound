using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.Gameplay
{
    // WeaponLoadout.md, UI in combat: "a compact loadout strip, six small slots, each showing the
    // weapon icon and a cooldown sweep. No text during combat. Active Set Bonuses are shown as small
    // tier pips beside the strip, not as text." This replaces the single-weapon widget that
    // Weapons.md specified (name, bar, modifier line) - with six weapons firing, a per-weapon name
    // and modifier line would be six lines of text in the corner of a fight.
    //
    // Each tile also carries an ammo bar, because the reload loop (Upgrades.md option a) is only
    // fair if the player can see a magazine running dry before it does.
    //
    // Assets 90 (weapon_slot_frame.png) and 91 (weapon_set_tier_pips.png) do not exist yet; the
    // tiles are drawn procedurally and pick the art up by path once it does.
    public sealed class WeaponHudPresenter : MonoBehaviour
    {
        private const string CanvasName = "WeaponHudCanvas";
        private const string SlotFramePath = "Assets/Art/UI/HUD/weapon_slot_frame.png";
        private const float TileSize = 58f;
        private const float TileGap = 8f;
        private const float PanelPadding = 10f;
        private const float AmmoBarHeight = 6f;
        private const float PipSize = 10f;

        private static readonly Color LockedTileColor = new Color(0.1f, 0.11f, 0.14f, 0.55f);
        private static readonly Color EmptyTileColor = new Color(0.14f, 0.16f, 0.21f, 0.85f);
        private static readonly Color FilledTileColor = new Color(0.18f, 0.2f, 0.27f, 0.95f);
        private static readonly Color SweepColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color ReloadSweepColor = new Color(0.9f, 0.35f, 0.2f, 0.6f);
        private static readonly Color AmmoColor = new Color(1f, 0.82f, 0.45f, 0.95f);
        private static readonly Color PipOffColor = new Color(0.3f, 0.32f, 0.38f, 0.8f);
        private static readonly Color PipOnColor = new Color(1f, 0.78f, 0.3f, 1f);

        [SerializeField] private PlayerCombatController _playerCombatController;

        private readonly TileView[] _tiles = new TileView[WeaponLoadoutRuntime.Capacity];
        private RectTransform _canvasRoot;
        private RectTransform _panel;
        private RectTransform _pipRow;
        private readonly Image[] _pips = new Image[18];
        private Sprite _squareSprite;
        private Sprite _frameSprite;
        private UIManagedPanel _managedPanel;
        private int _renderedVersion = -1;
        private int _renderedUnlocked = -1;

        private void Awake()
        {
            ResolveCombatController();
            _squareSprite = DebugSpriteFactory.CreateSquare(100f);
            _frameSprite = DebugSpriteFactory.LoadProjectSprite(SlotFramePath);
            BuildCanvas();
            _managedPanel = UIManager.RegisterPanel(_canvasRoot, "weapon_hud", UIPanelLayer.HUD, false, false);
        }

        private void OnDestroy()
        {
            UIManager.UnregisterPanel(_managedPanel);
        }

        private void Update()
        {
            ResolveCombatController();

            if (_playerCombatController == null || _panel == null)
            {
                return;
            }

            var loadout = _playerCombatController.Loadout;

            if (loadout.Version != _renderedVersion || loadout.UnlockedSlotCount != _renderedUnlocked)
            {
                _renderedVersion = loadout.Version;
                _renderedUnlocked = loadout.UnlockedSlotCount;
                RefreshTiles(loadout);
                RefreshPips(loadout);
            }

            for (var index = 0; index < _tiles.Length; index++)
            {
                RefreshSweep(_tiles[index], loadout.GetSlot(index));
            }
        }

        private void ResolveCombatController()
        {
            if (_playerCombatController == null)
            {
                _playerCombatController = FindAnyObjectByType<PlayerCombatController>();
            }
        }

        private void RefreshTiles(WeaponLoadoutRuntime loadout)
        {
            for (var index = 0; index < _tiles.Length; index++)
            {
                var tile = _tiles[index];
                var slot = loadout.GetSlot(index);
                var isLocked = index >= loadout.UnlockedSlotCount;

                // Locked slots are shown, dimmed, so the player can see there is more to earn.
                tile.Background.color = isLocked ? LockedTileColor : (slot.IsEmpty ? EmptyTileColor : FilledTileColor);
                tile.Edge.color = slot.IsEmpty ? new Color(0f, 0f, 0f, 0f) : ResolveTierColor(slot.Tier);

                var presentation = slot.IsEmpty ? null : slot.Weapon.Presentation;
                var icon = presentation != null ? presentation.ResolveIconSprite() : null;
                tile.Icon.sprite = icon;
                tile.Icon.enabled = icon != null;
                tile.Icon.color = presentation != null ? presentation.AccentColor : Color.white;
                tile.AmmoBar.gameObject.SetActive(!slot.IsEmpty);
                tile.Letter.text = !slot.IsEmpty && icon == null ? slot.Weapon.DisplayName.Substring(0, 1) : string.Empty;
            }
        }

        // One pip row per active Set Bonus archetype: filled pips for copies held, hollow up to the
        // next tier, so "3/4 toward Focused" reads without text.
        private void RefreshPips(WeaponLoadoutRuntime loadout)
        {
            for (var index = 0; index < _pips.Length; index++)
            {
                _pips[index].gameObject.SetActive(false);
            }

            var pipIndex = 0;
            var bonuses = loadout.ActiveSetBonuses;

            for (var row = 0; row < bonuses.Count && row < 3; row++)
            {
                var bonus = bonuses[row];
                var target = bonus.NextTier != null ? bonus.NextTier.RequiredCopies : bonus.Copies;
                var accent = bonus.Weapon != null && bonus.Weapon.Presentation != null ? bonus.Weapon.Presentation.AccentColor : PipOnColor;

                for (var column = 0; column < target && column < 6 && pipIndex < _pips.Length; column++)
                {
                    var pip = _pips[pipIndex++];
                    pip.gameObject.SetActive(true);
                    pip.color = column < bonus.Copies ? accent : PipOffColor;
                    pip.rectTransform.anchoredPosition = new Vector2(column * (PipSize + 3f), -row * (PipSize + 4f));
                }
            }
        }

        private static void RefreshSweep(TileView tile, WeaponSlotRuntime slot)
        {
            if (slot == null || slot.IsEmpty)
            {
                tile.Sweep.fillAmount = 0f;
                return;
            }

            if (slot.IsReloading)
            {
                tile.Sweep.color = ReloadSweepColor;
                tile.Sweep.fillAmount = slot.ReloadNormalized;
            }
            else
            {
                tile.Sweep.color = SweepColor;
                tile.Sweep.fillAmount = slot.CooldownNormalized;
            }

            var fill = tile.AmmoFill.rectTransform;
            fill.anchorMax = new Vector2(slot.IsReloading ? 1f - slot.ReloadNormalized : slot.AmmoNormalized, 1f);
            tile.AmmoFill.color = slot.IsReloading ? ReloadSweepColor : AmmoColor;
        }

        private static Color ResolveTierColor(int tier)
        {
            switch ((CardRarity)tier)
            {
                case CardRarity.Rare:
                    return new Color(0.35f, 0.62f, 1f, 1f);
                case CardRarity.Epic:
                    return new Color(0.72f, 0.4f, 1f, 1f);
                case CardRarity.Legendary:
                    return new Color(1f, 0.72f, 0.2f, 1f);
                default:
                    return new Color(0.75f, 0.78f, 0.82f, 1f);
            }
        }

        // Same shape as BossHudPresenter.BuildCanvas, and for the same reason: this presenter can
        // share CombatRuntime with world-space services, so adding a Canvas to gameObject itself
        // would convert that Transform into a CanvasScaler-driven RectTransform and displace every
        // sibling's world-space children. The canvas lives on its own child object.
        private void BuildCanvas()
        {
            var existing = transform.Find(CanvasName);

            if (existing != null)
            {
                // A previous build of this presenter (the single-weapon panel) - discard it
                // rather than build on top of it. Renamed first: Destroy is deferred.
                existing.name = CanvasName + "_Discarded";
                existing.gameObject.SetActive(false);
                Destroy(existing.gameObject);
            }

            var canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _canvasRoot = canvasObject.GetComponent<RectTransform>();

            // Bottom-left: the boss bar owns bottom-centre and the status box owns top-right.
            var panelWidth = PanelPadding * 2f + WeaponLoadoutRuntime.Capacity * TileSize + (WeaponLoadoutRuntime.Capacity - 1) * TileGap;
            var panelHeight = PanelPadding * 2f + TileSize + AmmoBarHeight + 4f;
            var panelImage = ProceduralUIFactory.CreateImage("LoadoutStrip", _canvasRoot, _squareSprite, new Color(0.06f, 0.07f, 0.1f, 0.78f), Image.Type.Simple);
            panelImage.raycastTarget = false;
            _panel = panelImage.rectTransform;
            _panel.anchorMin = Vector2.zero;
            _panel.anchorMax = Vector2.zero;
            _panel.pivot = Vector2.zero;
            _panel.anchoredPosition = new Vector2(26f, 26f);
            _panel.sizeDelta = new Vector2(panelWidth, panelHeight);

            var font = UIFontResolver.Resolve();

            for (var index = 0; index < _tiles.Length; index++)
            {
                _tiles[index] = CreateTile(index, font);
            }

            var pipObject = new GameObject("SetPips", typeof(RectTransform));
            _pipRow = pipObject.GetComponent<RectTransform>();
            _pipRow.SetParent(_panel, false);
            _pipRow.anchorMin = new Vector2(1f, 1f);
            _pipRow.anchorMax = new Vector2(1f, 1f);
            _pipRow.pivot = new Vector2(0f, 1f);
            _pipRow.anchoredPosition = new Vector2(10f, -PanelPadding);
            _pipRow.sizeDelta = new Vector2(90f, 50f);

            for (var index = 0; index < _pips.Length; index++)
            {
                var pip = ProceduralUIFactory.CreateImage("Pip" + index, _pipRow, _squareSprite, PipOffColor, Image.Type.Simple);
                pip.raycastTarget = false;
                pip.rectTransform.anchorMin = new Vector2(0f, 1f);
                pip.rectTransform.anchorMax = new Vector2(0f, 1f);
                pip.rectTransform.pivot = new Vector2(0f, 1f);
                pip.rectTransform.sizeDelta = new Vector2(PipSize, PipSize);
                pip.gameObject.SetActive(false);
                _pips[index] = pip;
            }
        }

        private TileView CreateTile(int index, Font font)
        {
            var x = PanelPadding + index * (TileSize + TileGap);
            var edge = ProceduralUIFactory.CreateImage("Slot" + index, _panel, _frameSprite != null ? _frameSprite : _squareSprite, Color.clear, Image.Type.Simple);
            edge.raycastTarget = false;
            var edgeRect = edge.rectTransform;
            edgeRect.anchorMin = Vector2.zero;
            edgeRect.anchorMax = Vector2.zero;
            edgeRect.pivot = Vector2.zero;
            edgeRect.anchoredPosition = new Vector2(x, PanelPadding + AmmoBarHeight + 4f);
            edgeRect.sizeDelta = new Vector2(TileSize, TileSize);

            var background = ProceduralUIFactory.CreateImage("Background", edgeRect, _squareSprite, EmptyTileColor, Image.Type.Simple);
            background.raycastTarget = false;
            Stretch(background.rectTransform, 3f);

            var icon = ProceduralUIFactory.CreateImage("Icon", edgeRect, null, Color.white, Image.Type.Simple);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.enabled = false;
            Stretch(icon.rectTransform, 8f);

            var letter = ProceduralUIFactory.CreateText("Letter", edgeRect, font, 26, FontStyle.Bold, new Color(0.9f, 0.92f, 0.96f, 1f), TextAnchor.MiddleCenter);
            letter.raycastTarget = false;
            Stretch(letter.rectTransform, 0f);

            // Radial sweep over the icon: cooldown between volleys, reload in a warm colour.
            var sweep = ProceduralUIFactory.CreateImage("Sweep", edgeRect, _squareSprite, SweepColor, Image.Type.Filled);
            sweep.raycastTarget = false;
            sweep.fillMethod = Image.FillMethod.Radial360;
            sweep.fillOrigin = (int)Image.Origin360.Top;
            sweep.fillClockwise = false;
            sweep.fillAmount = 0f;
            Stretch(sweep.rectTransform, 3f);

            var ammoBar = ProceduralUIFactory.CreateImage("AmmoBar" + index, _panel, _squareSprite, new Color(0.12f, 0.13f, 0.17f, 0.9f), Image.Type.Simple);
            ammoBar.raycastTarget = false;
            var ammoRect = ammoBar.rectTransform;
            ammoRect.anchorMin = Vector2.zero;
            ammoRect.anchorMax = Vector2.zero;
            ammoRect.pivot = Vector2.zero;
            ammoRect.anchoredPosition = new Vector2(x, PanelPadding);
            ammoRect.sizeDelta = new Vector2(TileSize, AmmoBarHeight);

            var ammoFill = ProceduralUIFactory.CreateImage("AmmoFill", ammoRect, _squareSprite, AmmoColor, Image.Type.Simple);
            ammoFill.raycastTarget = false;
            ammoFill.rectTransform.anchorMin = Vector2.zero;
            ammoFill.rectTransform.anchorMax = Vector2.one;
            ammoFill.rectTransform.offsetMin = Vector2.zero;
            ammoFill.rectTransform.offsetMax = Vector2.zero;

            return new TileView(edge, background, icon, letter, sweep, ammoRect, ammoFill);
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private sealed class TileView
        {
            public TileView(Image edge, Image background, Image icon, Text letter, Image sweep, RectTransform ammoBar, Image ammoFill)
            {
                Edge = edge;
                Background = background;
                Icon = icon;
                Letter = letter;
                Sweep = sweep;
                AmmoBar = ammoBar;
                AmmoFill = ammoFill;
            }

            public Image Edge { get; }

            public Image Background { get; }

            public Image Icon { get; }

            public Text Letter { get; }

            public Image Sweep { get; }

            public RectTransform AmmoBar { get; }

            public Image AmmoFill { get; }
        }
    }
}
