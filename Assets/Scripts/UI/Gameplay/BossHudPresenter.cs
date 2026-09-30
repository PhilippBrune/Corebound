using Corebound.Gameplay.Bosses;
using Corebound.Gameplay.Shared;
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

        private float _noticeTimer;
        private string _noticeMessage = string.Empty;
        private BossNoticeKind _noticeKind = BossNoticeKind.None;
        private bool _isBossActive;
        private UIManagedPanel _managedPanel;
        private RectTransform _canvasRoot;

        public string WarningMessage => _noticeTimer > 0f ? _noticeMessage : string.Empty;

        private void Awake()
        {
            TryBind();
        }

        private void OnEnable()
        {
            AdditiveCanvasSceneLoader.CanvasSceneReady += OnCanvasSceneReady;
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
            AdditiveCanvasSceneLoader.CanvasSceneReady -= OnCanvasSceneReady;
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
            // The moment a missing overlay scene actually costs something.
            if (_canvasRoot == null)
            {
                Debug.LogWarning("[BossHud] a boss spawned but OverlayCanvas is not loaded - no boss bar. Is the scene in the build settings?");
            }

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

        // Open Topics 4 (b), 2026-09-30: the layout lives in OverlayCanvas.unity. This only finds
        // the elements - it never sets anchors, positions or sizes (CLAUDE.md, canvas layout is
        // scene-file-driven). Called at Awake when the scene is already open (Editor) and again
        // when it finishes loading (MainMenu -> New Run, where LoadScene completes at frame end).
        private void TryBind()
        {
            if (_canvasRoot != null)
            {
                return;
            }

            var root = AdditiveCanvasSceneLoader.FindRoot(AdditiveCanvasSceneLoader.OverlayCanvasSceneName, "BossHudCanvas");

            // Not an error at Awake: the loader only requests the scene once SampleScene has
            // finished loading, which is after every Awake in it. CanvasSceneReady retries.
            if (root == null)
            {
                return;
            }

            _canvas = root.GetComponent<Canvas>();
            _canvasRoot = root.GetComponent<RectTransform>();
            _bossPanel = _canvasRoot.Find("BossPanel") as RectTransform;
            _bossNameText = FindText(_bossPanel, "BossNameText");
            _phaseText = FindText(_bossPanel, "PhaseText");
            _healthBarBg = _bossPanel != null ? _bossPanel.Find("HealthBarBg") as RectTransform : null;
            _healthBarFill = _healthBarBg != null ? _healthBarBg.Find("HealthBarFill") as RectTransform : null;
            _healthText = FindText(_healthBarBg, "HealthText");
            _warningPanel = _canvasRoot.Find("WarningPanel") as RectTransform;
            _warningText = FindText(_warningPanel, "WarningText");

            RegisterWithUIManager();
            SetBossPanelVisible(_isBossActive);
            SetWarningVisible(_noticeTimer > 0f);

            if (_warningText != null && _noticeTimer > 0f)
            {
                _warningText.text = _noticeMessage;
            }

            Refresh();
        }

        private static Text FindText(Transform parent, string childName)
        {
            var child = parent != null ? parent.Find(childName) : null;
            return child != null ? child.GetComponent<Text>() : null;
        }

        private void OnCanvasSceneReady(string sceneName)
        {
            if (sceneName == AdditiveCanvasSceneLoader.OverlayCanvasSceneName)
            {
                TryBind();
            }
        }

        private void RegisterWithUIManager()
        {
            if (_canvasRoot != null && _managedPanel == null)
            {
                _managedPanel = UIManager.RegisterPanel(_canvasRoot, "boss_hud", UIPanelLayer.HUD, false, false);
            }
        }
    }
}
