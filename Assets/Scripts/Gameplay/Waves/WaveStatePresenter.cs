using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Resources;
using Corebound.Gameplay.Shop;
using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Towers;
using Corebound.Input;
using Corebound.Settings;
using Corebound.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Corebound.Gameplay.Waves
{
    // Known debt: this single [ExecuteAlways] class mixes four separable concerns - runtime
    // procedural HUD construction, #if UNITY_EDITOR scene-authoring scaffolding, live data
    // binding/formatting, and pause/scene-transition game-flow control. 2026-09-08: the pure
    // banner-color lookup was extracted into WaveHudColorPalette since it had zero dependency on
    // this presenter's live/procedural/editor state. The remaining three concerns are each wired
    // into _service/_waveManager/scene UI object references, and this is the main gameplay HUD
    // running in the editor too via [ExecuteAlways] - a mis-split breaks visibly in the editor
    // immediately, so the rest needs Editor access to verify before splitting further.
    [ExecuteAlways]
    public sealed class WaveStatePresenter : MonoBehaviour
    {
        // Shrunk from 980x158 per playtest feedback ("HUD with player % etc should be smaller and
        // more decent") - scaled to ~65% and all the internal fixed-pixel margins below scaled by
        // the same factor so the layout stays proportional instead of the margins swallowing a
        // smaller box.
        private const float StatusPanelWidth = 640f;
        private const float StatusPanelHeight = 102f;
        private const float BannerWidth = 760f;
        private const float BannerHeight = 84f;
        private const float BannerDisplaySeconds = 1.85f;
        // The frame art (button_primary_frame.png) has a huge fixed 220px/140px (horiz/vert)
        // 9-slice border baked into its texture - any panel using it needs to be comfortably
        // bigger than 2x that border in both dimensions, or the border graphics themselves eat
        // the space text insets were leaving for content. The previous 760x260/920x320 sizes
        // left as little as 40px of clean vertical space, which is why title/body overlapped.
        private const float PausePanelWidth = 1000f;
        private const float PausePanelHeight = 500f;
        private const float FrameBorderHorizontal = 230f;
        private const float FrameBorderTop = 165f;
        private const float FrameBorderBottom = 140f;
        private const string GameplayModalTexturePath = "Assets/Art/UI/HUD/gameplay_modal_frame.png";

        [SerializeField] private WaveManager _waveManager;
        [SerializeField] private SettingsManager _settingsManager;
        [SerializeField] private Texture2D _statusPanelTexture;
        [SerializeField] private Texture2D _waveBannerTexture;
        [SerializeField] private Texture2D _modalPanelTexture;
        [SerializeField] private Vector4 _statusPanelBorder = new Vector4(220f, 140f, 220f, 140f);
        [SerializeField] private Vector4 _waveBannerBorder = new Vector4(220f, 140f, 220f, 140f);
        [SerializeField] private Vector4 _modalPanelBorder = new Vector4(220f, 140f, 220f, 140f);
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        // Deliberately NOT [SerializeField] - see CardRewardPresenter's identical fields for why:
        // cross-scene references (HUDCanvas.unity is a separate scene), always re-resolved fresh
        // via GameObject.Find() in TryBindAuthoredHud(), and [SerializeField] made Unity log
        // "Cross scene references are not supported" and null the field out on domain reload/exit
        // Play Mode.
        private RectTransform _authoredHudRoot;
        private Image _authoredStatusPanelImage;
        private Text _authoredCurrencyText;
        private Text _authoredPlayerHealthText;
        private Text _authoredCoreHealthText;
        private Text _authoredWaveText;
        private Text _authoredTowerText;
        private Text _authoredStateText;

        private Canvas _canvas;
        private RectTransform _statusPanelRect;
        private Image _bannerImage;
        private Text _bannerText;
        private Text _currencyText;
        private Text _playerHealthText;
        private Text _coreHealthText;
        private Text _towerText;
        private Text _waveText;
        private Text _stateText;
        private Image _pausePanel;
        private Text _pauseTitleText;
        private Text _pauseBodyText;
        private Font _font;
        private Sprite _statusPanelSprite;
        private Sprite _waveBannerSprite;
        private Sprite _modalPanelSprite;
        private float _bannerTimer;
        private Texture2D _runtimeLoadedModalTexture;
        private UIManagedPanel _managedPanel;

        // NOT #if UNITY_EDITOR-gated: TryBindAuthoredHud() (used by Awake()/OnEnable()/Start() at
        // real runtime, not just in-Editor) reads these. They were incorrectly gated before,
        // which would have failed to compile in an actual Standalone build despite compiling fine
        // in the Editor (UNITY_EDITOR is always defined there) - caught while adding
        // RepairAuthoredLayoutForEditorTool() below, which needed the same names.
        private const string HudCanvasName = "HUDCanvas";
        private const string HudRootName = "HUD";
        private const string HudFrameName = "HUD_Frame_Panel";
        private const string CurrencyName = "Currency";
        private const string PlayerHealthName = "PlayerHealth";
        private const string CoreHealthName = "CoreHealth";
        private const string WaveName = "Wave";
        private const string TowerInfoName = "TowerInfo";
        private const string StateInfoName = "StateInfo";

        // Entry point for Assets/Editor/AuthoredCanvasRepairTool.cs - see
        // CardRewardPresenter.RepairAuthoredLayoutForEditorTool() for why this exists. Also runs
        // EnsureOverlayWidgets() so WaveBanner/PausePanel get correctly re-parented onto the full
        // canvas (not left nested under the small HUD status box) and that gets baked into the
        // saved scene too.
        //
        // Unlike CardRewardPresenter/ShopPresenter (which build their own sprites directly inside
        // TryBindAuthoredLayout via DebugSpriteFactory), TryBindAuthoredHud() unconditionally
        // assigns _authoredStatusPanelImage.sprite = _statusPanelSprite - a field only populated
        // by CreateRuntimeSprites(), a separate step normally run earlier by EnsureInitialized().
        // Calling TryBindAuthoredHud() alone left _statusPanelSprite null and actively overwrote
        // the panel's sprite with null (the exact white-box bug this tool exists to fix) -
        // CreateRuntimeSprites() must run first.
        public bool RepairAuthoredLayoutForEditorTool()
        {
            if (_font == null)
            {
                _font = ResolveFont();
            }

            ResolveRuntimeReferences();
            CreateRuntimeSprites();
            var bound = TryBindAuthoredHud();

            if (bound)
            {
                EnsureOverlayWidgets();
            }

            return bound;
        }

        private ResourceWalletService _resourceWalletService;
        private PlayerCombatController _playerCombatController;
        private CombatHitReceiver _coreHitReceiver;
        private ShopService _shopService;
        private TowerService _towerService;
        private CardRewardController _cardRewardController;
        private CardService _cardService;
        private IInputReader _inputReader;
        private bool _hasShownStartOverlay;
        private OverlayMode _overlayMode;
        private int _pauseOverlayClosedFrame = -1;

        private enum OverlayMode
        {
            None,
            Start,
            Pause,
            Failed,

            // Tutorial.md / playtest feedback (2026-09-16): a paused, dismiss-on-Confirm explanation
            // beat shown once per Tutorial step, owned here rather than by a second component so
            // there is still only one thing in the game that ever touches Time.timeScale.
            Tutorial
        }

        private System.Action _tutorialBeatDismissed;

        private IInputReader InputReader
        {
            get
            {
                if (_inputReader == null)
                {
                    _inputReader = FindFirstObjectByType<CoreboundInputReader>();
                }

                return _inputReader;
            }
        }

        // See CardRewardPresenter.Awake() for the full root-cause writeup (2026-09-16 playtest:
        // HUD showed as a blank white box via MainMenu -> New Run, fine when opening SampleScene
        // directly). Calling the fallback-capable EnsureInitialized() from Awake() let
        // ResolveOrBuildHud()'s local-build fallback (BuildCanvas()) run before HUDCanvas.unity's
        // additive load completes, permanently satisfying EnsureInitialized()'s "_pausePanel !=
        // null" guard and defeating Start()'s later retry. Awake() now only attempts a bind (safe
        // no-op if the canvas is not yet loaded).
        private void Awake()
        {
            TryBindAuthoredHud();
            ApplyHudScale();
            Refresh();
        }

        // The one place guaranteed to run after HUDCanvas.unity's additive load has completed -
        // see the Awake() comment above for why the fallback-capable EnsureInitialized() must not
        // be called any earlier than this.
        private void Start()
        {
            EnsureInitialized();
            ApplyHudScale();
            Refresh();
        }

        // See CardRewardPresenter.EnsureInitialized() / ShopPresenter.EnsureInitialized() for why
        // this guard exists: _pausePanel and the other runtime fields built here are plain
        // (non-serialized) fields, reset by any domain reload (script recompile, or entering Play
        // Mode with "Reload Domain" enabled). Unity's backup/restore cycle re-invokes OnEnable() on
        // already-existing scene objects without calling Awake() again, so without this guard the
        // first OnEnable()->Refresh() after a reload ran against null HUD widget references - this
        // is the same root cause already traced (2026-09-15) to EnsureOverlayWidgets() resolving a
        // stale cached _canvas and building orphaned duplicate PausePanel/WaveBanner objects; this
        // closes the same gap for every other field Awake() used to populate exactly once.
        private void EnsureInitialized()
        {
            if (_pausePanel != null)
            {
                return;
            }

            _font = ResolveFont();
            ResolveRuntimeReferences();
            CreateRuntimeSprites();
            ResolveOrBuildHud();
            RegisterWithUIManager();
        }

        private void ApplyHudScale()
        {
            // [ExecuteAlways] also runs Awake in the Editor outside Play mode; only rescale the
            // authored HUD once actually playing so editing the scene never shows a resized HUD.
            if (!Application.isPlaying || _authoredHudRoot == null)
            {
                return;
            }

            var hudScale = _settingsManager != null && _settingsManager.RuntimeSettings != null
                ? _settingsManager.RuntimeSettings.HudScale
                : 1f;

            _authoredHudRoot.localScale = Vector3.one * Mathf.Clamp(hudScale, 0.5f, 2f);
        }

#if UNITY_EDITOR
        // HUDCanvas.unity is a separate authored scene now (Assets/Scenes/HUDCanvas.unity), not
        // something this presenter creates - OnValidate no longer builds anything. It only retries
        // the bind, a no-op once EnsureInitialized() already succeeded, and picks the canvas up if
        // the user opens HUDCanvas.unity additively alongside this scene later.
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            UnityEditor.EditorApplication.delayCall += DeferredEnsureInitialized;
        }

        private void DeferredEnsureInitialized()
        {
            if (this == null || Application.isPlaying)
            {
                return;
            }

            EnsureInitialized();
            ApplyHudScale();
            Refresh();
        }
#endif

        private void OnEnable()
        {
            // Same reasoning as Awake() above - OnEnable() runs in the same early phase, before
            // HUDCanvas.unity's additive load can complete on the MainMenu-transition path.
            TryBindAuthoredHud();
            ApplyHudScale();

            // HUDCanvas.unity only exists from the end of the frame the gameplay scene loaded in,
            // which is after Start(). This is the first point at which binding can actually work
            // on the MainMenu -> New Run path.
            AdditiveCanvasSceneLoader.CanvasSceneReady += OnCanvasSceneReady;

            if (_waveManager != null)
            {
                _waveManager.StateChanged += Refresh;
            }

            if (_settingsManager != null)
            {
                _settingsManager.SettingsApplied += ApplyHudScale;
            }

            WaveEvents.WaveStarted += OnWaveStarted;
            WaveEvents.WaveCompleted += OnWaveCompleted;
            WaveEvents.IntermissionStarted += OnIntermissionStarted;
            StageEvents.StageStarted += OnStageStarted;
            StageEvents.StageCompleted += OnStageCompleted;
            StageEvents.StageTransitioned += OnStageTransitioned;

            if (InputReader != null)
            {
                InputReader.PausePressed += OnPausePressed;
                InputReader.ConfirmPressed += OnConfirmPressed;
                InputReader.CancelPressed += OnCancelPressed;
            }

            Refresh();
        }

        private void OnCanvasSceneReady(string sceneName)
        {
            if (sceneName != AdditiveCanvasSceneLoader.HudCanvasSceneName)
            {
                return;
            }

            if (TryBindAuthoredHud())
            {
                EnsureOverlayWidgets();
                RegisterWithUIManager();
                ApplyHudScale();
            }

            Refresh();
        }

        private void OnDisable()
        {
            AdditiveCanvasSceneLoader.CanvasSceneReady -= OnCanvasSceneReady;

            if (_waveManager != null)
            {
                _waveManager.StateChanged -= Refresh;
            }

            if (_settingsManager != null)
            {
                _settingsManager.SettingsApplied -= ApplyHudScale;
            }

            WaveEvents.WaveStarted -= OnWaveStarted;
            WaveEvents.WaveCompleted -= OnWaveCompleted;
            WaveEvents.IntermissionStarted -= OnIntermissionStarted;
            StageEvents.StageStarted -= OnStageStarted;
            StageEvents.StageCompleted -= OnStageCompleted;
            StageEvents.StageTransitioned -= OnStageTransitioned;

            if (InputReader != null)
            {
                InputReader.PausePressed -= OnPausePressed;
                InputReader.ConfirmPressed -= OnConfirmPressed;
                InputReader.CancelPressed -= OnCancelPressed;
            }

            if (_overlayMode != OverlayMode.None)
            {
                CloseOverlay();
            }
        }

        private void OnDestroy()
        {
            DestroyRuntimeSprites();
            CleanupTexture(_runtimeLoadedModalTexture);
            UIManager.UnregisterPanel(_managedPanel);
        }

        private void Update()
        {
            if (_bannerTimer > 0f)
            {
                _bannerTimer = Mathf.Max(0f, _bannerTimer - Time.deltaTime);

                if (_bannerTimer <= 0f)
                {
                    HideBanner();
                }
            }

            Refresh();
        }

        private void BuildCanvas()
        {
            if (TryBindAuthoredHud())
            {
                EnsureOverlayWidgets();
                return;
            }

            _canvas = gameObject.GetComponent<Canvas>();

            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            if (gameObject.GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            var rectTransform = gameObject.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var statusPanel = CreateImage("StatusPanel", rectTransform, _statusPanelSprite, new Color(1f, 1f, 1f, 0.98f), ResolvePanelType(_statusPanelSprite));
            _statusPanelRect = statusPanel.rectTransform;
            _statusPanelRect.anchorMin = new Vector2(1f, 1f);
            _statusPanelRect.anchorMax = new Vector2(1f, 1f);
            _statusPanelRect.pivot = new Vector2(1f, 1f);
            // Pulled off the literal corner by the golden ratio (margin * phi, phi=1.618) instead of
            // sitting flush against it - a small, tasteful inset instead of edge-to-edge placement.
            _statusPanelRect.anchoredPosition = new Vector2(-45f, -32f);
            _statusPanelRect.sizeDelta = new Vector2(StatusPanelWidth, StatusPanelHeight);

            _currencyText = CreateText("CurrencyText", _statusPanelRect, 14, TextAnchor.MiddleLeft, new Color(0.95f, 0.97f, 1f, 1f));
            StretchToParent(_currencyText.rectTransform, new Vector2(0f, 0.56f), new Vector2(0.22f, 1f), new Vector2(27f, 9f), new Vector2(-5f, -10f));
            ConfigureResponsiveHudText(_currencyText, 10, 14);

            _playerHealthText = CreateText("PlayerHealthText", _statusPanelRect, 14, TextAnchor.MiddleCenter, new Color(0.95f, 0.97f, 1f, 1f));
            StretchToParent(_playerHealthText.rectTransform, new Vector2(0.22f, 0.56f), new Vector2(0.42f, 1f), new Vector2(5f, 9f), new Vector2(-5f, -10f));
            ConfigureResponsiveHudText(_playerHealthText, 10, 14);

            _coreHealthText = CreateText("CoreHealthText", _statusPanelRect, 14, TextAnchor.MiddleCenter, new Color(0.95f, 0.97f, 1f, 1f));
            StretchToParent(_coreHealthText.rectTransform, new Vector2(0.42f, 0.56f), new Vector2(0.58f, 1f), new Vector2(5f, 9f), new Vector2(-5f, -10f));
            ConfigureResponsiveHudText(_coreHealthText, 10, 14);

            _waveText = CreateText("WaveText", _statusPanelRect, 18, TextAnchor.MiddleRight, Color.white);
            StretchToParent(_waveText.rectTransform, new Vector2(0.58f, 0.56f), new Vector2(1f, 1f), new Vector2(12f, 9f), new Vector2(-27f, -10f));
            ConfigureResponsiveHudText(_waveText, 12, 18);

            _towerText = CreateText("TowerText", _statusPanelRect, 12, TextAnchor.MiddleLeft, new Color(0.95f, 0.97f, 1f, 1f));
            StretchToParent(_towerText.rectTransform, new Vector2(0f, 0f), new Vector2(0.65f, 0.56f), new Vector2(27f, 12f), new Vector2(-12f, -12f));
            ConfigureResponsiveHudText(_towerText, 8, 12);

            _stateText = CreateText("StateText", _statusPanelRect, 14, TextAnchor.MiddleRight, new Color(1f, 0.92f, 0.72f, 1f));
            StretchToParent(_stateText.rectTransform, new Vector2(0.65f, 0f), new Vector2(1f, 0.56f), new Vector2(12f, 12f), new Vector2(-27f, -12f));
            ConfigureResponsiveHudText(_stateText, 10, 14);

            var bannerPanel = CreateImage("WaveBanner", rectTransform, _waveBannerSprite != null ? _waveBannerSprite : _statusPanelSprite, new Color(1f, 1f, 1f, 0.98f), ResolvePanelType(_waveBannerSprite != null ? _waveBannerSprite : _statusPanelSprite));
            var bannerRect = bannerPanel.rectTransform;
            bannerRect.anchorMin = new Vector2(0.5f, 1f);
            bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.anchoredPosition = new Vector2(0f, -28f);
            bannerRect.sizeDelta = new Vector2(BannerWidth, BannerHeight);
            bannerRect.gameObject.SetActive(false);
            _bannerImage = bannerPanel;

            _bannerText = CreateText("WaveBannerText", bannerRect, 30, TextAnchor.MiddleCenter, new Color(0.1f, 0.12f, 0.16f, 0.95f));
            StretchToParent(_bannerText.rectTransform, Vector2.zero, Vector2.one, new Vector2(48f, 18f), new Vector2(-48f, -18f));

            var modalSprite = _modalPanelSprite != null ? _modalPanelSprite : _statusPanelSprite;
            _pausePanel = CreateImage("PausePanel", rectTransform, modalSprite, new Color(1f, 1f, 1f, 0.98f), ResolvePanelType(modalSprite));
            var pauseRect = _pausePanel.rectTransform;
            pauseRect.anchorMin = new Vector2(0.5f, 0.5f);
            pauseRect.anchorMax = new Vector2(0.5f, 0.5f);
            pauseRect.pivot = new Vector2(0.5f, 0.5f);
            pauseRect.anchoredPosition = Vector2.zero;
            pauseRect.sizeDelta = new Vector2(PausePanelWidth, PausePanelHeight);
            pauseRect.gameObject.SetActive(false);

            // Non-overlapping bands inside the safe zone (FrameBorderBottom to
            // PausePanelHeight - FrameBorderTop = 140 to 335): title takes the top 80px, body
            // takes a 100px band below it with a gap in between.
            _pauseTitleText = CreateText("PauseTitle", pauseRect, 30, TextAnchor.UpperCenter, new Color(0.08f, 0.12f, 0.17f, 0.98f));
            StretchToParent(_pauseTitleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(FrameBorderHorizontal, 255f), new Vector2(-FrameBorderHorizontal, -FrameBorderTop));
            _pauseTitleText.text = LocalizationService.Resolve("Paused", "Pausiert");

            _pauseBodyText = CreateText("PauseBody", pauseRect, 18, TextAnchor.MiddleCenter, new Color(0.1f, 0.12f, 0.16f, 0.96f));
            StretchToParent(_pauseBodyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(FrameBorderHorizontal, FrameBorderBottom), new Vector2(-FrameBorderHorizontal, -260f));
            _pauseBodyText.text = LocalizationService.Resolve(
                "Press Pause or Cancel to resume.\nPress Confirm to return to the Main Menu.",
                "Pause oder Abbrechen zum Fortsetzen.\nBestätigen kehrt zum Hauptmenü zurück.");
        }

        private void Refresh()
        {
            if (_waveManager == null || _waveText == null || _stateText == null)
            {
                return;
            }

            var isRewardOpen = _cardRewardController != null && _cardRewardController.IsOpen;

            if (_statusPanelRect != null)
            {
                _statusPanelRect.gameObject.SetActive(!isRewardOpen);
            }

            if (isRewardOpen)
            {
                return;
            }

            if (_waveManager.State == WaveState.Failed && _overlayMode != OverlayMode.Failed && _overlayMode != OverlayMode.Tutorial)
            {
                // Playtest feedback (2026-09-17): "Run Failed should contain 'player Died' or 'core
                // destroyed'" - the generic message didn't say which.
                OpenOverlay(
                    OverlayMode.Failed,
                    LocalizationService.Resolve("Run Failed", "Run gescheitert"),
                    _waveManager.FailureCauseLabel + LocalizationService.Resolve(". Confirm returns to the Main Menu.", ". Bestätigen kehrt zum Hauptmenü zurück."));
            }

            _waveText.text = _waveManager.StageLabel + " | " + ResolveHeaderLabel();
            if (_currencyText != null)
            {
                _currencyText.text = ResolveCurrencyText();
            }

            if (_playerHealthText != null)
            {
                _playerHealthText.text = ResolvePlayerHealthText();
            }

            if (_coreHealthText != null)
            {
                _coreHealthText.text = ResolveCoreHealthText();
            }

            // Tower placement/upgrade info only matters while the player can actually act on it
            // (Intermission); showing stale slot-selection text during Combat cluttered the HUD
            // with information that wasn't currently relevant, per playtest feedback that the HUD
            // "should contain HP player and HP core, Scrap, stage and wave" with tower info visible
            // only during intermission.
            var showTowerText = _waveManager.State == WaveState.Intermission;
            _towerText.gameObject.SetActive(showTowerText);

            if (showTowerText)
            {
                _towerText.text = ResolveTowerText();
            }

            switch (_waveManager.State)
            {
                case WaveState.Combat:
                    _stateText.text = LocalizationService.Resolve("Combat | Remaining Enemies: ", "Kampf | Verbleibende Gegner: ") + _waveManager.RemainingAliveEnemies;
                    return;
                case WaveState.BossCombat:
                    _stateText.text = "Boss | " + _waveManager.BossLabel + " | " + _waveManager.BossPhaseLabel + " | " + (_waveManager.BossHealthNormalized * 100f).ToString("0") + "%";
                    return;
                case WaveState.Reward:
                    _stateText.text = LocalizationService.Resolve("Reward | Choose a Card to continue.", "Belohnung | Wähle eine Karte, um fortzufahren.");
                    return;
                case WaveState.Intermission:
                    // Playtest feedback (2026-09-17): Confirm now always ends Intermission
                    // (WaveManager.OnConfirmPressed no longer gates on RequiresIntermissionTowerAction
                    // - building is optional), so this no longer claims a tower action is required.
                    _stateText.text = _waveManager.RequiresIntermissionShopAction
                        ? LocalizationService.Resolve("Shop | Confirm buys. Cancel skips.", "Shop | Bestätigen kauft. Abbrechen überspringt.")
                        : _waveManager.RequiresIntermissionTowerAction
                        ? LocalizationService.Resolve("Intermission | Optionally place or upgrade a tower. Confirm to continue.", "Zwischenphase | Optional einen Turm bauen oder verbessern. Bestätigen zum Fortfahren.")
                        : LocalizationService.Resolve("Intermission | ", "Zwischenphase | ") + _waveManager.IntermissionSecondsRemaining.ToString("0.0") + LocalizationService.Resolve("s or Confirm to continue.", "s oder Bestätigen zum Fortfahren.");
                    return;
                case WaveState.Failed:
                    _stateText.text = LocalizationService.Resolve("Run Failed | ", "Run gescheitert | ") + _waveManager.FailureCauseLabel + ".";
                    return;
                default:
                    _stateText.text = LocalizationService.Resolve("Stage Complete", "Stage abgeschlossen");
                    UIManager.NotifyPanelChanged(_managedPanel);
                    return;
            }
        }

        private void OnPausePressed()
        {
            if (_overlayMode == OverlayMode.Start || _overlayMode == OverlayMode.Failed || _overlayMode == OverlayMode.Tutorial)
            {
                return;
            }

            if (_cardRewardController != null && _cardRewardController.IsOpen)
            {
                return;
            }

            // Shop's own Cancel handler also lives on Escape (see the frame-guard below) - without
            // this check, skipping a shop offer with Escape would simultaneously pop the Pause
            // overlay open on top of the closing Shop panel, since nothing here knew the Shop was
            // the thing actually being interacted with.
            if (_shopService != null && _shopService.IsOpen)
            {
                return;
            }

            if (_overlayMode == OverlayMode.Pause)
            {
                CloseOverlay();
                return;
            }

            // Pause (System map) and Cancel (Build/UI maps) both bind to Escape, so a single Escape
            // press while paused fires both handlers in the same frame. Without this guard, Cancel
            // closes the overlay first and Pause's own re-open check (above) then immediately
            // reopens it, since by that point _overlayMode has already reverted to None - making the
            // pause screen appear impossible to dismiss with Escape.
            if (_pauseOverlayClosedFrame == Time.frameCount)
            {
                return;
            }

            OpenOverlay(OverlayMode.Pause, LocalizationService.Resolve("Paused", "Pausiert"), ResolvePauseBodyText());
        }

        // Playtest feedback: "pause menu should show all current active effects" - the selected
        // cards ARE the run's active effects (each is a permanent passive upgrade for the rest of
        // the run, applied once via CardService.TryApplyCard and never re-shown anywhere after the
        // reward screen closes), so listing CardService.SelectedCards here is the direct answer
        // rather than a new tracked-effects system.
        private string ResolvePauseBodyText()
        {
            var baseText = LocalizationService.Resolve(
                "Press Confirm to return to the Main Menu.\nPress Pause or Cancel to resume.",
                "Bestätigen kehrt zum Hauptmenü zurück.\nPause oder Abbrechen zum Fortsetzen.");

            if (_cardService == null || _cardService.SelectedCards == null || _cardService.SelectedCards.Count == 0)
            {
                return baseText;
            }

            var effectsList = LocalizationService.Resolve("Active Effects: ", "Aktive Effekte: ");

            for (var index = 0; index < _cardService.SelectedCards.Count; index++)
            {
                var card = _cardService.SelectedCards[index];

                if (card == null)
                {
                    continue;
                }

                if (index > 0)
                {
                    effectsList += ", ";
                }

                effectsList += card.DisplayName;
            }

            return effectsList + "\n" + baseText;
        }

        private void OnConfirmPressed()
        {
            if (_overlayMode == OverlayMode.Start)
            {
                CloseOverlay();
                return;
            }

            if (_overlayMode == OverlayMode.Tutorial)
            {
                CloseOverlay();
                var callback = _tutorialBeatDismissed;
                _tutorialBeatDismissed = null;
                callback?.Invoke();
                return;
            }

            if (_overlayMode != OverlayMode.Pause && _overlayMode != OverlayMode.Failed)
            {
                return;
            }

            CloseOverlay();

            if (!string.IsNullOrWhiteSpace(_mainMenuSceneName))
            {
                SceneManager.LoadScene(_mainMenuSceneName);
            }
        }

        private void OnCancelPressed()
        {
            if (_overlayMode == OverlayMode.Pause)
            {
                CloseOverlay();
            }
        }

        private string ResolveHeaderLabel()
        {
            if (_waveManager.State == WaveState.BossCombat)
            {
                return LocalizationService.Resolve("Boss Encounter", "Boss-Begegnung");
            }

            if (_waveManager.State == WaveState.Failed)
            {
                return LocalizationService.Resolve("Run Failed", "Run gescheitert");
            }

            return LocalizationService.Resolve("Wave ", "Welle ") + _waveManager.CurrentWaveNumber + "/" + _waveManager.TotalWaveCount;
        }

        private void OnWaveStarted(WaveProgressEventArgs eventArgs)
        {
            var label = string.IsNullOrWhiteSpace(eventArgs.Label) ? LocalizationService.Resolve("Wave ", "Welle ") + eventArgs.WaveIndex : eventArgs.Label;
            ShowBanner(label, ResolveBannerColor(eventArgs.IsEliteEncounter, false));

            if (!_hasShownStartOverlay && eventArgs.WaveIndex == 1 && _overlayMode != OverlayMode.Tutorial)
            {
                _hasShownStartOverlay = true;
                OpenOverlay(OverlayMode.Start, label, LocalizationService.Resolve("Confirm begins the encounter.", "Bestätigen startet die Begegnung."));
            }
        }

        private void OnWaveCompleted(WaveProgressEventArgs eventArgs)
        {
            ShowBanner(LocalizationService.Resolve("Wave Clear", "Welle geschafft"), new Color(0.84f, 0.97f, 0.9f, 0.98f));
        }

        private void OnIntermissionStarted(WaveProgressEventArgs eventArgs)
        {
            var isBossIncoming = !string.IsNullOrWhiteSpace(eventArgs.Label) && eventArgs.Label.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0;
            ShowBanner(isBossIncoming ? LocalizationService.Resolve("Boss Incoming", "Boss naht") : LocalizationService.Resolve("Intermission", "Zwischenphase"), ResolveBannerColor(eventArgs.IsEliteEncounter, isBossIncoming));
        }

        private void OnStageStarted(StageEventArgs eventArgs)
        {
            var detailLabel = string.IsNullOrWhiteSpace(eventArgs.DetailLabel) ? eventArgs.StageLabel : eventArgs.DetailLabel;
            ShowBanner(eventArgs.StageLabel + " | " + detailLabel, new Color(0.9f, 0.94f, 1f, 0.98f));
        }

        private void OnStageCompleted(StageEventArgs eventArgs)
        {
            var detailLabel = string.IsNullOrWhiteSpace(eventArgs.DetailLabel) ? LocalizationService.Resolve("Stage Complete", "Stage abgeschlossen") : eventArgs.DetailLabel;
            ShowBanner(detailLabel, new Color(0.95f, 0.96f, 1f, 0.98f));
        }

        private void OnStageTransitioned(StageEventArgs eventArgs)
        {
            if (string.IsNullOrWhiteSpace(eventArgs.DetailLabel))
            {
                return;
            }

            ShowBanner(
                eventArgs.DetailLabel,
                eventArgs.IsFinalStage ? new Color(1f, 0.93f, 0.72f, 0.98f) : new Color(0.88f, 0.96f, 1f, 0.98f));
        }

        private void ShowBanner(string message, Color tint)
        {
            if (_bannerImage == null || _bannerText == null)
            {
                return;
            }

            _bannerImage.color = tint;
            _bannerText.text = message;
            _bannerImage.gameObject.SetActive(true);
            _bannerTimer = BannerDisplaySeconds;
        }

        private void HideBanner()
        {
            if (_bannerImage != null)
            {
                _bannerImage.gameObject.SetActive(false);
            }
        }

        private string ResolveCurrencyText()
        {
            return _resourceWalletService != null
                ? _resourceWalletService.DisplayLabel + ": " + _resourceWalletService.CurrentAmount
                : LocalizationService.Resolve("Resources: --", "Ressourcen: --");
        }

        private string ResolvePlayerHealthText()
        {
            return _playerCombatController != null
                ? LocalizationService.Resolve("Player ", "Spieler ") + (_playerCombatController.CurrentHealthNormalized * 100f).ToString("0") + "%"
                : LocalizationService.Resolve("Player --", "Spieler --");
        }

        private string ResolveCoreHealthText()
        {
            return _coreHitReceiver != null
                ? "Core " + (_coreHitReceiver.CurrentHealthNormalized * 100f).ToString("0") + "%"
                : "Core --";
        }

        private void ResolveOrBuildHud()
        {
            if (TryBindAuthoredHud())
            {
                EnsureOverlayWidgets();
                return;
            }

            // HUDCanvas.unity's additive load completes at the end of this frame, after Start().
            // Building the procedural fallback HUD here is what produced the playtest report
            // "Player %, Core % and currency are shown shortly in white but disappear after some
            // seconds" (2026-09-19): the white placeholder rendered for a frame or so and was then
            // covered by the real HUDCanvas arriving on top of it. Wait for CanvasSceneReady
            // instead - OnCanvasSceneReady() drives the real bind once the objects exist.
            if (AdditiveCanvasSceneLoader.IsScenePending(AdditiveCanvasSceneLoader.HudCanvasSceneName))
            {
                return;
            }

            BuildCanvas();
        }

        // HUDCanvas.unity (Assets/Scenes/HUDCanvas.unity) is loaded additively by
        // AdditiveCanvasSceneLoader, so it is never a child of this presenter's own GameObject -
        // GameObject.Find() searches every loaded scene, which transform.Find() (own-children-only)
        // cannot do. Re-resolved fresh on every call rather than trusting the cached _authored*
        // fields, matching CardRewardPresenter/ShopPresenter's identical fix.
        private bool TryBindAuthoredHud()
        {
            var canvasObject = GameObject.Find(HudCanvasName);
            var canvasRect = canvasObject != null ? canvasObject.transform as RectTransform : null;
            _authoredHudRoot = canvasRect != null ? canvasRect.Find(HudRootName) as RectTransform : null;

            if (_authoredHudRoot == null)
            {
                return false;
            }

            _authoredStatusPanelImage = _authoredHudRoot.Find(HudFrameName) != null ? _authoredHudRoot.Find(HudFrameName).GetComponent<Image>() : null;
            _authoredCurrencyText = _authoredHudRoot.Find(CurrencyName) != null ? _authoredHudRoot.Find(CurrencyName).GetComponent<Text>() : null;
            _authoredPlayerHealthText = _authoredHudRoot.Find(PlayerHealthName) != null ? _authoredHudRoot.Find(PlayerHealthName).GetComponent<Text>() : null;
            _authoredCoreHealthText = _authoredHudRoot.Find(CoreHealthName) != null ? _authoredHudRoot.Find(CoreHealthName).GetComponent<Text>() : null;
            _authoredWaveText = _authoredHudRoot.Find(WaveName) != null ? _authoredHudRoot.Find(WaveName).GetComponent<Text>() : null;
            _authoredTowerText = _authoredHudRoot.Find(TowerInfoName) != null ? _authoredHudRoot.Find(TowerInfoName).GetComponent<Text>() : null;
            _authoredStateText = _authoredHudRoot.Find(StateInfoName) != null ? _authoredHudRoot.Find(StateInfoName).GetComponent<Text>() : null;

            if (_authoredStatusPanelImage != null)
            {
                // The scene's authored reference to hud_frame_panel.png's sprite sub-asset was
                // saved back when that texture's Sprite Editor import was broken (rect outside
                // texture bounds - fixed earlier, but fixing the import doesn't repair a reference
                // already baked into the scene as pointing at a since-regenerated/missing sprite).
                // Reassigning from the runtime-built sprite (made directly from the raw Texture2D,
                // bypassing the Sprite Editor sub-asset system entirely) sidesteps that permanently
                // instead of depending on the scene's on-disk reference happening to still resolve.
                _authoredStatusPanelImage.sprite = _statusPanelSprite;
            }

            _statusPanelRect = _authoredStatusPanelImage != null ? _authoredStatusPanelImage.rectTransform : null;
            _currencyText = _authoredCurrencyText;
            _playerHealthText = _authoredPlayerHealthText;
            _coreHealthText = _authoredCoreHealthText;
            _waveText = _authoredWaveText;
            _towerText = _authoredTowerText;
            _stateText = _authoredStateText;

            // GetComponentInParent<Canvas>() includes the object it's called on, not just true
            // ancestors - "HUD" (_authoredHudRoot) carries its own nested Canvas (m_OverrideSorting
            // so the small status box gets a local sort order), so this used to resolve to that
            // nested Canvas instead of HUDCanvas's true root. .rootCanvas walks the rest of the way
            // up regardless of how many nested Canvases sit in between.
            var nearestHudCanvas = _authoredHudRoot.GetComponentInParent<Canvas>();
            _canvas = nearestHudCanvas != null ? nearestHudCanvas.rootCanvas : null;
            return _statusPanelRect != null && _waveText != null && _stateText != null;
        }

        private void EnsureOverlayWidgets()
        {
            if (_authoredHudRoot == null)
            {
                return;
            }

            // Overlays (wave banner, pause/failed modal) must be screen-centered, not confined to
            // the small top-right HUD status box - parent them on the full-screen HUD canvas
            // instead of _authoredHudRoot. Falls back to _authoredHudRoot only if the canvas
            // somehow can't be resolved, so overlays never end up unparented.
            //
            // Root cause of the recurring "WaveBanner/PausePanel duplicated under HUD" bug
            // (2026-09-15, again 2026-09-17): _authoredHudRoot ("HUD") carries its own nested
            // Canvas (m_OverrideSorting, for the status box's local sort order).
            // GetComponentInParent<Canvas>() includes the object it's called on, not just true
            // ancestors, so this always resolved to that nested Canvas on "HUD" itself -
            // overlayParent silently collapsed to _authoredHudRoot every time. WaveBanner/
            // PausePanel actually live one level up, under HUDCanvas's true root, so
            // overlayParent.Find(...) below always missed and created fresh duplicates inside
            // "HUD" instead of reusing them. .rootCanvas walks the rest of the way up past any
            // number of nested Canvases to the real outermost one.
            var nearestCanvas = _authoredHudRoot.GetComponentInParent<Canvas>();
            var canvasRect = nearestCanvas != null
                ? nearestCanvas.rootCanvas.transform as RectTransform
                : null;
            var overlayParent = canvasRect != null ? canvasRect : _authoredHudRoot;

            if (_bannerImage == null)
            {
                // Look for an already-authored WaveBanner before creating one - this method runs on
                // every Awake() (including edit-mode ExecuteAlways passes), and blindly creating a
                // new GameObject each time left dozens of orphaned duplicates in the scene, one of
                // which could be stuck active from an interrupted session with no live script
                // reference left to hide it again.
                var existingBannerRect = overlayParent.Find("WaveBanner") as RectTransform;

                // Older scenes may still have the banner parented under the HUD status box from
                // before this fix - reparent it onto the full-screen canvas instead of leaving a
                // second, differently-parented copy behind.
                if (existingBannerRect == null && overlayParent != _authoredHudRoot)
                {
                    existingBannerRect = _authoredHudRoot.Find("WaveBanner") as RectTransform;

                    if (existingBannerRect != null)
                    {
                        existingBannerRect.SetParent(overlayParent, false);
                    }
                }

                if (existingBannerRect != null)
                {
                    _bannerImage = existingBannerRect.GetComponent<Image>();
                    var existingBannerTextTransform = existingBannerRect.Find("WaveBannerText");
                    _bannerText = existingBannerTextTransform != null ? existingBannerTextTransform.GetComponent<Text>() : null;
                }

                if (_bannerImage == null || _bannerText == null)
                {
                    var bannerPanel = CreateImage("WaveBanner", overlayParent, _waveBannerSprite != null ? _waveBannerSprite : _statusPanelSprite, new Color(1f, 1f, 1f, 0.98f), ResolvePanelType(_waveBannerSprite != null ? _waveBannerSprite : _statusPanelSprite));
                    var bannerRect = bannerPanel.rectTransform;
                    bannerRect.anchorMin = new Vector2(0.5f, 1f);
                    bannerRect.anchorMax = new Vector2(0.5f, 1f);
                    bannerRect.pivot = new Vector2(0.5f, 1f);
                    bannerRect.anchoredPosition = new Vector2(0f, -28f);
                    bannerRect.sizeDelta = new Vector2(BannerWidth, BannerHeight);
                    _bannerImage = bannerPanel;

                    _bannerText = CreateText("WaveBannerText", bannerRect, 30, TextAnchor.MiddleCenter, new Color(0.1f, 0.12f, 0.16f, 0.95f));
                    StretchToParent(_bannerText.rectTransform, Vector2.zero, Vector2.one, new Vector2(48f, 18f), new Vector2(-48f, -18f));
                }

                // Re-apply the sprite/layout unconditionally, even when reusing an existing banner
                // found by name above - a banner Image left over from before the runtime sprites
                // loaded correctly would otherwise keep a stale/null sprite forever (rendering as
                // a blank white rectangle) since the create-new branch above only runs when no
                // existing banner was found at all.
                var reusedBannerSprite = _waveBannerSprite != null ? _waveBannerSprite : _statusPanelSprite;
                _bannerImage.sprite = reusedBannerSprite;
                _bannerImage.type = ResolvePanelType(reusedBannerSprite);
                var reusedBannerRect = _bannerImage.rectTransform;
                reusedBannerRect.anchorMin = new Vector2(0.5f, 1f);
                reusedBannerRect.anchorMax = new Vector2(0.5f, 1f);
                reusedBannerRect.pivot = new Vector2(0.5f, 1f);
                reusedBannerRect.anchoredPosition = new Vector2(0f, -28f);
                reusedBannerRect.sizeDelta = new Vector2(BannerWidth, BannerHeight);

                if (_bannerText != null)
                {
                    StretchToParent(_bannerText.rectTransform, Vector2.zero, Vector2.one, new Vector2(48f, 18f), new Vector2(-48f, -18f));
                }

                // _bannerTimer starts at 0 (no countdown restored across a domain reload), so
                // whichever banner we ended up bound to must start hidden regardless of whatever
                // active state it was left in.
                _bannerImage.gameObject.SetActive(false);
            }

            if (_pausePanel == null)
            {
                var existingPausePanel = overlayParent.Find("PausePanel") as RectTransform;

                // Same reparent-if-found-in-the-old-location handling as the wave banner above.
                if (existingPausePanel == null && overlayParent != _authoredHudRoot)
                {
                    existingPausePanel = _authoredHudRoot.Find("PausePanel") as RectTransform;

                    if (existingPausePanel != null)
                    {
                        existingPausePanel.SetParent(overlayParent, false);
                    }
                }

                var existingPauseImage = existingPausePanel != null ? existingPausePanel.GetComponent<Image>() : null;

                if (existingPauseImage != null)
                {
                    _pausePanel = existingPauseImage;
                    var existingTitleTransform = existingPausePanel.Find("PauseTitle");
                    var existingBodyTransform = existingPausePanel.Find("PauseBody");
                    _pauseTitleText = existingTitleTransform != null ? existingTitleTransform.GetComponent<Text>() : null;
                    _pauseBodyText = existingBodyTransform != null ? existingBodyTransform.GetComponent<Text>() : null;
                }

                if (_pausePanel == null || _pauseTitleText == null || _pauseBodyText == null)
                {
                    var modalSprite = _modalPanelSprite != null ? _modalPanelSprite : _statusPanelSprite;
                    _pausePanel = CreateImage("PausePanel", overlayParent, modalSprite, new Color(1f, 1f, 1f, 0.98f), ResolvePanelType(modalSprite));

                    _pauseTitleText = CreateText("PauseTitle", _pausePanel.rectTransform, 30, TextAnchor.UpperCenter, new Color(0.08f, 0.12f, 0.17f, 0.98f));
                    _pauseBodyText = CreateText("PauseBody", _pausePanel.rectTransform, 18, TextAnchor.MiddleCenter, new Color(0.1f, 0.12f, 0.16f, 0.96f));
                }

                // Re-apply sprite and layout unconditionally, even when reusing an existing panel
                // found by name above - an old panel from before this fix would otherwise keep its
                // stale (too-small, or null-sprite) state forever, since the block above only runs
                // when creating fresh.
                var reusedModalSprite = _modalPanelSprite != null ? _modalPanelSprite : _statusPanelSprite;
                _pausePanel.sprite = reusedModalSprite;
                _pausePanel.type = ResolvePanelType(reusedModalSprite);
                var reusedPauseRect = _pausePanel.rectTransform;
                reusedPauseRect.anchorMin = new Vector2(0.5f, 0.5f);
                reusedPauseRect.anchorMax = new Vector2(0.5f, 0.5f);
                reusedPauseRect.pivot = new Vector2(0.5f, 0.5f);
                reusedPauseRect.anchoredPosition = Vector2.zero;
                reusedPauseRect.sizeDelta = new Vector2(PausePanelWidth, PausePanelHeight);
                StretchToParent(_pauseTitleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(FrameBorderHorizontal, 255f), new Vector2(-FrameBorderHorizontal, -FrameBorderTop));
                StretchToParent(_pauseBodyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(FrameBorderHorizontal, FrameBorderBottom), new Vector2(-FrameBorderHorizontal, -260f));

                // Pause visibility is driven explicitly elsewhere (not by a timer), so only force it
                // closed here if we just bound to a pre-existing panel left open from a stuck state.
                _pausePanel.gameObject.SetActive(false);
            }
        }

        private string ResolveTowerText()
        {
            if (_shopService != null && _shopService.IsOpen)
            {
                return _shopService.SelectedDescription;
            }

            return _towerService != null ? _towerService.SelectedSlotSummary : string.Empty;
        }

        private void ResolveRuntimeReferences()
        {
            _resourceWalletService = GetComponent<ResourceWalletService>();
            _shopService = GetComponent<ShopService>();
            _towerService = GetComponent<TowerService>();
            _cardRewardController = GetComponent<CardRewardController>();
            _cardService = GetComponent<CardService>();
            _playerCombatController = FindFirstObjectByType<PlayerCombatController>();

            if (_coreHitReceiver == null)
            {
                var coreObject = GameObject.Find(CombatHitReceiver.CoreAnchorObjectName);
                _coreHitReceiver = coreObject != null ? coreObject.GetComponent<CombatHitReceiver>() : null;
            }
        }

        // Called by TutorialService. Returns false (and shows nothing) if another overlay already
        // owns the screen - the caller is expected to retry later rather than force its way in,
        // since only one overlay may ever hold Time.timeScale at a time.
        public bool TryShowTutorialBeat(string title, string body, System.Action onDismissed)
        {
            if (_overlayMode != OverlayMode.None)
            {
                return false;
            }

            _tutorialBeatDismissed = onDismissed;
            OpenOverlay(OverlayMode.Tutorial, title, body);
            return true;
        }

        private void OpenOverlay(OverlayMode overlayMode, string title, string body)
        {
            _overlayMode = overlayMode;
            Time.timeScale = 0f;

            if (_pausePanel != null)
            {
                _pausePanel.gameObject.SetActive(true);
                _pausePanel.rectTransform.SetAsLastSibling();
            }

            if (_pauseTitleText != null)
            {
                _pauseTitleText.text = title;
            }

            if (_pauseBodyText != null)
            {
                _pauseBodyText.text = body;
            }
        }

        private void CloseOverlay()
        {
            if (_overlayMode == OverlayMode.Pause)
            {
                _pauseOverlayClosedFrame = Time.frameCount;
            }

            _overlayMode = OverlayMode.None;
            Time.timeScale = 1f;

            if (_pausePanel != null)
            {
                _pausePanel.gameObject.SetActive(false);
            }

            UIManager.NotifyPanelChanged(_managedPanel);
        }

        private Color ResolveBannerColor(bool isEliteEncounter, bool forceWarning)
        {
            return WaveHudColorPalette.ResolveBannerColor(isEliteEncounter, forceWarning);
        }

        private Text CreateText(string name, RectTransform parent, int fontSize, TextAnchor anchor, Color color)
        {
            return ProceduralUIFactory.CreateText(name, parent, _font, fontSize, FontStyle.Normal, color, anchor);
        }

        private static void ConfigureResponsiveHudText(Text text, int minSize, int maxSize)
        {
            if (text == null)
            {
                return;
            }

            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color, Image.Type imageType)
        {
            return ProceduralUIFactory.CreateImage(name, parent, sprite, color, imageType);
        }

        private void CreateRuntimeSprites()
        {
            _statusPanelSprite = CreateRuntimeSpriteRect(_statusPanelTexture, HudFrameContentRect, ResolveStatusPanelBorder());
            _waveBannerSprite = CreateRuntimeSprite(_waveBannerTexture, ResolveWaveBannerBorder());
            _modalPanelSprite = ResolveModalSprite();
        }

        // hud_frame_panel.png is a 1088x606 canvas but the actual hollow-frame artwork only
        // occupies a 960x454 content rect inside it (measured directly: alpha bbox (64,69)-
        // (1024,523)). The border values below were already correctly measured against that real
        // artwork ("~30-35px sides, ~36-60px top/bottom" per the comment on ResolveStatusPanelBorder)
        // but were being applied against the FULL PADDED CANVAS (DebugSpriteFactory.CreateSprite
        // always slices the whole texture, not a cropped rect) - so even a correctly-sized border
        // landed entirely inside the transparent margin around the real frame, sampling empty space
        // at every corner (same bug class as the main menu button frame, playtest 2026-09-15 "size
        // adjust hud_frame_panel.png to the current content"). Slicing from this content rect
        // instead fixes it the same way.
        private static readonly Rect HudFrameContentRect = new Rect(64f, 83f, 960f, 454f);

        private void DestroyRuntimeSprites()
        {
            DestroyRuntimeSprite(_statusPanelSprite);
            DestroyRuntimeSprite(_waveBannerSprite);
            DestroyRuntimeSprite(_modalPanelSprite);
        }

        private Vector4 ResolveStatusPanelBorder()
        {
            // hud_frame_panel.png is a hollow outline frame, not a filled panel like the card/shop
            // art. Re-measured directly against the cropped HudFrameContentRect (960x454, not the
            // padded 1088x606 canvas): left ~37px, right ~35px, top ~61px, bottom ~38px.
            return _statusPanelBorder.sqrMagnitude > 0f ? _statusPanelBorder : new Vector4(37f, 38f, 35f, 61f);
        }

        private Vector4 ResolveWaveBannerBorder()
        {
            return _waveBannerBorder.sqrMagnitude > 0f ? _waveBannerBorder : new Vector4(220f, 140f, 220f, 140f);
        }

        private static Image.Type ResolvePanelType(Sprite sprite)
        {
            return sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        private static Sprite CreateRuntimeSprite(Texture2D texture, Vector4 border)
        {
            return DebugSpriteFactory.CreateSprite(texture, 100f, border);
        }

        private static Sprite CreateRuntimeSpriteRect(Texture2D texture, Rect pixelRect, Vector4 border)
        {
            if (texture == null)
            {
                return null;
            }

            var sprite = Sprite.Create(texture, pixelRect, new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, border);
            sprite.hideFlags = HideFlags.DontSaveInEditor;
            return sprite;
        }

        private static void DestroyRuntimeSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(sprite);
            }
            else
            {
                DestroyImmediate(sprite);
            }
        }

        private Sprite ResolveModalSprite()
        {
            if (_modalPanelTexture != null)
            {
                return CreateRuntimeSprite(_modalPanelTexture, _modalPanelBorder);
            }

            _runtimeLoadedModalTexture = DebugSpriteFactory.LoadTextureFromProjectFile(GameplayModalTexturePath);
            return CreateRuntimeSprite(_runtimeLoadedModalTexture, _modalPanelBorder);
        }

        private static void CleanupTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }

        private static void StretchToParent(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static Font ResolveFont()
        {
            return Corebound.UI.UIFontResolver.Resolve();
        }

        private void RegisterWithUIManager()
        {
            var root = _authoredHudRoot != null ? _authoredHudRoot : gameObject.GetComponent<RectTransform>();
            _managedPanel = UIManager.RegisterPanel(root, "gameplay_hud", UIPanelLayer.HUD, false, false);
        }

#if UNITY_EDITOR
        private void EnsureSceneHudLayout()
        {
            if (_authoredHudRoot != null && _authoredStatusPanelImage != null && _authoredWaveText != null && _authoredStateText != null)
            {
                return;
            }

            var canvasTransform = transform.Find(HudCanvasName) as RectTransform;

            if (canvasTransform == null)
            {
                var canvasObject = new GameObject(HudCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                canvasTransform = canvasObject.GetComponent<RectTransform>();
                StretchToParent(canvasTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            var hudRoot = EnsureEditorUiChild(canvasTransform, HudRootName, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-45f, -32f), new Vector2(StatusPanelWidth, StatusPanelHeight));
            _authoredHudRoot = hudRoot;
            _authoredStatusPanelImage = EnsureEditorImage(hudRoot, HudFrameName, "Assets/Art/UI/HUD/hud_frame_panel.png", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(StatusPanelWidth, StatusPanelHeight));
            _authoredCurrencyText = EnsureEditorText(hudRoot, CurrencyName, new Vector2(0f, 0.56f), new Vector2(0.22f, 1f), new Vector2(0f, 0.5f), new Vector2(27f, 9f), new Vector2(-12f, -10f), TextAnchor.MiddleLeft);
            _authoredPlayerHealthText = EnsureEditorText(hudRoot, PlayerHealthName, new Vector2(0.22f, 0.56f), new Vector2(0.42f, 1f), new Vector2(0.5f, 0.5f), new Vector2(5f, 9f), new Vector2(-5f, -10f), TextAnchor.MiddleCenter);
            _authoredCoreHealthText = EnsureEditorText(hudRoot, CoreHealthName, new Vector2(0.42f, 0.56f), new Vector2(0.58f, 1f), new Vector2(0.5f, 0.5f), new Vector2(5f, 9f), new Vector2(-5f, -10f), TextAnchor.MiddleCenter);
            _authoredWaveText = EnsureEditorText(hudRoot, WaveName, new Vector2(0.58f, 0.56f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(12f, 9f), new Vector2(-27f, -10f), TextAnchor.MiddleRight);
            _authoredTowerText = EnsureEditorText(hudRoot, TowerInfoName, new Vector2(0f, 0f), new Vector2(0.65f, 0.56f), new Vector2(0f, 0.5f), new Vector2(27f, 12f), new Vector2(-12f, -12f), TextAnchor.MiddleLeft);
            _authoredStateText = EnsureEditorText(hudRoot, StateInfoName, new Vector2(0.65f, 0f), new Vector2(1f, 0.56f), new Vector2(1f, 0.5f), new Vector2(12f, 12f), new Vector2(-27f, -12f), TextAnchor.MiddleRight);
            EditorUtility.SetDirty(this);
        }

        private static RectTransform EnsureEditorUiChild(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var child = parent.Find(name) as RectTransform;

            if (child == null)
            {
                var childObject = new GameObject(name, typeof(RectTransform));
                childObject.layer = 5;
                childObject.transform.SetParent(parent, false);
                child = childObject.GetComponent<RectTransform>();
            }

            child.anchorMin = anchorMin;
            child.anchorMax = anchorMax;
            child.pivot = pivot;
            child.anchoredPosition = anchoredPosition;
            child.sizeDelta = sizeDelta;
            return child;
        }

        private Image EnsureEditorImage(RectTransform parent, string name, string spritePath, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var child = EnsureEditorUiChild(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            var renderer = child.GetComponent<CanvasRenderer>() ?? child.gameObject.AddComponent<CanvasRenderer>();
            var image = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private Text EnsureEditorText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax, TextAnchor alignment)
        {
            var child = EnsureEditorUiChild(parent, name, anchorMin, anchorMax, pivot, Vector2.zero, Vector2.zero);
            StretchToParent(child, anchorMin, anchorMax, offsetMin, offsetMax);
            var renderer = child.GetComponent<CanvasRenderer>() ?? child.gameObject.AddComponent<CanvasRenderer>();
            var text = child.GetComponent<Text>() ?? child.gameObject.AddComponent<Text>();
            text.font = _font != null ? _font : ResolveFont();
            text.fontSize = 22;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 28;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = name;
            return text;
        }
#endif
    }
}

