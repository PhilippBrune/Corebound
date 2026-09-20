using System.Text;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.Gameplay
{
    // Weapons.md, UI: "aktuelle Weapon-Identitaet", "relevante Bereitschafts- oder Cooldown-
    // Zustaende", "aktive Weapon-Modifikatoren". Before WeaponEvents existed these three had no
    // data source at all, which is why this presenter is new rather than an addition to
    // WaveStatePresenter - that one is already the largest presenter in the project and is listed
    // as a refactor candidate in Docs/10_OPEN_POINTS.md.
    public sealed class WeaponHudPresenter : MonoBehaviour
    {
        private const string CanvasName = "WeaponHudCanvas";
        private const float ReadyPulseSeconds = 0.12f;

        [SerializeField] private PlayerCombatController _playerCombatController;

        private Canvas _canvas;
        private RectTransform _canvasRoot;
        private RectTransform _panel;
        private Image _iconImage;
        private Text _weaponNameText;
        private Text _modifierText;
        private RectTransform _cooldownBarBg;
        private RectTransform _cooldownBarFill;
        private Image _cooldownBarFillImage;

        private Font _font;
        private UIManagedPanel _managedPanel;
        private readonly StringBuilder _modifierBuilder = new StringBuilder(96);

        private int _renderedModifierVersion = -1;
        private string _renderedWeaponId = null;
        private float _readyPulseTimer;

        private void Awake()
        {
            _font = UIFontResolver.Resolve();
            ResolveCombatController();
            BuildCanvas();
            _managedPanel = UIManager.RegisterPanel(_canvasRoot, "weapon_hud", UIPanelLayer.HUD, false, false);
            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            WeaponEvents.WeaponModified += OnWeaponModified;
            WeaponEvents.WeaponStateChanged += OnWeaponStateChanged;
            RefreshIdentity();
        }

        private void OnDisable()
        {
            WeaponEvents.WeaponModified -= OnWeaponModified;
            WeaponEvents.WeaponStateChanged -= OnWeaponStateChanged;
        }

        private void OnDestroy()
        {
            UIManager.UnregisterPanel(_managedPanel);
        }

        private void Update()
        {
            if (_playerCombatController == null)
            {
                ResolveCombatController();
                return;
            }

            RefreshIdentity();
            RefreshCooldown();
        }

        private void OnWeaponModified(WeaponModifiedEventArgs eventArgs)
        {
            RefreshModifierText(eventArgs.ModifierState);
        }

        private void OnWeaponStateChanged(WeaponStateChangedEventArgs eventArgs)
        {
            SetPanelVisible(eventArgs.CurrentState != WeaponReadinessState.Unavailable);

            // A weapon at a 0.12s interval spends almost every frame Cooling, so a bar alone reads
            // as static. The brief tint on the Cooling -> Ready edge is what actually communicates
            // "you can fire again" at that rhythm.
            if (eventArgs.PreviousState == WeaponReadinessState.Cooling && eventArgs.CurrentState == WeaponReadinessState.Ready)
            {
                _readyPulseTimer = ReadyPulseSeconds;
            }
        }

        private void ResolveCombatController()
        {
            if (_playerCombatController != null)
            {
                return;
            }

            _playerCombatController = FindAnyObjectByType<PlayerCombatController>();
        }

        private void RefreshIdentity()
        {
            if (_playerCombatController == null)
            {
                return;
            }

            var weaponId = _playerCombatController.ActiveWeaponId;

            if (string.Equals(weaponId, _renderedWeaponId, System.StringComparison.Ordinal))
            {
                return;
            }

            _renderedWeaponId = weaponId;

            var weapon = _playerCombatController.ActiveWeapon;
            var presentation = weapon != null ? weapon.Presentation : null;

            if (_weaponNameText != null)
            {
                _weaponNameText.text = weapon != null
                    ? weapon.DisplayName
                    : LocalizationService.Resolve("No Weapon", "Keine Waffe");
                _weaponNameText.color = presentation != null ? presentation.AccentColor : new Color(1f, 0.82f, 0.45f, 1f);
            }

            if (_iconImage != null)
            {
                var iconSprite = presentation != null ? presentation.ResolveIconSprite() : null;
                _iconImage.sprite = iconSprite;
                _iconImage.color = presentation != null
                    ? presentation.AccentColor
                    : new Color(0.55f, 0.62f, 0.75f, 0.9f);
                _iconImage.enabled = iconSprite != null;
            }

            if (_cooldownBarFillImage != null && presentation != null)
            {
                _cooldownBarFillImage.color = presentation.AccentColor;
            }

            SetPanelVisible(weapon != null);

            // A weapon swap changes which modifiers pass AppliesTo, so the list has to be redrawn
            // even when nothing about the build itself changed.
            _renderedModifierVersion = -1;
            RefreshModifierText(_playerCombatController.WeaponModifierState);
        }

        private void RefreshCooldown()
        {
            if (_cooldownBarFill == null || _cooldownBarBg == null)
            {
                return;
            }

            // The bar fills as the weapon recovers, so a full bar means ready - the opposite
            // reading (bar drains to nothing) looks like the weapon ran out of something.
            var readiness = 1f - Mathf.Clamp01(_playerCombatController.FireCooldownNormalized);
            _cooldownBarFill.sizeDelta = new Vector2(_cooldownBarBg.rect.width * readiness, _cooldownBarFill.sizeDelta.y);

            if (_readyPulseTimer > 0f)
            {
                _readyPulseTimer -= Time.deltaTime;
            }

            if (_cooldownBarFillImage == null)
            {
                return;
            }

            var baseColor = _cooldownBarFillImage.color;
            var alpha = _readyPulseTimer > 0f ? 1f : 0.72f;
            _cooldownBarFillImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        private void RefreshModifierText(WeaponModifierState modifierState)
        {
            if (_modifierText == null || modifierState == null)
            {
                return;
            }

            if (modifierState.Version == _renderedModifierVersion)
            {
                return;
            }

            _renderedModifierVersion = modifierState.Version;
            _modifierBuilder.Length = 0;

            if (!modifierState.HasActiveModifiers)
            {
                _modifierText.text = LocalizationService.Resolve("No modifiers", "Keine Modifikatoren");
                return;
            }

            var modifiers = modifierState.ActiveModifiers;

            for (var index = 0; index < modifiers.Count; index++)
            {
                if (_modifierBuilder.Length > 0)
                {
                    _modifierBuilder.Append("  ");
                }

                _modifierBuilder.Append(FormatModifier(modifiers[index]));
            }

            _modifierText.text = _modifierBuilder.ToString();
        }

        // A multiplicative 1.15 reads as "+15%"; an additive 0.05 crit chance reads as "+5%"; an
        // additive projectile count reads as "+1". Driven off the enum's own IsMultiplicative so
        // the label can never disagree with how WeaponModifierState actually composed the value.
        private static string FormatModifier(WeaponModifierData modifier)
        {
            if (modifier == null)
            {
                return string.Empty;
            }

            var label = !string.IsNullOrWhiteSpace(modifier.DisplayName) ? modifier.DisplayName : modifier.ModifierId;

            if (modifier.Target == WeaponModifierTarget.AdditionalProjectiles)
            {
                return label + " +" + Mathf.RoundToInt(modifier.Value);
            }

            var percent = modifier.Target.IsMultiplicative()
                ? (modifier.Value - 1f) * 100f
                : modifier.Value * 100f;

            return label + " " + (percent >= 0f ? "+" : string.Empty) + percent.ToString("0") + "%";
        }

        private void SetPanelVisible(bool visible)
        {
            if (_panel != null && _panel.gameObject.activeSelf != visible)
            {
                _panel.gameObject.SetActive(visible);
            }
        }

        // Same shape as BossHudPresenter.BuildCanvas, and for the same reason: this presenter can
        // share CombatRuntime with world-space services, so adding a Canvas to gameObject itself
        // would convert that Transform into a CanvasScaler-driven RectTransform and displace every
        // sibling's world-space children. Find-existing-first guards against a duplicate Awake.
        private void BuildCanvas()
        {
            var existingCanvasTransform = transform.Find(CanvasName);
            var canvasObject = existingCanvasTransform != null ? existingCanvasTransform.gameObject : new GameObject(CanvasName);

            if (existingCanvasTransform == null)
            {
                canvasObject.transform.SetParent(transform, false);
            }

            _canvas = canvasObject.GetComponent<Canvas>();

            if (_canvas == null)
            {
                _canvas = canvasObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();

            if (scaler == null)
            {
                scaler = canvasObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            _canvasRoot = canvasObject.GetComponent<RectTransform>();
            _canvasRoot.anchorMin = Vector2.zero;
            _canvasRoot.anchorMax = Vector2.one;
            _canvasRoot.offsetMin = Vector2.zero;
            _canvasRoot.offsetMax = Vector2.zero;

            var existingPanel = _canvasRoot.Find("WeaponPanel");
            var panelObject = existingPanel != null ? existingPanel.gameObject : new GameObject("WeaponPanel", typeof(RectTransform), typeof(Image));
            _panel = panelObject.GetComponent<RectTransform>();
            _panel.SetParent(_canvasRoot, false);

            // Bottom-left: the boss bar owns bottom-centre and the status box owns top-right, so
            // this is the only free HUD corner that does not overlap an existing widget.
            _panel.anchorMin = Vector2.zero;
            _panel.anchorMax = Vector2.zero;
            _panel.pivot = Vector2.zero;
            _panel.anchoredPosition = new Vector2(26f, 26f);
            _panel.sizeDelta = new Vector2(360f, 96f);

            var panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.07f, 0.08f, 0.12f, 0.8f);
            panelImage.raycastTarget = false;

            var existingIcon = _panel.Find("WeaponIcon");
            var iconObject = existingIcon != null ? existingIcon.gameObject : new GameObject("WeaponIcon", typeof(RectTransform), typeof(Image));
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(_panel, false);
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(12f, -12f);
            iconRect.sizeDelta = new Vector2(34f, 34f);
            _iconImage = iconObject.GetComponent<Image>();
            _iconImage.raycastTarget = false;
            _iconImage.preserveAspect = true;
            _iconImage.enabled = false;

            _weaponNameText = ProceduralUIFactory.CreateText("WeaponNameText", _panel, _font, 22, FontStyle.Bold, new Color(1f, 0.82f, 0.45f, 1f), TextAnchor.MiddleLeft);
            _weaponNameText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _weaponNameText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _weaponNameText.rectTransform.pivot = new Vector2(0f, 1f);
            _weaponNameText.rectTransform.anchoredPosition = new Vector2(54f, -12f);
            _weaponNameText.rectTransform.sizeDelta = new Vector2(-66f, 34f);
            _weaponNameText.text = LocalizationService.Resolve("No Weapon", "Keine Waffe");

            var existingBarBg = _panel.Find("CooldownBarBg");
            var barBgObject = existingBarBg != null ? existingBarBg.gameObject : new GameObject("CooldownBarBg", typeof(RectTransform), typeof(Image));
            _cooldownBarBg = barBgObject.GetComponent<RectTransform>();
            _cooldownBarBg.SetParent(_panel, false);
            _cooldownBarBg.anchorMin = new Vector2(0f, 1f);
            _cooldownBarBg.anchorMax = new Vector2(1f, 1f);
            _cooldownBarBg.pivot = new Vector2(0f, 1f);
            _cooldownBarBg.anchoredPosition = new Vector2(12f, -52f);
            _cooldownBarBg.sizeDelta = new Vector2(-24f, 10f);

            var barBgImage = barBgObject.GetComponent<Image>();
            barBgImage.color = new Color(0.16f, 0.17f, 0.22f, 0.9f);
            barBgImage.raycastTarget = false;

            var existingBarFill = _cooldownBarBg.Find("CooldownBarFill");
            var barFillObject = existingBarFill != null ? existingBarFill.gameObject : new GameObject("CooldownBarFill", typeof(RectTransform), typeof(Image));
            _cooldownBarFill = barFillObject.GetComponent<RectTransform>();
            _cooldownBarFill.SetParent(_cooldownBarBg, false);
            _cooldownBarFill.anchorMin = new Vector2(0f, 0f);
            _cooldownBarFill.anchorMax = new Vector2(0f, 1f);
            _cooldownBarFill.pivot = new Vector2(0f, 0.5f);
            _cooldownBarFill.anchoredPosition = Vector2.zero;
            _cooldownBarFill.sizeDelta = new Vector2(_cooldownBarBg.rect.width, 0f);

            _cooldownBarFillImage = barFillObject.GetComponent<Image>();
            _cooldownBarFillImage.color = new Color(1f, 0.72f, 0.35f, 0.72f);
            _cooldownBarFillImage.raycastTarget = false;

            _modifierText = ProceduralUIFactory.CreateText("WeaponModifierText", _panel, _font, 15, FontStyle.Normal, new Color(0.74f, 0.82f, 0.95f, 1f), TextAnchor.UpperLeft);
            _modifierText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _modifierText.rectTransform.anchorMax = new Vector2(1f, 0f);
            _modifierText.rectTransform.pivot = new Vector2(0f, 0f);
            _modifierText.rectTransform.anchoredPosition = new Vector2(12f, 8f);
            _modifierText.rectTransform.sizeDelta = new Vector2(-24f, 28f);
            _modifierText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _modifierText.text = LocalizationService.Resolve("No modifiers", "Keine Modifikatoren");
        }
    }
}
