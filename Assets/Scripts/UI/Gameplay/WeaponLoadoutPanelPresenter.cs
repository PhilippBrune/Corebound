using System.Collections.Generic;
using System.Text;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Waves;
using Corebound.Gameplay.Weapons;
using Corebound.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.Gameplay
{
    // WeaponLoadout.md, UI in Intermission: six slots navigable with the d-pad, each with name,
    // archetype and tier; the active Set Bonus per archetype in plain words with the next tier's
    // requirement; sell and rearrange; the projectile budget as used-of-total.
    //
    // Controller-first with nothing but Navigate / Confirm / Cancel: Left/Right picks a slot,
    // Up/Down picks an action, Confirm runs it. While open the input context is UI + System only,
    // so the player neither walks nor fires and Build's Confirm cannot also end the Intermission.
    //
    // Opened with the Loadout action (Select / I) during an Intermission, and automatically when a
    // weapon arrived with every slot full, since that weapon waits for a decision.
    public sealed class WeaponLoadoutPanelPresenter : MonoBehaviour
    {
        private const float PanelWidth = 1180f;
        private const float PanelHeight = 560f;
        private const float SlotWidth = 170f;
        private const float SlotHeight = 150f;
        private const float SlotGap = 14f;

        private static readonly Color PanelColor = new Color(0.05f, 0.06f, 0.09f, 0.94f);
        private static readonly Color SlotColor = new Color(0.14f, 0.16f, 0.21f, 1f);
        private static readonly Color SlotSelectedColor = new Color(0.32f, 0.26f, 0.12f, 1f);
        private static readonly Color SlotLockedColor = new Color(0.09f, 0.1f, 0.12f, 1f);
        private static readonly Color TextColor = new Color(0.92f, 0.94f, 0.97f, 1f);
        private static readonly Color DimTextColor = new Color(0.6f, 0.64f, 0.7f, 1f);
        private static readonly Color AccentColor = new Color(1f, 0.78f, 0.35f, 1f);
        private static readonly Color DisabledColor = new Color(0.45f, 0.47f, 0.52f, 1f);

        // The pause presenter asks this to avoid opening on the same Escape press that closed us
        // (UI Cancel and System Pause share Escape - see the Escape dual-binding gotcha).
        public static bool IsOpen { get; private set; }

        public static int LastToggleFrame { get; private set; } = -1;

        private enum PanelAction
        {
            Upgrade,
            Sell,
            MoveLeft,
            MoveRight,
            PlacePending,
            DeclinePending,
            Close
        }

        private readonly List<PanelAction> _actions = new List<PanelAction>();
        private readonly List<Text> _actionTexts = new List<Text>();
        private readonly Image[] _slotImages = new Image[WeaponLoadoutRuntime.Capacity];
        private readonly Text[] _slotTexts = new Text[WeaponLoadoutRuntime.Capacity];
        private readonly StringBuilder _builder = new StringBuilder(512);

        private PlayerCombatController _player;
        private CoreboundInputReader _inputReader;
        private WaveManager _waveManager;
        private InputContextData _panelContext;
        private InputContextData _gameplayContext;
        private GameObject _canvasObject;
        private CanvasGroup _canvasGroup;
        private Text _titleText;
        private Text _detailText;
        private Text _footerText;
        private RectTransform _actionRoot;
        private Font _font;
        private Sprite _square;
        private int _selectedSlot;
        private int _selectedAction;
        private int _renderedVersion = -1;
        private bool _isOpen;

        public void Bind(PlayerCombatController player)
        {
            _player = player;
        }

        private void Start()
        {
            if (_player == null)
            {
                _player = GetComponent<PlayerCombatController>();
            }

            _waveManager = FindFirstObjectByType<WaveManager>();
            BindInputReader();
            _panelContext = InputContextData.CreateRuntime(false, false, true, true);
            _gameplayContext = InputContextData.CreateRuntime(true, true, false, true);
        }

        private void OnDestroy()
        {
            UnbindInputReader();
            IsOpen = false;

            if (_canvasObject != null)
            {
                Destroy(_canvasObject);
            }
        }

        // MainMenu.unity and SampleScene.unity each carry a reader; bind the one in the player's
        // scene, the same rule the autopilot had to learn.
        private void BindInputReader()
        {
            var readers = FindObjectsByType<CoreboundInputReader>(FindObjectsSortMode.None);

            for (var index = 0; index < readers.Length; index++)
            {
                if (readers[index] != null && readers[index].gameObject.scene == gameObject.scene)
                {
                    _inputReader = readers[index];
                    break;
                }
            }

            if (_inputReader == null && readers.Length > 0)
            {
                _inputReader = readers[0];
            }

            if (_inputReader == null)
            {
                return;
            }

            _inputReader.LoadoutPressed += OnLoadoutPressed;
            _inputReader.NavigatePerformed += OnNavigate;
            _inputReader.ConfirmPressed += OnConfirm;
            _inputReader.CancelPressed += OnCancel;
        }

        private void UnbindInputReader()
        {
            if (_inputReader == null)
            {
                return;
            }

            _inputReader.LoadoutPressed -= OnLoadoutPressed;
            _inputReader.NavigatePerformed -= OnNavigate;
            _inputReader.ConfirmPressed -= OnConfirm;
            _inputReader.CancelPressed -= OnCancel;
        }

        private void Update()
        {
            if (_player == null || _waveManager == null)
            {
                return;
            }

            var canOpen = CanOpenNow();

            if (_isOpen && !canOpen)
            {
                Close();
                return;
            }

            // A weapon waiting for a slot opens the panel by itself at the first chance.
            if (!_isOpen && canOpen && _player.Loadout.HasPendingWeapon && LastToggleFrame != Time.frameCount)
            {
                Open();
            }

            if (_isOpen && _player.Loadout.Version != _renderedVersion)
            {
                Refresh();
            }

            if (_isOpen)
            {
                UpdateMouse();
            }
        }

        // Playtest 2026-09-27: "replace interaction is not working with mouse click." The panel was
        // controller/keyboard only - it never read the mouse at all, so the Replace row could be
        // read but not pressed. The gameplay scene carries no EventSystem (the HUD canvases are
        // additive and none of them has a raycaster), so pointer input is hit-tested by hand here,
        // the same way CardRewardPresenter and MainMenuRoutePanelPresenter already do it.
        //
        // Hover selects and click confirms, matching the reward screen, so one mental model covers
        // both. Clicking a weapon slot selects that slot rather than firing an action, because the
        // destructive actions (Sell, Replace) must stay a deliberate second click.
        private Vector2 _lastPointerPosition;

        private void UpdateMouse()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;

            if (mouse == null)
            {
                return;
            }

            var pointer = mouse.position.ReadValue();
            var moved = (pointer - _lastPointerPosition).sqrMagnitude > 4f;
            _lastPointerPosition = pointer;
            var clicked = mouse.leftButton.wasPressedThisFrame;

            if (!moved && !clicked)
            {
                return;
            }

            for (var index = 0; index < _slotImages.Length; index++)
            {
                var slotImage = _slotImages[index];

                if (slotImage == null
                    || !slotImage.gameObject.activeInHierarchy
                    || index >= _player.Loadout.UnlockedSlotCount
                    || !RectTransformUtility.RectangleContainsScreenPoint(slotImage.rectTransform, pointer, null))
                {
                    continue;
                }

                if (index != _selectedSlot)
                {
                    _selectedSlot = index;
                    _selectedAction = 0;
                    Refresh();
                }

                return;
            }

            for (var index = 0; index < _actionTexts.Count && index < _actions.Count; index++)
            {
                var actionText = _actionTexts[index];

                if (actionText == null
                    || !actionText.gameObject.activeInHierarchy
                    || !RectTransformUtility.RectangleContainsScreenPoint(actionText.rectTransform, pointer, null))
                {
                    continue;
                }

                if (index != _selectedAction)
                {
                    _selectedAction = index;
                    RefreshActionHighlight();
                }

                if (clicked)
                {
                    OnConfirm();
                }

                return;
            }
        }

        private bool CanOpenNow()
        {
            if (_waveManager.State != WaveState.Intermission || Time.timeScale <= 0f)
            {
                return false;
            }

            var shop = FindFirstObjectByType<Corebound.Gameplay.Shop.ShopService>();
            return shop == null || !shop.IsOpen;
        }

        private void OnLoadoutPressed()
        {
            if (LastToggleFrame == Time.frameCount)
            {
                return;
            }

            if (_isOpen)
            {
                Close();
            }
            else if (_player != null && _waveManager != null && CanOpenNow())
            {
                Open();
            }
        }

        private void Open()
        {
            EnsureBuilt();
            _isOpen = true;
            IsOpen = true;
            LastToggleFrame = Time.frameCount;
            _selectedSlot = Mathf.Clamp(_selectedSlot, 0, Mathf.Max(0, _player.Loadout.UnlockedSlotCount - 1));
            _selectedAction = 0;
            _canvasGroup.alpha = 1f;
            _inputReader?.ApplyContext(_panelContext);
            _inputReader?.ResetInputState();
            Refresh();
        }

        private void Close()
        {
            _isOpen = false;
            IsOpen = false;
            LastToggleFrame = Time.frameCount;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }

            _inputReader?.ApplyContext(_gameplayContext);
            _inputReader?.ResetInputState();
        }

        private void OnNavigate(Vector2 input)
        {
            if (!_isOpen || LastToggleFrame == Time.frameCount)
            {
                return;
            }

            if (Mathf.Abs(input.x) >= 0.5f && Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                var count = Mathf.Max(1, _player.Loadout.UnlockedSlotCount);
                _selectedSlot = (_selectedSlot + (input.x > 0f ? 1 : -1) + count) % count;
                _selectedAction = 0;
                Refresh();
            }
            else if (Mathf.Abs(input.y) >= 0.5f && _actions.Count > 0)
            {
                _selectedAction = (_selectedAction + (input.y > 0f ? -1 : 1) + _actions.Count) % _actions.Count;
                RefreshActionHighlight();
            }
        }

        private void OnConfirm()
        {
            // Same-frame guard: the Confirm that opened something must not also run an action.
            if (!_isOpen || LastToggleFrame == Time.frameCount || _selectedAction < 0 || _selectedAction >= _actions.Count)
            {
                return;
            }

            switch (_actions[_selectedAction])
            {
                case PanelAction.Upgrade:
                    _player.TryUpgradeSlot(_selectedSlot);
                    break;
                case PanelAction.Sell:
                    _player.TrySellSlot(_selectedSlot);
                    break;
                case PanelAction.MoveLeft:
                    _player.SwapSlots(_selectedSlot, _selectedSlot - 1);
                    _selectedSlot = Mathf.Max(0, _selectedSlot - 1);
                    break;
                case PanelAction.MoveRight:
                    _player.SwapSlots(_selectedSlot, _selectedSlot + 1);
                    _selectedSlot = Mathf.Min(_player.Loadout.UnlockedSlotCount - 1, _selectedSlot + 1);
                    break;
                case PanelAction.PlacePending:
                    _player.TryPlacePendingWeapon(_selectedSlot);
                    break;
                case PanelAction.DeclinePending:
                    _player.DeclinePendingWeapon();
                    break;
                case PanelAction.Close:
                    Close();
                    return;
            }

            Refresh();
        }

        private void OnCancel()
        {
            if (!_isOpen || LastToggleFrame == Time.frameCount)
            {
                return;
            }

            // Cancel with a weapon pending is "decline", which the spec requires to always be
            // possible - and it must be deliberate, so it takes the second press.
            if (_player.Loadout.HasPendingWeapon)
            {
                _selectedAction = Mathf.Max(0, _actions.IndexOf(PanelAction.DeclinePending));
                RefreshActionHighlight();
                return;
            }

            Close();
        }

        private void Refresh()
        {
            if (_canvasObject == null)
            {
                return;
            }

            var loadout = _player.Loadout;
            _renderedVersion = loadout.Version;

            for (var index = 0; index < _slotImages.Length; index++)
            {
                var slot = loadout.GetSlot(index);
                var locked = index >= loadout.UnlockedSlotCount;
                _slotImages[index].color = locked ? SlotLockedColor : (index == _selectedSlot ? SlotSelectedColor : SlotColor);
                _slotTexts[index].color = locked ? DisabledColor : TextColor;

                if (locked)
                {
                    _slotTexts[index].text = LocalizationService.Resolve("Locked", "Gesperrt");
                }
                else if (slot.IsEmpty)
                {
                    _slotTexts[index].text = LocalizationService.Resolve("Empty", "Leer");
                }
                else
                {
                    _slotTexts[index].text = slot.Weapon.DisplayName + "\n<size=15>" + ResolveTierName(slot.Tier) + "</size>";
                }
            }

            RebuildActions(loadout);
            _titleText.text = loadout.HasPendingWeapon
                ? LocalizationService.Resolve("New weapon: ", "Neue Waffe: ") + loadout.PendingWeapon.DisplayName + LocalizationService.Resolve(" - choose a slot or decline", " - wähle einen Platz oder lehne ab")
                : LocalizationService.Resolve("Loadout", "Ausrüstung");
            _detailText.text = BuildDetailText(loadout);
            _footerText.text = LocalizationService.Resolve(
                "Left/Right: slot    Up/Down: action    Confirm: do it    Cancel: close",
                "Links/Rechts: Platz    Hoch/Runter: Aktion    Bestätigen: ausführen    Abbrechen: schließen");
            RefreshActionHighlight();
        }

        private void RebuildActions(WeaponLoadoutRuntime loadout)
        {
            _actions.Clear();
            var slot = loadout.GetSlot(_selectedSlot);

            if (loadout.HasPendingWeapon)
            {
                _actions.Add(PanelAction.PlacePending);
                _actions.Add(PanelAction.DeclinePending);
            }
            else
            {
                if (slot != null && !slot.IsEmpty)
                {
                    _actions.Add(PanelAction.Upgrade);
                    _actions.Add(PanelAction.Sell);
                }

                if (_selectedSlot > 0)
                {
                    _actions.Add(PanelAction.MoveLeft);
                }

                if (_selectedSlot < loadout.UnlockedSlotCount - 1)
                {
                    _actions.Add(PanelAction.MoveRight);
                }

                _actions.Add(PanelAction.Close);
            }

            _selectedAction = Mathf.Clamp(_selectedAction, 0, _actions.Count - 1);

            while (_actionTexts.Count < _actions.Count)
            {
                var text = ProceduralUIFactory.CreateText("Action" + _actionTexts.Count, _actionRoot, _font, 20, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
                text.rectTransform.anchorMin = new Vector2(0f, 1f);
                text.rectTransform.anchorMax = new Vector2(1f, 1f);
                text.rectTransform.pivot = new Vector2(0f, 1f);
                text.rectTransform.sizeDelta = new Vector2(0f, 30f);
                text.rectTransform.anchoredPosition = new Vector2(0f, -_actionTexts.Count * 32f);
                _actionTexts.Add(text);
            }

            for (var index = 0; index < _actionTexts.Count; index++)
            {
                _actionTexts[index].gameObject.SetActive(index < _actions.Count);

                if (index < _actions.Count)
                {
                    _actionTexts[index].text = DescribeAction(_actions[index], slot, loadout);
                }
            }
        }

        private string DescribeAction(PanelAction action, WeaponSlotRuntime slot, WeaponLoadoutRuntime loadout)
        {
            switch (action)
            {
                case PanelAction.Upgrade:
                    return slot.Tier >= WeaponTierRules.MaxTier
                        ? LocalizationService.Resolve("Upgrade - already Legendary", "Verbessern - bereits Legendär")
                        : LocalizationService.Resolve("Upgrade to ", "Verbessern zu ") + ResolveTierName(slot.Tier + 1) + "  (" + _player.ResolveTierUpgradeCost(_selectedSlot) + LocalizationService.Resolve(" Scrap)", " Schrott)");
                case PanelAction.Sell:
                    return loadout.FilledSlotCount <= 1
                        ? LocalizationService.Resolve("Sell - your last weapon cannot be sold", "Verkaufen - die letzte Waffe kann nicht verkauft werden")
                        : LocalizationService.Resolve("Sell for ", "Verkaufen für ") + _player.ResolveSellValue(_selectedSlot) + LocalizationService.Resolve(" Scrap", " Schrott");
                case PanelAction.MoveLeft:
                    return LocalizationService.Resolve("Move left", "Nach links");
                case PanelAction.MoveRight:
                    return LocalizationService.Resolve("Move right", "Nach rechts");
                case PanelAction.PlacePending:
                    return slot != null && !slot.IsEmpty
                        ? LocalizationService.Resolve("Replace ", "Ersetze ") + slot.Weapon.DisplayName + LocalizationService.Resolve(" (sold for ", " (verkauft für ") + _player.ResolveSellValue(_selectedSlot) + ")"
                        : LocalizationService.Resolve("Place here", "Hier einsetzen");
                case PanelAction.DeclinePending:
                    return loadout.PendingRefund > 0
                        ? LocalizationService.Resolve("Decline (refund ", "Ablehnen (Erstattung ") + loadout.PendingRefund + ")"
                        : LocalizationService.Resolve("Decline", "Ablehnen");
                default:
                    return LocalizationService.Resolve("Close", "Schließen");
            }
        }

        private void RefreshActionHighlight()
        {
            for (var index = 0; index < _actionTexts.Count && index < _actions.Count; index++)
            {
                var selected = index == _selectedAction;
                var text = _actionTexts[index].text.TrimStart('>', ' ');
                _actionTexts[index].text = (selected ? "> " : "   ") + text;
                _actionTexts[index].color = selected ? AccentColor : TextColor;
            }
        }

        private string BuildDetailText(WeaponLoadoutRuntime loadout)
        {
            _builder.Clear();
            var slot = loadout.GetSlot(_selectedSlot);

            if (slot != null && !slot.IsEmpty)
            {
                _builder.Append("<b>").Append(slot.Weapon.DisplayName).Append("</b>  ·  ").Append(ResolveTierName(slot.Tier)).Append('\n');
                _builder.Append("<color=#A9B3C2>").Append(slot.Weapon.Summary).Append("</color>\n");
            }

            // Every represented archetype's Set Bonus, current tier and progress to the next.
            var bonuses = loadout.ActiveSetBonuses;

            for (var index = 0; index < bonuses.Count; index++)
            {
                var bonus = bonuses[index];
                _builder.Append('\n').Append(bonus.Weapon.DisplayName).Append(" x").Append(bonus.Copies).Append(": ");

                if (bonus.Tier != null)
                {
                    _builder.Append("<color=#FFC75A>").Append(bonus.Tier.DisplayName).Append("</color> - ").Append(bonus.Tier.Description);
                }
                else
                {
                    _builder.Append(LocalizationService.Resolve("no Set Bonus yet", "noch kein Setbonus"));
                }

                if (bonus.NextTier != null)
                {
                    _builder.Append("  <color=#A9B3C2>(").Append(bonus.Copies).Append('/').Append(bonus.NextTier.RequiredCopies)
                        .Append(LocalizationService.Resolve(" toward ", " bis ")).Append(bonus.NextTier.DisplayName).Append(")</color>");
                }
            }

            if (loadout.IsArsenalActive)
            {
                _builder.Append('\n').Append(LocalizationService.Resolve("<color=#FFC75A>Arsenal</color> - six different weapons: +12% damage", "<color=#FFC75A>Arsenal</color> - sechs verschiedene Waffen: +12 % Schaden"));
            }

            _builder.Append("\n\n").Append(LocalizationService.Resolve("Projectiles in flight: ", "Projektile im Flug: "))
                .Append(_player.LiveProjectileCount).Append(" / ").Append(_player.ProjectileBudget);
            return _builder.ToString();
        }

        private static string ResolveTierName(int tier)
        {
            switch ((CardRarity)Mathf.Clamp(tier, 0, WeaponTierRules.MaxTier))
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

        private void EnsureBuilt()
        {
            if (_canvasObject != null)
            {
                return;
            }

            _font = UIFontResolver.Resolve();
            _square = DebugSpriteFactory.CreateSquare(100f);

            // A scene-root object, not a child of the player: the player rotates to aim, and the
            // shared-GameObject Canvas gotcha rules out a Canvas on a gameplay object itself.
            _canvasObject = new GameObject("WeaponLoadoutPanelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 420;
            var scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = _canvasObject.GetComponent<RectTransform>();
            var panel = ProceduralUIFactory.CreateImage("LoadoutPanel", root, _square, PanelColor, Image.Type.Simple);
            panel.raycastTarget = false;
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            _canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;

            _titleText = CreateText("Title", panelRect, 30, TextAnchor.UpperLeft, AccentColor);
            Place(_titleText.rectTransform, 30f, -24f, PanelWidth - 60f, 44f);

            var slotsWidth = WeaponLoadoutRuntime.Capacity * SlotWidth + (WeaponLoadoutRuntime.Capacity - 1) * SlotGap;
            var startX = (PanelWidth - slotsWidth) * 0.5f;

            for (var index = 0; index < WeaponLoadoutRuntime.Capacity; index++)
            {
                var slotImage = ProceduralUIFactory.CreateImage("Slot" + index, panelRect, _square, SlotColor, Image.Type.Simple);
                slotImage.raycastTarget = false;
                Place(slotImage.rectTransform, startX + index * (SlotWidth + SlotGap), -84f, SlotWidth, SlotHeight);
                _slotImages[index] = slotImage;

                var slotText = CreateText("SlotText", slotImage.rectTransform, 19, TextAnchor.MiddleCenter, TextColor);
                slotText.supportRichText = true;
                slotText.rectTransform.anchorMin = Vector2.zero;
                slotText.rectTransform.anchorMax = Vector2.one;
                slotText.rectTransform.offsetMin = new Vector2(8f, 8f);
                slotText.rectTransform.offsetMax = new Vector2(-8f, -8f);
                _slotTexts[index] = slotText;
            }

            _detailText = CreateText("Detail", panelRect, 17, TextAnchor.UpperLeft, TextColor);
            _detailText.supportRichText = true;
            Place(_detailText.rectTransform, 30f, -254f, 720f, 250f);

            var actionObject = new GameObject("Actions", typeof(RectTransform));
            _actionRoot = actionObject.GetComponent<RectTransform>();
            _actionRoot.SetParent(panelRect, false);
            Place(_actionRoot, 790f, -254f, 360f, 230f);

            _footerText = CreateText("Footer", panelRect, 15, TextAnchor.LowerLeft, DimTextColor);
            Place(_footerText.rectTransform, 30f, -(PanelHeight - 44f), PanelWidth - 60f, 28f);

            _canvasGroup.alpha = 0f;
        }

        private Text CreateText(string name, RectTransform parent, int size, TextAnchor anchor, Color color)
        {
            var text = ProceduralUIFactory.CreateText(name, parent, _font, size, FontStyle.Normal, color, anchor);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        // Top-left based placement inside the panel.
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
