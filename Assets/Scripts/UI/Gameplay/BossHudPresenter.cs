using Corebound.Gameplay.Bosses;
using Corebound.Gameplay.Waves;
using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.Gameplay
{
    public sealed class BossHudPresenter : MonoBehaviour
    {
        // Used for internal state comparisons (e.g. "was the last notice the pre-boss warning?")
        // instead of comparing against the localized display string, which would silently break
        // once that string is translated or reworded.
        private enum BossNoticeKind
        {
            None,
            PhaseShift,
            BossDefeated,
            BossIncoming,
        }

        [SerializeField] private WaveManager _waveManager;

        private Canvas _canvas;
        private RectTransform _bossPanel;
        private Text _bossNameText;
        private Text _phaseText;
        private RectTransform _healthBarBg;
        private RectTransform _healthBarFill;
        private Text _healthText;

        private RectTransform _warningPanel;
        private Text _warningText;

        private Font _font;
        private float _noticeTimer;
        private string _noticeMessage = string.Empty;
        private BossNoticeKind _noticeKind = BossNoticeKind.None;
        private bool _isBossActive;
        private UIManagedPanel _managedPanel;
        private RectTransform _canvasRoot;

        public string WarningMessage => _noticeTimer > 0f ? _noticeMessage : string.Empty;

        private void Awake()
        {
            _font = ResolveFont();
            BuildCanvas();
            RegisterWithUIManager();
            SetBossPanelVisible(false);
            SetWarningVisible(false);
        }

        private void OnEnable()
        {
            BossEvents.OnBossSpawned += OnBossSpawned;
            BossEvents.OnBossPhaseChanged += OnBossPhaseChanged;
            BossEvents.OnBossDefeated += OnBossDefeated;
            WaveEvents.IntermissionStarted += OnIntermissionStarted;

            if (_waveManager != null)
            {
                _waveManager.StateChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            BossEvents.OnBossSpawned -= OnBossSpawned;
            BossEvents.OnBossPhaseChanged -= OnBossPhaseChanged;
            BossEvents.OnBossDefeated -= OnBossDefeated;
            WaveEvents.IntermissionStarted -= OnIntermissionStarted;

            if (_waveManager != null)
            {
                _waveManager.StateChanged -= Refresh;
            }
        }

        private void OnDestroy()
        {
            UIManager.UnregisterPanel(_managedPanel);
        }

        private void Update()
        {
            if (_noticeTimer > 0f)
            {
                _noticeTimer -= Time.deltaTime;

                if (_noticeTimer <= 0f)
                {
                    _noticeMessage = string.Empty;
                    SetWarningVisible(false);
                }
            }

            RefreshHealth();
        }

        private void OnBossSpawned(BossEventArgs args)
        {
            _isBossActive = true;
            SetBossPanelVisible(true);

            if (_bossNameText != null)
            {
                _bossNameText.text = args.BossName;
            }

            if (_phaseText != null)
            {
                _phaseText.text = args.PhaseLabel;
            }

            // Clear pre-boss incoming warning upon spawn
            if (_noticeKind == BossNoticeKind.BossIncoming)
            {
                _noticeTimer = 0f;
                _noticeMessage = string.Empty;
                _noticeKind = BossNoticeKind.None;
                SetWarningVisible(false);
            }

            RefreshHealth();
        }

        private void OnBossPhaseChanged(BossEventArgs args)
        {
            if (_phaseText != null)
            {
                _phaseText.text = args.PhaseLabel;
            }

            ShowNotice(BossNoticeKind.PhaseShift, LocalizationService.Resolve("Phase Shift", "Phasenwechsel"), 2.5f);
            RefreshHealth();
        }

        private void OnBossDefeated(BossEventArgs args)
        {
            _isBossActive = false;
            ShowNotice(BossNoticeKind.BossDefeated, LocalizationService.Resolve("Boss Defeated", "Boss besiegt"), 3.0f);
            SetBossPanelVisible(false);
        }

        private void OnIntermissionStarted(WaveProgressEventArgs args)
        {
            var bossIncomingText = LocalizationService.Resolve("Boss Incoming", "Boss erscheint bald");

            // Check if intermission precedes the boss encounter
            if (_waveManager != null && _waveManager.CurrentWaveNumber >= _waveManager.TotalWaveCount)
            {
                ShowNotice(BossNoticeKind.BossIncoming, bossIncomingText, _waveManager.IntermissionSecondsRemaining > 0f ? _waveManager.IntermissionSecondsRemaining : 5f);
            }
            else if (!string.IsNullOrEmpty(args.Label) && args.Label.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ShowNotice(BossNoticeKind.BossIncoming, bossIncomingText, 5.0f);
            }
        }

        private void ShowNotice(BossNoticeKind kind, string message, float duration)
        {
            _noticeKind = kind;
            _noticeMessage = message;
            _noticeTimer = duration;

            if (_warningText != null)
            {
                _warningText.text = message;
            }

            SetWarningVisible(true);
        }

        private void Refresh()
        {
            if (_waveManager == null)
            {
                return;
            }

            if (_waveManager.State == WaveState.BossCombat)
            {
                if (!_isBossActive)
                {
                    _isBossActive = true;
                    SetBossPanelVisible(true);
                }

                if (_bossNameText != null && string.IsNullOrEmpty(_bossNameText.text))
                {
                    _bossNameText.text = _waveManager.BossLabel;
                }

                if (_phaseText != null)
                {
                    _phaseText.text = _waveManager.BossPhaseLabel;
                }
            }
            else if (_waveManager.State != WaveState.BossCombat && _isBossActive)
            {
                _isBossActive = false;
                SetBossPanelVisible(false);
            }

            RefreshHealth();
        }

        private void RefreshHealth()
        {
            if (!_isBossActive && (_waveManager == null || _waveManager.State != WaveState.BossCombat))
            {
                return;
            }

            var healthNormalized = _waveManager != null ? _waveManager.BossHealthNormalized : 1f;
            healthNormalized = Mathf.Clamp01(healthNormalized);

            if (_healthBarFill != null && _healthBarBg != null)
            {
                var bgWidth = _healthBarBg.rect.width;
                _healthBarFill.sizeDelta = new Vector2(bgWidth * healthNormalized, _healthBarFill.sizeDelta.y);
            }

            if (_healthText != null)
            {
                _healthText.text = (healthNormalized * 100f).ToString("0") + "%";
            }

            UIManager.NotifyPanelChanged(_managedPanel);
        }

        private void SetBossPanelVisible(bool visible)
        {
            if (_bossPanel != null)
            {
                _bossPanel.gameObject.SetActive(visible);
            }
        }

        private void SetWarningVisible(bool visible)
        {
            if (_warningPanel != null)
            {
                _warningPanel.gameObject.SetActive(visible);
            }
        }

        private void BuildCanvas()
        {
            // BossHudPresenter shares its GameObject (CombatRuntime) with world-space gameplay
            // services such as TowerService. Adding Canvas/CanvasScaler/RectTransform directly to
            // gameObject converts CombatRuntime's plain Transform into a Canvas-scaled RectTransform,
            // which silently corrupts the world position of any sibling's children parented under
            // that same transform (e.g. TowerService's slot markers ended up scaled/offset by the
            // CanvasScaler and rendered far outside the camera). Build the HUD under its own child
            // GameObject instead, matching NotificationPresenter/TutorialService.
            // Find-existing-first: this presenter is not [ExecuteAlways] (Awake only runs on real
            // instantiation, not editor recompiles) but it also has no authored-scene-layout fallback
            // like ShopPresenter/CardRewardPresenter to bind to instead - if CombatRuntime's Awake
            // ever runs more than once on the same GameObject (e.g. a future singleton bug like the
            // one already found and fixed on LocalProfileSaveManager this session), this rebuild had
            // no guard at all and would silently duplicate the whole BossHudCanvas hierarchy. Cheap
            // insurance against that class of bug, matching the fix already applied to ShopPresenter.
            var existingCanvasTransform = transform.Find("BossHudCanvas");
            var canvasObject = existingCanvasTransform != null ? existingCanvasTransform.gameObject : new GameObject("BossHudCanvas");

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

            var rootRect = canvasObject.GetComponent<RectTransform>();
            _canvasRoot = rootRect;

            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Boss Panel
            var existingBossPanel = rootRect.Find("BossPanel");
            var bossPanelObj = existingBossPanel != null ? existingBossPanel.gameObject : new GameObject("BossPanel", typeof(RectTransform), typeof(Image));
            _bossPanel = bossPanelObj.GetComponent<RectTransform>();
            _bossPanel.SetParent(rootRect, false);
            _bossPanel.anchorMin = new Vector2(0.5f, 0f);
            _bossPanel.anchorMax = new Vector2(0.5f, 0f);
            _bossPanel.pivot = new Vector2(0.5f, 0f);
            _bossPanel.anchoredPosition = new Vector2(0f, 28f);
            _bossPanel.sizeDelta = new Vector2(720f, 75f);

            var panelBg = bossPanelObj.GetComponent<Image>();
            panelBg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
            panelBg.raycastTarget = false;

            _bossNameText = CreateText("BossNameText", _bossPanel, 24, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f, 1f));
            _bossNameText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _bossNameText.rectTransform.anchorMax = new Vector2(0f, 1f);
            _bossNameText.rectTransform.pivot = new Vector2(0f, 1f);
            _bossNameText.rectTransform.anchoredPosition = new Vector2(20f, -8f);
            _bossNameText.rectTransform.sizeDelta = new Vector2(400f, 30f);
            _bossNameText.text = "BOSS";

            _phaseText = CreateText("PhaseText", _bossPanel, 20, TextAnchor.MiddleRight, new Color(0.4f, 0.85f, 1f, 1f));
            _phaseText.rectTransform.anchorMin = new Vector2(1f, 1f);
            _phaseText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _phaseText.rectTransform.pivot = new Vector2(1f, 1f);
            _phaseText.rectTransform.anchoredPosition = new Vector2(-20f, -8f);
            _phaseText.rectTransform.sizeDelta = new Vector2(260f, 30f);
            _phaseText.text = "Phase 1";

            // Health Bar Bg
            var existingHealthBg = _bossPanel.Find("HealthBarBg");
            var healthBgObj = existingHealthBg != null ? existingHealthBg.gameObject : new GameObject("HealthBarBg", typeof(RectTransform), typeof(Image));
            _healthBarBg = healthBgObj.GetComponent<RectTransform>();
            _healthBarBg.SetParent(_bossPanel, false);
            _healthBarBg.anchorMin = new Vector2(0f, 0f);
            _healthBarBg.anchorMax = new Vector2(1f, 0f);
            _healthBarBg.pivot = new Vector2(0.5f, 0f);
            _healthBarBg.anchoredPosition = new Vector2(0f, 12f);
            _healthBarBg.sizeDelta = new Vector2(-40f, 22f);

            var healthBgImg = healthBgObj.GetComponent<Image>();
            healthBgImg.color = new Color(0.2f, 0.05f, 0.05f, 0.9f);
            healthBgImg.raycastTarget = false;

            // Health Bar Fill
            var existingHealthFill = _healthBarBg.Find("HealthBarFill");
            var healthFillObj = existingHealthFill != null ? existingHealthFill.gameObject : new GameObject("HealthBarFill", typeof(RectTransform), typeof(Image));
            _healthBarFill = healthFillObj.GetComponent<RectTransform>();
            _healthBarFill.SetParent(_healthBarBg, false);
            _healthBarFill.anchorMin = new Vector2(0f, 0f);
            _healthBarFill.anchorMax = new Vector2(0f, 1f);
            _healthBarFill.pivot = new Vector2(0f, 0.5f);
            _healthBarFill.anchoredPosition = Vector2.zero;
            _healthBarFill.sizeDelta = new Vector2(_healthBarBg.rect.width, 0f);

            var healthFillImg = healthFillObj.GetComponent<Image>();
            healthFillImg.color = new Color(0.85f, 0.15f, 0.15f, 1f);
            healthFillImg.raycastTarget = false;

            _healthText = CreateText("HealthText", _healthBarBg, 16, TextAnchor.MiddleCenter, Color.white);
            _healthText.rectTransform.anchorMin = Vector2.zero;
            _healthText.rectTransform.anchorMax = Vector2.one;
            _healthText.rectTransform.offsetMin = Vector2.zero;
            _healthText.rectTransform.offsetMax = Vector2.zero;
            _healthText.text = "100%";

            // Warning Panel
            var existingWarningPanel = rootRect.Find("WarningPanel");
            var warningPanelObj = existingWarningPanel != null ? existingWarningPanel.gameObject : new GameObject("WarningPanel", typeof(RectTransform), typeof(Image));
            _warningPanel = warningPanelObj.GetComponent<RectTransform>();
            _warningPanel.SetParent(rootRect, false);
            _warningPanel.anchorMin = new Vector2(0.5f, 0.82f);
            _warningPanel.anchorMax = new Vector2(0.5f, 0.82f);
            _warningPanel.pivot = new Vector2(0.5f, 0.5f);
            _warningPanel.anchoredPosition = Vector2.zero;
            _warningPanel.sizeDelta = new Vector2(650f, 60f);

            var warningBgImg = warningPanelObj.GetComponent<Image>();
            warningBgImg.color = new Color(0.4f, 0.02f, 0.02f, 0.88f);
            warningBgImg.raycastTarget = false;

            _warningText = CreateText("WarningText", _warningPanel, 32, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.2f, 1f));
            _warningText.rectTransform.anchorMin = Vector2.zero;
            _warningText.rectTransform.anchorMax = Vector2.one;
            _warningText.rectTransform.offsetMin = Vector2.zero;
            _warningText.rectTransform.offsetMax = Vector2.zero;
            _warningText.text = "WARNING";
        }

        private Text CreateText(string name, RectTransform parent, int fontSize, TextAnchor anchor, Color color)
        {
            return ProceduralUIFactory.CreateText(name, parent, _font, fontSize, FontStyle.Normal, color, anchor);
        }

        private static Font ResolveFont()
        {
            return UIFontResolver.Resolve();
        }

        private void RegisterWithUIManager()
        {
            _managedPanel = UIManager.RegisterPanel(_canvasRoot, "boss_hud", UIPanelLayer.HUD, false, false);
        }
    }
}
