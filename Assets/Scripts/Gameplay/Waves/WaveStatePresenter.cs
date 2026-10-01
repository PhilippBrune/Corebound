using System.Collections.Generic;
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
        // Playtest 2026-09-22 ("wave_banner.png still not in use", screenshot of two spear shapes):
        // the art WAS in use, but 9-sliced into a 760 x 84 rect. With 220/140 px borders on a
        // frame whose visible part is 943 x 387, slicing removed the whole middle and left only the
        // metal end caps. The banner now shows the cropped art unsliced at its own aspect (2.44),
        // with the text inside the art's dark screen.
        private const float BannerWidth = 520f;
        private const float BannerHeight = 213f;
        private static readonly Rect WaveBannerArtRect = new Rect(85f, 110f, 944f, 388f);

        // The art's inner dark screen, as insets from the banner edges (measured on the art).
        private static readonly Vector2 BannerTextInsetMin = new Vector2(108f, 56f);
        private static readonly Vector2 BannerTextInsetMax = new Vector2(-104f, -50f);
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
        private bool _builtProceduralFallback;
        private Corebound.Gameplay.Factory.FactoryService _factoryService;
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
        private const string WaveBannerBackingName = "WaveLabelBanner";
        private const string CurrencyIconName = "CurrencyIcon";
        private const string CurrencyIconPath = "Assets/Art/Resources/resource_primary_icon.png";
        private const float CurrencyIconSize = 22f;
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
        private InputContextData _contextBeforeOverlay;

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
                InputReader.PreviousPressed += OnPausePreviousPressed;
                InputReader.NextPressed += OnPauseNextPressed;
                InputReader.NavigatePerformed += OnPauseNavigate;
            }

            Refresh();
        }

        private void OnCanvasSceneReady(string sceneName)
        {
            if (sceneName != AdditiveCanvasSceneLoader.HudCanvasSceneName)
            {
                return;
            }

            // Must go through the full EnsureInitialized() sequence rather than binding directly:
            // HUDCanvas.unity's WaveBanner, PausePanel and HUD_Frame_Panel Images are all saved
            // with no sprite assigned and get theirs at runtime from CreateRuntimeSprites(), which
            // EnsureInitialized() runs *before* the bind. An earlier version of this method called
            // TryBindAuthoredHud()/EnsureOverlayWidgets() on their own, which skipped that step and
            // left every one of those panels rendering as a plain white box (playtest 2026-09-19).
            EnsureInitialized();
            ApplyHudScale();
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
                InputReader.PreviousPressed -= OnPausePreviousPressed;
                InputReader.NextPressed -= OnPauseNextPressed;
                InputReader.NavigatePerformed -= OnPauseNavigate;
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
            UpdatePauseHover();
            UpdateOverlayClicks();

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
                DiscardProceduralFallbackHud();
                EnsureOverlayWidgets();
                EnsureHudDecorations();
                return;
            }

            _builtProceduralFallback = true;

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

            // Run clock and account level, both behind the ShowRunTimer setting (run timer D2,
            // levelling L4). Drawn under the wave label rather than inside the crowded status row,
            // and hidden entirely when the setting is off so the default HUD is unchanged.
            _runTimerText = CreateText("RunTimerText", _statusPanelRect, 13, TextAnchor.MiddleRight, new Color(0.88f, 0.92f, 1f, 0.92f));
            StretchToParent(_runTimerText.rectTransform, new Vector2(0.45f, 0f), new Vector2(1f, 0.44f), new Vector2(12f, -6f), new Vector2(-27f, -20f));
            ConfigureResponsiveHudText(_runTimerText, 9, 13);
            _runTimerText.gameObject.SetActive(false);

            // The Factory's tier and live amplifier bonus (OPEN_TOPICS 2.2). Built here because
            // this presenter already owns the HUD canvas and its font; the indicator hides itself
            // when there is nothing to report, so a run that ignores the Factory sees nothing.
            var indicatorHost = new GameObject("FactoryChainIndicatorHost");
            indicatorHost.transform.SetParent(rectTransform, false);
            indicatorHost.AddComponent<Corebound.UI.Gameplay.FactoryChainIndicatorPresenter>()
                .Build(rectTransform, ResolveFont());

            var bannerPanel = CreateImage("WaveBanner", rectTransform, _waveBannerSprite != null ? _waveBannerSprite : _statusPanelSprite, new Color(1f, 1f, 1f, 0.98f), ResolveBannerImageType());
            var bannerRect = bannerPanel.rectTransform;
            bannerRect.anchorMin = new Vector2(0.5f, 1f);
            bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.anchoredPosition = new Vector2(0f, -28f);
            bannerRect.sizeDelta = new Vector2(BannerWidth, BannerHeight);
            bannerRect.gameObject.SetActive(false);
            _bannerImage = bannerPanel;

            _bannerText = CreateText("WaveBannerText", bannerRect, 30, TextAnchor.MiddleCenter, new Color(0.1f, 0.12f, 0.16f, 0.95f));
            StretchToParent(_bannerText.rectTransform, Vector2.zero, Vector2.one, BannerTextInsetMin, BannerTextInsetMax);

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
                    _waveManager.FailureCauseLabel + ".\n" + ResolveRunEarningsLine() + "\n" + LocalizationService.Resolve("Confirm returns to the Main Menu.", "Bestätigen kehrt zum Hauptmenü zurück."));
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

            UpdateHealthBars();
            UpdateRunTimerReadout();

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
                        ? LocalizationService.Resolve("Shop | Confirm or click buys. Cancel or right-click skips.", "Shop | Bestätigen oder Klick kauft. Abbrechen oder Rechtsklick überspringt.")
                        : _waveManager.RequiresIntermissionTowerAction
                        ? LocalizationService.Resolve("Intermission | Optionally place or upgrade a tower. Confirm to continue.", "Zwischenphase | Optional einen Turm bauen oder verbessern. Bestätigen zum Fortfahren.")
                        : LocalizationService.Resolve("Intermission | ", "Zwischenphase | ") + _waveManager.IntermissionSecondsRemaining.ToString("0.0") + LocalizationService.Resolve("s or Confirm to continue.", "s oder Bestätigen zum Fortfahren.");

                    _stateText.text += ResolveFactoryStatusSuffix();
                    _stateText.text += ResolveLoadoutHint();
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

            // Escape is both UI Cancel (closes the loadout panel) and System Pause.
            if (Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.IsOpen || Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.LastToggleFrame == Time.frameCount)
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
        // reward screen closes). They are listed by LayoutPauseOverlay as a grid with each card's
        // tag icons to its left (playtest 2026-09-21: "use them as well in the pause menu left to
        // the current active perks"); the body text is only the instructions now.
        private string ResolvePauseBodyText()
        {
            return LocalizationService.Resolve(
                "Press Confirm to return to the Main Menu.\nPress Pause or Cancel to resume.",
                "Bestätigen kehrt zum Hauptmenü zurück.\nPause oder Abbrechen zum Fortsetzen.");
        }

        private const int PauseEffectColumns = 3;
        private const float PauseEffectRowHeight = 34f;
        private const float PauseEffectIconSize = 26f;
        private const int PauseEffectMaxIcons = 2;
        private const float PauseEffectsWidePanelWidth = 1240f;
        private const float PauseInstructionBandHeight = 76f;
        private const float PauseTitleBandHeight = 60f;
        private const float PauseTabBandHeight = 36f;
        private const float PauseDescriptionHeight = 64f;
        private const float PauseStatRowHeight = 24f;

        private static readonly Color PauseTextColor = new Color(0.1f, 0.12f, 0.16f, 0.96f);
        private static readonly Color PauseSelectedColor = new Color(0.55f, 0.32f, 0.02f, 1f);

        // Playtest 2026-09-22: the pause menu showed icons and names but not "what the perk is
        // doing", and there was nowhere to see overall numbers. Three tabs now: Perks (a grid; the
        // selected card's description below it; select with the arrows or by hovering with the
        // mouse), Player and Towers (every stat as base value with the bonus in brackets).
        // Previous/Next (Q / Tab, LB / RB) switch tab, like the build context.
        private enum PauseTab
        {
            Perks,
            Player,
            Towers,

            // B6 (spec review 2026-09-22): Settings.md promises settings from allowed in-game
            // menus. Volumes, aim assist and the input device are the ones worth changing without
            // leaving a run; resolution and fullscreen stay in the main menu, where a confirmation
            // flow can protect them.
            Settings
        }

        private readonly List<PauseEffectEntry> _pauseEffectEntries = new List<PauseEffectEntry>();

        // How many of the perk grid's entries are cards; everything at or after this index is a
        // relic, which is what lets one grid and one selection index cover both.
        private int _pauseEffectCards;
        private readonly List<StatLine> _pauseStatLines = new List<StatLine>();
        private RectTransform _pauseEffectsGrid;
        private Text _pauseTabText;
        private Text _pauseDescriptionText;
        private Text _pauseStatsLeft;
        private Text _pauseStatsRight;
        private PauseTab _pauseTab;
        private int _pauseSelectedEffect;
        private int _pauseEffectCount;
        private Vector2 _lastPausePointer;

        // The pause panel's 9-slice border is large (230/165/140 px), which leaves a 540x100 band -
        // room for one line of text, not a list. With content to show the panel grows to fit it,
        // and the bands (title, tabs, content, instructions) are laid out from the top and bottom
        // edges instead of the fixed offsets that assumed a 500 px panel.
        private void LayoutPauseOverlay(bool showPauseContent)
        {
            if (_pausePanel == null || _pauseTitleText == null || _pauseBodyText == null)
            {
                return;
            }

            var panelRect = _pausePanel.rectTransform;

            if (!showPauseContent)
            {
                panelRect.sizeDelta = new Vector2(PausePanelWidth, PausePanelHeight);
                StretchToParent(_pauseTitleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(FrameBorderHorizontal, 255f), new Vector2(-FrameBorderHorizontal, -FrameBorderTop));
                StretchToParent(_pauseBodyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(FrameBorderHorizontal, FrameBorderBottom), new Vector2(-FrameBorderHorizontal, -260f));

                if (_pauseEffectsGrid != null)
                {
                    _pauseEffectsGrid.gameObject.SetActive(false);
                }

                // The tab strip is a sibling of the grid, not a child, so it needs hiding on its
                // own - otherwise it stays on the Run Failed / Stage Complete overlays.
                if (_pauseTabText != null)
                {
                    _pauseTabText.gameObject.SetActive(false);
                }

                SetPauseButtonsVisible(false);
                _pauseBodyText.gameObject.SetActive(true);
                return;
            }

            EnsurePauseContentWidgets(panelRect);
            var contentHeight = _pauseTab == PauseTab.Perks
                ? LayoutPerksTab()
                : _pauseTab == PauseTab.Settings ? LayoutSettingsTab() : LayoutStatsTab();
            var height = FrameBorderTop + PauseTitleBandHeight + PauseTabBandHeight + contentHeight + PauseInstructionBandHeight + FrameBorderBottom;
            panelRect.sizeDelta = new Vector2(PauseEffectsWidePanelWidth, Mathf.Max(PausePanelHeight, height));

            StretchToParent(_pauseTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(FrameBorderHorizontal, -FrameBorderTop - PauseTitleBandHeight), new Vector2(-FrameBorderHorizontal, -FrameBorderTop + 20f));
            StretchToParent(_pauseBodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(FrameBorderHorizontal, FrameBorderBottom + 6f), new Vector2(-FrameBorderHorizontal, FrameBorderBottom + PauseInstructionBandHeight + 6f));

            var gridInset = FrameBorderHorizontal - 60f;
            var top = -FrameBorderTop - PauseTitleBandHeight;
            StretchToParent(_pauseTabText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(gridInset, top - PauseTabBandHeight), new Vector2(-gridInset, top));
            StretchToParent(_pauseEffectsGrid, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(gridInset, top - PauseTabBandHeight - contentHeight), new Vector2(-gridInset, top - PauseTabBandHeight));
            _pauseEffectsGrid.gameObject.SetActive(true);
            _pauseTabText.gameObject.SetActive(true);
            _pauseTabText.text = LocalizationService.Resolve("<size=15>(Q / Tab or LB / RB)</size>", "<size=15>(Q / Tab oder LB / RB)</size>");
            LayoutPauseTabLabels();

            // Resume and Main Menu as buttons in the instruction band; the old two-line
            // instruction is what they replace, so their labels carry the keys.
            _pauseBodyText.gameObject.SetActive(false);
            SetPauseButtonsVisible(true);
            PlacePauseButton(_pauseResumeButton, LocalizationService.Resolve("Resume   <size=15>(Pause / Cancel)</size>", "Weiter   <size=15>(Pause / Abbrechen)</size>"), -1f);
            PlacePauseButton(_pauseMenuButton, LocalizationService.Resolve("Main Menu   <size=15>(Confirm)</size>", "Hauptmenü   <size=15>(Bestätigen)</size>"), 1f);
        }

        // ---- Mouse on the gameplay overlays (playtest 2026-09-29: "check that I can click with
        // the mouse everywhere"). --------------------------------------------------------------
        //
        // The left mouse button is bound to PrimaryFire and nothing else, so no overlay here ever
        // saw a click: the pause tabs were one line of text, and Resume / Main Menu existed only
        // as a sentence about keys. The gameplay scene has no EventSystem (MainMenu owns the only
        // one), so this hit-tests directly, as UpdatePauseHover already does for the perks.
        //
        // Start, Run Failed and tutorial beats each have exactly one action, so a click anywhere
        // performs it - after a short grace, because these open in the middle of combat while the
        // same button is being used to fire.
        private const float OverlayClickGraceSeconds = 0.6f;
        private static readonly Color PauseButtonColor = new Color(0.22f, 0.17f, 0.11f, 0.92f);
        private static readonly Color PauseButtonHoverColor = new Color(0.55f, 0.33f, 0.08f, 0.96f);
        private readonly Text[] _pauseTabLabels = new Text[4];
        private Image _pauseResumeButton;
        private Image _pauseMenuButton;
        private float _overlayOpenedAt;

        private void UpdateOverlayClicks()
        {
            if (_overlayMode == OverlayMode.None)
            {
                return;
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;

            if (mouse == null)
            {
                return;
            }

            var pointer = mouse.position.ReadValue();

            if (_overlayMode == OverlayMode.Pause)
            {
                HighlightPauseButton(_pauseResumeButton, pointer);
                HighlightPauseButton(_pauseMenuButton, pointer);
            }

            var leftClick = mouse.leftButton.wasPressedThisFrame;
            var rightClick = mouse.rightButton.wasPressedThisFrame;

            if ((!leftClick && !rightClick) || Time.unscaledTime - _overlayOpenedAt < OverlayClickGraceSeconds)
            {
                return;
            }

            if (_overlayMode != OverlayMode.Pause)
            {
                if (leftClick)
                {
                    OnConfirmPressed();
                }

                return;
            }

            if (leftClick && HitsPauseButton(_pauseResumeButton, pointer))
            {
                OnCancelPressed();
                return;
            }

            if (leftClick && HitsPauseButton(_pauseMenuButton, pointer))
            {
                OnConfirmPressed();
                return;
            }

            for (var index = 0; leftClick && index < _pauseTabLabels.Length; index++)
            {
                var label = _pauseTabLabels[index];

                if (label != null && label.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(label.rectTransform, pointer, null))
                {
                    if ((int)_pauseTab != index)
                    {
                        _pauseTab = (PauseTab)index;
                        LayoutPauseOverlay(true);
                    }

                    return;
                }
            }

            if (_pauseTab == PauseTab.Settings)
            {
                ClickPauseSetting(pointer, leftClick ? 1 : -1);
            }
        }

        // The settings list is one multi-line Text, so the row under the pointer comes from the
        // text generator's own line boxes rather than an assumed line height.
        private void ClickPauseSetting(Vector2 pointer, int direction)
        {
            if (_pauseStatsLeft == null || _pauseSettingsCount == 0
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_pauseStatsLeft.rectTransform, pointer, null, out var local))
            {
                return;
            }

            var generator = _pauseStatsLeft.cachedTextGenerator;
            var scale = _pauseStatsLeft.pixelsPerUnit;

            for (var line = 0; line < generator.lineCount && line < _pauseSettingsCount; line++)
            {
                var info = generator.lines[line];
                var top = info.topY / scale;
                var bottom = top - info.height / scale;

                if (local.y > top || local.y < bottom)
                {
                    continue;
                }

                if (line != _pauseSettingsIndex)
                {
                    _pauseSettingsIndex = line;
                    LayoutPauseOverlay(true);
                    return;
                }

                AdjustPauseSetting(direction);
                return;
            }
        }

        private void LayoutPauseTabLabels()
        {
            var names = new[]
            {
                LocalizationService.Resolve("Perks", "Perks"),
                LocalizationService.Resolve("Player", "Spieler"),
                LocalizationService.Resolve("Towers", "Türme"),
                LocalizationService.Resolve("Settings", "Einstellungen"),
            };

            var widths = new float[names.Length];
            var total = 0f;

            for (var index = 0; index < names.Length; index++)
            {
                var active = index == (int)_pauseTab;
                _pauseTabLabels[index].text = active ? "<b><color=#8A5200>[" + names[index] + "]</color></b>" : names[index];
                widths[index] = _pauseTabLabels[index].preferredWidth + 10f;
                total += widths[index] + (index > 0 ? 36f : 0f);
            }

            // Centred in the band, as the one-string strip was.
            var x = (_pauseTabText.rectTransform.rect.width - total) * 0.5f;

            for (var index = 0; index < names.Length; index++)
            {
                var rect = _pauseTabLabels[index].rectTransform;
                rect.sizeDelta = new Vector2(widths[index], 0f);
                rect.anchoredPosition = new Vector2(x, 0f);
                x += widths[index] + 36f;
            }
        }

        private Image CreatePauseButton(string name, RectTransform parent)
        {
            var button = CreateImage(name, parent, null, PauseButtonColor, Image.Type.Simple);
            button.raycastTarget = false;
            var label = CreateText(name + "Label", button.rectTransform, 20, TextAnchor.MiddleCenter, new Color(0.98f, 0.92f, 0.8f, 1f));
            label.supportRichText = true;
            StretchToParent(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            button.gameObject.SetActive(false);
            return button;
        }

        // side: -1 left of centre, +1 right of centre, inside the instruction band.
        private void PlacePauseButton(Image button, string label, float side)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(320f, 50f);
            rect.anchoredPosition = new Vector2(side * 175f, FrameBorderBottom + 14f);
            button.GetComponentInChildren<Text>().text = label;
        }

        private void SetPauseButtonsVisible(bool visible)
        {
            if (_pauseResumeButton != null)
            {
                _pauseResumeButton.gameObject.SetActive(visible);
            }

            if (_pauseMenuButton != null)
            {
                _pauseMenuButton.gameObject.SetActive(visible);
            }
        }

        private static bool HitsPauseButton(Image button, Vector2 pointer)
        {
            return button != null && button.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(button.rectTransform, pointer, null);
        }

        private static void HighlightPauseButton(Image button, Vector2 pointer)
        {
            if (button != null && button.gameObject.activeInHierarchy)
            {
                button.color = HitsPauseButton(button, pointer) ? PauseButtonHoverColor : PauseButtonColor;
            }
        }

        private void EnsurePauseContentWidgets(RectTransform panelRect)
        {
            if (_pauseEffectsGrid != null)
            {
                return;
            }

            var gridObject = new GameObject("PauseEffectsGrid", typeof(RectTransform));
            _pauseEffectsGrid = gridObject.GetComponent<RectTransform>();
            _pauseEffectsGrid.SetParent(panelRect, false);

            _pauseTabText = CreateText("PauseTabs", panelRect, 20, TextAnchor.MiddleRight, PauseTextColor);
            _pauseTabText.supportRichText = true;

            for (var index = 0; index < _pauseTabLabels.Length; index++)
            {
                _pauseTabLabels[index] = CreateText("PauseTab" + index, _pauseTabText.rectTransform, 20, TextAnchor.MiddleLeft, PauseTextColor);
                _pauseTabLabels[index].supportRichText = true;
                var rect = _pauseTabLabels[index].rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
            }

            _pauseResumeButton = CreatePauseButton("PauseResumeButton", panelRect);
            _pauseMenuButton = CreatePauseButton("PauseMenuButton", panelRect);

            _pauseDescriptionText = CreateText("PauseDescription", _pauseEffectsGrid, 17, TextAnchor.UpperLeft, PauseTextColor);
            _pauseDescriptionText.supportRichText = true;
            _pauseDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _pauseDescriptionText.verticalOverflow = VerticalWrapMode.Overflow;

            _pauseStatsLeft = CreateStatsColumn("PauseStatsLeft", 0f, 0.5f);
            _pauseStatsRight = CreateStatsColumn("PauseStatsRight", 0.5f, 1f);
        }

        private Text CreateStatsColumn(string name, float xMin, float xMax)
        {
            var text = CreateText(name, _pauseEffectsGrid, 16, TextAnchor.UpperLeft, PauseTextColor);
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.1f;
            StretchToParent(text.rectTransform, new Vector2(xMin, 0f), new Vector2(xMax, 1f), new Vector2(10f, 0f), new Vector2(-10f, -6f));
            return text;
        }

        private float LayoutPerksTab()
        {
            _pauseStatsLeft.gameObject.SetActive(false);
            _pauseStatsRight.gameObject.SetActive(false);
            _pauseDescriptionText.gameObject.SetActive(true);

            // Playtest 2026-09-28: "I get a relic but I don't see it afterwards in any overview."
            // Relics were granted, saved and applied, and shown exactly once in a reveal that fades
            // after three seconds - after which the only evidence of owning one was the modifier it
            // silently contributed. From the player's side a card and a relic are the same thing (a
            // permanent effect picked up mid-run), so they share this grid: relics after cards, and
            // prefixed so the two stay tellable apart.
            var cards = _cardService != null ? _cardService.SelectedCards : null;
            var relics = _cardService != null ? _cardService.SelectedRelics : null;
            _pauseEffectCards = cards != null ? cards.Count : 0;
            _pauseEffectCount = _pauseEffectCards + (relics != null ? relics.Count : 0);
            _pauseSelectedEffect = Mathf.Clamp(_pauseSelectedEffect, 0, Mathf.Max(0, _pauseEffectCount - 1));
            var rows = Mathf.Max(1, Mathf.CeilToInt(_pauseEffectCount / (float)PauseEffectColumns));
            var gridHeight = rows * PauseEffectRowHeight + 16f;
            var columnWidth = (PauseEffectsWidePanelWidth - 2f * (FrameBorderHorizontal - 60f)) / PauseEffectColumns;

            while (_pauseEffectEntries.Count < _pauseEffectCount)
            {
                _pauseEffectEntries.Add(CreatePauseEffectEntry(_pauseEffectsGrid, _pauseEffectEntries.Count));
            }

            for (var index = 0; index < _pauseEffectEntries.Count; index++)
            {
                var entry = _pauseEffectEntries[index];
                var isRelic = index >= _pauseEffectCards;
                var card = !isRelic && index < _pauseEffectCards ? cards[index] : null;
                var relic = isRelic && relics != null && index - _pauseEffectCards < relics.Count
                    ? relics[index - _pauseEffectCards]
                    : null;
                entry.Root.gameObject.SetActive(card != null || relic != null);

                if (card == null && relic == null)
                {
                    continue;
                }

                var column = index % PauseEffectColumns;
                var row = index / PauseEffectColumns;
                entry.Root.anchorMin = new Vector2(0f, 1f);
                entry.Root.anchorMax = new Vector2(0f, 1f);
                entry.Root.pivot = new Vector2(0f, 1f);
                entry.Root.sizeDelta = new Vector2(columnWidth - 8f, PauseEffectRowHeight);
                entry.Root.anchoredPosition = new Vector2(column * columnWidth, -8f - row * PauseEffectRowHeight);

                // Only cards carry tags; a relic row simply shows no icons.
                var tags = card != null ? card.Tags : null;
                var iconCount = 0;

                for (var iconIndex = 0; iconIndex < entry.Icons.Length; iconIndex++)
                {
                    var sprite = tags != null && iconIndex < tags.Length ? CardTagIconAtlas.ResolveIcon(tags[iconIndex]) : null;
                    entry.Icons[iconIndex].sprite = sprite;
                    entry.Icons[iconIndex].gameObject.SetActive(sprite != null);

                    if (sprite != null)
                    {
                        entry.Icons[iconIndex].rectTransform.anchoredPosition = new Vector2(iconCount * (PauseEffectIconSize + 2f), 0f);
                        iconCount++;
                    }
                }

                // The name always starts at the same x so a column of names stays aligned no
                // matter how many icons each card has.
                var textLeft = PauseEffectMaxIcons * (PauseEffectIconSize + 2f) + 6f;
                entry.Label.rectTransform.offsetMin = new Vector2(textLeft, 0f);
                entry.Label.rectTransform.offsetMax = Vector2.zero;
                var selected = index == _pauseSelectedEffect;
                var entryName = card != null ? card.DisplayName : "* " + relic.DisplayName;
                entry.Label.text = selected ? "<b>" + entryName + "</b>" : entryName;
                entry.Label.supportRichText = true;
                entry.Label.color = selected ? PauseSelectedColor : PauseTextColor;
            }

            var descriptionTop = gridHeight;
            StretchToParent(_pauseDescriptionText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(10f, -descriptionTop - PauseDescriptionHeight), new Vector2(-10f, -descriptionTop));

            if (_pauseEffectCount == 0)
            {
                _pauseDescriptionText.text = LocalizationService.Resolve(
                    "No perks yet - card rewards and boss relics add them.",
                    "Noch keine Perks - Kartenbelohnungen und Boss-Relikte fügen sie hinzu.");
            }
            else
            {
                var selectedIsRelic = _pauseSelectedEffect >= _pauseEffectCards;
                var selectedName = selectedIsRelic
                    ? relics[_pauseSelectedEffect - _pauseEffectCards].DisplayName + LocalizationService.Resolve(" (Relic)", " (Relikt)")
                    : cards[_pauseSelectedEffect].DisplayName;
                var selectedDescription = selectedIsRelic
                    ? relics[_pauseSelectedEffect - _pauseEffectCards].Description
                    : cards[_pauseSelectedEffect].Description;
                _pauseDescriptionText.text = "<b>" + selectedName + "</b>  ·  " + selectedDescription
                    + LocalizationService.Resolve("\n<size=14>Arrows or mouse to select a perk.</size>", "\n<size=14>Pfeiltasten oder Maus zum Auswählen.</size>");
            }

            return gridHeight + PauseDescriptionHeight;
        }

        // The in-run settings rows. Left/Right changes the selected row's value and commits it,
        // so a change survives the run the same way the Settings screen's does.
        private float LayoutSettingsTab()
        {
            for (var index = 0; index < _pauseEffectEntries.Count; index++)
            {
                _pauseEffectEntries[index].Root.gameObject.SetActive(false);
            }

            _pauseDescriptionText.gameObject.SetActive(false);
            _pauseStatsLeft.gameObject.SetActive(true);
            _pauseStatsRight.gameObject.SetActive(false);

            var manager = ResolveSettingsManager();
            var options = ResolvePauseSettingsOptions();
            _pauseSettingsCount = options.Count;
            _pauseSettingsIndex = Mathf.Clamp(_pauseSettingsIndex, 0, Mathf.Max(0, options.Count - 1));
            var builder = new System.Text.StringBuilder(256);

            if (manager == null || options.Count == 0)
            {
                builder.Append(LocalizationService.Resolve("Settings are unavailable in this scene.", "Einstellungen sind in dieser Szene nicht verfügbar."));
            }
            else
            {
                for (var index = 0; index < options.Count; index++)
                {
                    var option = options[index];
                    builder.Append(index == _pauseSettingsIndex ? "<b>> " : "   ");
                    builder.Append(option.Label);
                    builder.Append(":  ");
                    builder.Append(manager.GetDisplayValue(manager.RuntimeSettings, option));
                    builder.Append(index == _pauseSettingsIndex ? "</b>" : string.Empty);
                    builder.Append('\n');
                }

                builder.Append('\n');
                builder.Append(LocalizationService.Resolve("Left / Right changes the selected setting.", "Links / Rechts ändert die gewählte Einstellung."));
            }

            _pauseStatsLeft.text = builder.ToString();
            return (Mathf.Max(1, _pauseSettingsCount) + 3) * PauseStatRowHeight + 12f;
        }

        private int _pauseSettingsIndex;
        private int _pauseSettingsCount;
        private readonly List<Corebound.Settings.SettingsDefinitionData.OptionDefinition> _pauseSettingsOptions =
            new List<Corebound.Settings.SettingsDefinitionData.OptionDefinition>();

        private Corebound.Settings.SettingsManager ResolveSettingsManager()
        {
            if (_settingsManager == null)
            {
                _settingsManager = FindFirstObjectByType<Corebound.Settings.SettingsManager>();
            }

            return _settingsManager;
        }

        // Only the options that are safe to change mid-run.
        private List<Corebound.Settings.SettingsDefinitionData.OptionDefinition> ResolvePauseSettingsOptions()
        {
            _pauseSettingsOptions.Clear();
            var manager = ResolveSettingsManager();
            var definition = manager != null ? manager.DefinitionData : null;

            if (definition == null || definition.Categories == null)
            {
                return _pauseSettingsOptions;
            }

            for (var categoryIndex = 0; categoryIndex < definition.Categories.Length; categoryIndex++)
            {
                var category = definition.Categories[categoryIndex];

                if (category == null || category.Options == null)
                {
                    continue;
                }

                for (var optionIndex = 0; optionIndex < category.Options.Length; optionIndex++)
                {
                    var option = category.Options[optionIndex];

                    switch (option.Key)
                    {
                        case Corebound.Settings.SettingsOptionKey.MusicVolume:
                        case Corebound.Settings.SettingsOptionKey.SfxVolume:
                        case Corebound.Settings.SettingsOptionKey.UiVolume:
                        case Corebound.Settings.SettingsOptionKey.ControllerAimAssistStrength:
                        case Corebound.Settings.SettingsOptionKey.InputDevice:
                        // Playtest 2026-09-28: "Pause -> Settings should have HUD scaling". The
                        // option already existed and was reachable only from the main menu, which
                        // is the one place you cannot see the HUD you are scaling. Adjusting it
                        // mid-run with the arena behind you is the only way to judge it.
                        case Corebound.Settings.SettingsOptionKey.HudScale:
                        // The run timer and XP bar share this toggle (run-timer D2, levelling L4).
                        case Corebound.Settings.SettingsOptionKey.ShowRunTimer:
                            _pauseSettingsOptions.Add(option);
                            break;
                    }
                }
            }

            return _pauseSettingsOptions;
        }

        private void AdjustPauseSetting(int direction)
        {
            var manager = ResolveSettingsManager();
            var options = ResolvePauseSettingsOptions();

            if (manager == null || _pauseSettingsIndex < 0 || _pauseSettingsIndex >= options.Count)
            {
                return;
            }

            var candidate = manager.CreateWorkingCopy();
            manager.AdjustValue(candidate, options[_pauseSettingsIndex], direction);
            manager.Commit(candidate);
            LayoutPauseOverlay(true);
        }

        private float LayoutStatsTab()
        {
            for (var index = 0; index < _pauseEffectEntries.Count; index++)
            {
                _pauseEffectEntries[index].Root.gameObject.SetActive(false);
            }

            _pauseDescriptionText.gameObject.SetActive(false);
            _pauseStatsLeft.gameObject.SetActive(true);
            _pauseStatsRight.gameObject.SetActive(true);

            if (_pauseTab == PauseTab.Player)
            {
                if (_playerCombatController != null)
                {
                    _playerCombatController.CollectStatLines(_pauseStatLines);
                }
                else
                {
                    _pauseStatLines.Clear();
                }
            }
            else if (_towerService != null)
            {
                _towerService.CollectStatLines(_pauseStatLines);
            }
            else
            {
                _pauseStatLines.Clear();
            }

            // Two columns, split at a header where possible so a weapon's block is never cut.
            var split = _pauseStatLines.Count / 2;

            for (var index = split; index < _pauseStatLines.Count; index++)
            {
                if (_pauseStatLines[index].IsHeader)
                {
                    split = index;
                    break;
                }
            }

            _pauseStatsLeft.text = FormatStatLines(0, split);
            _pauseStatsRight.text = FormatStatLines(split, _pauseStatLines.Count);
            var rows = Mathf.Max(split, _pauseStatLines.Count - split) + 1;
            return rows * PauseStatRowHeight + 12f;
        }

        private string FormatStatLines(int from, int to)
        {
            var builder = new System.Text.StringBuilder(512);

            for (var index = from; index < to; index++)
            {
                var line = _pauseStatLines[index];
                builder.Append(line.IsHeader
                    ? (index > from ? "\n" : string.Empty) + "<b>" + line.Label + "</b>"
                    : line.Label + (line.Text.Length > 0 ? ":  " + line.Text : string.Empty));
                builder.Append('\n');
            }

            return builder.ToString();
        }

        private void OnPausePreviousPressed()
        {
            SwitchPauseTab(-1);
        }

        private void OnPauseNextPressed()
        {
            SwitchPauseTab(1);
        }

        private void SwitchPauseTab(int direction)
        {
            if (_overlayMode != OverlayMode.Pause)
            {
                return;
            }

            _pauseTab = (PauseTab)(((int)_pauseTab + direction + 4) % 4);
            LayoutPauseOverlay(true);
        }

        private void OnPauseNavigate(Vector2 input)
        {
            if (_overlayMode != OverlayMode.Pause)
            {
                return;
            }

            if (_pauseTab == PauseTab.Settings)
            {
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                {
                    if (Mathf.Abs(input.x) >= 0.5f)
                    {
                        AdjustPauseSetting(input.x > 0f ? 1 : -1);
                    }

                    return;
                }

                if (Mathf.Abs(input.y) >= 0.5f && _pauseSettingsCount > 0)
                {
                    _pauseSettingsIndex = Mathf.Clamp(_pauseSettingsIndex + (input.y > 0f ? -1 : 1), 0, _pauseSettingsCount - 1);
                    LayoutPauseOverlay(true);
                }

                return;
            }

            if (_pauseTab != PauseTab.Perks || _pauseEffectCount == 0)
            {
                return;
            }

            var step = Mathf.Abs(input.x) >= Mathf.Abs(input.y)
                ? (input.x > 0.5f ? 1 : input.x < -0.5f ? -1 : 0)
                : (input.y > 0.5f ? -PauseEffectColumns : input.y < -0.5f ? PauseEffectColumns : 0);

            if (step == 0)
            {
                return;
            }

            _pauseSelectedEffect = Mathf.Clamp(_pauseSelectedEffect + step, 0, _pauseEffectCount - 1);
            LayoutPauseOverlay(true);
        }

        // Mouse hover selects a perk. The gameplay scene has no EventSystem (MainMenu owns the only
        // one), so this hit-tests the entry rects directly - and it keeps working at timeScale 0.
        private void UpdatePauseHover()
        {
            if (_overlayMode != OverlayMode.Pause || _pauseTab != PauseTab.Perks || _pauseEffectCount == 0)
            {
                return;
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;

            if (mouse == null)
            {
                return;
            }

            var pointer = mouse.position.ReadValue();

            if ((pointer - _lastPausePointer).sqrMagnitude < 1f)
            {
                return;
            }

            _lastPausePointer = pointer;

            for (var index = 0; index < _pauseEffectCount && index < _pauseEffectEntries.Count; index++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_pauseEffectEntries[index].Root, pointer, null))
                {
                    if (index != _pauseSelectedEffect)
                    {
                        _pauseSelectedEffect = index;
                        LayoutPauseOverlay(true);
                    }

                    return;
                }
            }
        }

        private PauseEffectEntry CreatePauseEffectEntry(RectTransform parent, int index)
        {
            var rootObject = new GameObject("PauseEffect" + index, typeof(RectTransform));
            var root = rootObject.GetComponent<RectTransform>();
            root.SetParent(parent, false);

            var icons = new Image[PauseEffectMaxIcons];

            for (var iconIndex = 0; iconIndex < icons.Length; iconIndex++)
            {
                var icon = CreateImage("TagIcon" + iconIndex, root, null, Color.white, Image.Type.Simple);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var iconRect = icon.rectTransform;
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.sizeDelta = new Vector2(PauseEffectIconSize, PauseEffectIconSize);
                icon.gameObject.SetActive(false);
                icons[iconIndex] = icon;
            }

            var label = CreateText("Name", root, 18, TextAnchor.MiddleLeft, PauseTextColor);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            ConfigureResponsiveHudText(label, 12, 18);

            return new PauseEffectEntry(root, icons, label);
        }

        private sealed class PauseEffectEntry
        {
            public PauseEffectEntry(RectTransform root, Image[] icons, Text label)
            {
                Root = root;
                Icons = icons;
                Label = label;
            }

            public RectTransform Root { get; }

            public Image[] Icons { get; }

            public Text Label { get; }
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

        // The XP a wave pays, on the banner that announces it (playtest 2026-09-29: "I want to get
        // EXP per wave") - the one moment the player is already looking at the result of the wave.
        private void OnWaveCompleted(WaveProgressEventArgs eventArgs)
        {
            var experience = Corebound.MetaProgression.MetaRewardLedger.ResolveWaveExperience(_waveManager != null ? _waveManager.StageIndex : 1);
            ShowBanner(LocalizationService.Resolve("Wave Clear", "Welle geschafft") + "   +" + experience + " XP", new Color(0.84f, 0.97f, 0.9f, 0.98f));
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

            // Playtest 2026-09-28: "wave banner is behind the HUD". Draw order inside a canvas is
            // sibling order, and the banner is created before the HUD panels around it, so it was
            // permanently underneath them. Raised on show rather than at build time because other
            // things (the pause overlay) also claim the top and must keep it.
            _bannerImage.rectTransform.SetAsLastSibling();
            _bannerTimer = BannerDisplaySeconds;
        }

        private void HideBanner()
        {
            if (_bannerImage != null)
            {
                _bannerImage.gameObject.SetActive(false);
            }
        }

        // Everything a run pays is banked at the moment it is earned, so a failed run keeps it - the
        // screen says so. XP comes first: it is what every run earns, and the old line, which
        // counted only Skill Points and Alloy, told a player who died at the Stage 1 boss that the
        // run had earned "nothing" (playtest 2026-09-29).
        private static string ResolveRunEarningsLine()
        {
            var experience = Corebound.MetaProgression.MetaRewardLedger.RunExperience;
            var levels = Corebound.MetaProgression.MetaRewardLedger.RunLevelsGained;
            var skillPoints = Corebound.MetaProgression.MetaRewardLedger.RunSkillPoints;
            var alloy = Corebound.MetaProgression.MetaRewardLedger.RunAlloy;

            if (experience <= 0 && skillPoints <= 0 && alloy <= 0)
            {
                return LocalizationService.Resolve(
                    "Earned this run: nothing yet - every wave cleared pays XP, and bosses pay the most.",
                    "In diesem Run verdient: noch nichts - jede geschaffte Welle gibt EP, Bosse am meisten.");
            }

            return LocalizationService.Resolve("Earned this run (kept): ", "In diesem Run verdient (bleibt erhalten): ")
                + "+" + experience + LocalizationService.Resolve(" XP", " EP")
                + (levels > 0 ? LocalizationService.Resolve("  (+" + levels + (levels == 1 ? " level)" : " levels)"), "  (+" + levels + (levels == 1 ? " Stufe)" : " Stufen)")) : string.Empty)
                + "   +" + skillPoints + LocalizationService.Resolve(" Skill Points", " Fertigkeitspunkte")
                + "   +" + alloy + LocalizationService.Resolve(" Alloy", " Legierung");
        }

        // Playtest 2026-09-22: "unclear how to enter the weapon upgrade menu; figured out it is bound
        // to I - make this bind visible." Shown in every Intermission line, with the active device's
        // binding (I on keyboard, the Select/View button on a pad).
        private string ResolveLoadoutHint()
        {
            var reader = InputReader as CoreboundInputReader;
            var key = reader != null
                ? reader.GetBindingDisplayString("System", "Loadout", reader.IsUsingGamepad ? "Gamepad" : "Keyboard&Mouse")
                : string.Empty;

            if (string.IsNullOrWhiteSpace(key))
            {
                key = reader != null && reader.IsUsingGamepad ? "Select" : "I";
            }

            return LocalizationService.Resolve("  |  " + key.ToUpperInvariant() + ": Weapons", "  |  " + key.ToUpperInvariant() + ": Waffen");
        }

        private string ResolveCurrencyText()
        {
            return _resourceWalletService != null
                ? _resourceWalletService.DisplayLabel + ": " + _resourceWalletService.CurrentAmount
                : LocalizationService.Resolve("Resources: --", "Ressourcen: --");
        }

        // Open Topics 3.3: the bars themselves live in HudHealthBars; this only feeds them.
        private HudHealthBars _healthBars;

        private void UpdateHealthBars()
        {
            if (_healthBars == null || !_healthBars.IsAlive)
            {
                _healthBars = HudHealthBars.TryCreate(_playerHealthText, _coreHealthText);

                if (_healthBars == null)
                {
                    return;
                }
            }

            _healthBars.Show(
                _playerCombatController != null ? _playerCombatController.CurrentHealthNormalized : 1f,
                ResolveCoreHealthNormalized());
        }

        private Text _runTimerText;
        private Corebound.Gameplay.Waves.RunTimerService _runTimerService;

        // One line: elapsed unpaused time, then the profile level and its XP progress. Active time
        // rather than wall clock, because a clock that ticks while the game is paused feels broken;
        // both are still stored, and the run-end record carries the pair.
        private void UpdateRunTimerReadout()
        {
            if (_runTimerText == null)
            {
                return;
            }

            var manager = ResolveSettingsManager();
            var show = manager != null && manager.RuntimeSettings != null && manager.RuntimeSettings.ShowRunTimer;

            if (_runTimerText.gameObject.activeSelf != show)
            {
                _runTimerText.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            if (_runTimerService == null)
            {
                _runTimerService = FindFirstObjectByType<Corebound.Gameplay.Waves.RunTimerService>();
            }

            var elapsed = _runTimerService != null
                ? Corebound.Gameplay.Waves.RunTimerService.Format(_runTimerService.ElapsedActiveSeconds)
                : "0:00";

            // Read live from the save, not from RunProgressionSnapshot: that snapshot is frozen at
            // run start by design, and the level is the one thing here that moves during a run.
            // LoadMetaProgressionSaveData returns the manager's cached instance, so this is a field
            // read rather than a file read.
            var saveManager = _waveManager != null ? _waveManager.SaveManagerSource : null;
            var definition = _waveManager != null ? _waveManager.MetaProgressionDefinition : null;
            var metaSaveData = saveManager != null && definition != null
                ? saveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData())
                : null;

            if (metaSaveData == null)
            {
                _runTimerText.text = elapsed;
                return;
            }

            metaSaveData.EnsureLevelFloor();
            // Past the cap the bar is the next Paragon level (levelling v3).
            _runTimerText.text = elapsed + (metaSaveData.IsAtLevelCap ? "   Paragon " + metaSaveData.ParagonLevel : "   Lv " + metaSaveData.Level)
                + "  " + metaSaveData.Experience + "/" + metaSaveData.ExperienceForNextLevel;
        }

        private string ResolvePlayerHealthText()
        {
            return _playerCombatController != null
                ? LocalizationService.Resolve("Player ", "Spieler ") + (_playerCombatController.CurrentHealthNormalized * 100f).ToString("0") + "%"
                : LocalizationService.Resolve("Player --", "Spieler --");
        }

        private float ResolveCoreHealthNormalized()
        {
            return _coreHitReceiver != null ? _coreHitReceiver.CurrentHealthNormalized : 1f;
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

        // Playtest 2026-09-21: "HUD information are still not consistent there. I started at stage 5
        // and they are not visible."
        //
        // The procedural fallback HUD is built on this component's own GameObject when HUDCanvas has
        // not arrived yet. Once the authored HUD does arrive, TryBindAuthoredHud repoints every text
        // field at the authored labels - but the fallback's own StatusPanel and its now-orphaned
        // child texts were left in the scene, in the same screen corner, frozen at whatever they last
        // displayed. The player sees that stale panel; every diagnostic that reads the authored
        // labels sees correct values. Both are true at once, which is why this read as "sporadic".
        //
        // Starting on a later stage through the debug stage-select makes it far more likely, because
        // the extra save-resolution work delays HUDCanvas past the frame this presenter first runs.
        private void DiscardProceduralFallbackHud()
        {
            if (!_builtProceduralFallback)
            {
                return;
            }

            _builtProceduralFallback = false;

            var fallbackPanel = transform.Find("StatusPanel");

            if (fallbackPanel != null)
            {
                // Renamed and deactivated before Destroy: Destroy is deferred to end of frame and
                // the object stays findable by name until then, which has already caused a rebuild
                // to parent new content under a doomed object once in this project.
                fallbackPanel.name = "StatusPanel_Discarded";
                fallbackPanel.gameObject.SetActive(false);
                Destroy(fallbackPanel.gameObject);
            }

            // The fallback also added a Canvas to this GameObject. Leaving it enabled keeps an empty
            // screen-space canvas alive on CombatRuntime for the rest of the run.
            var localCanvas = gameObject.GetComponent<Canvas>();

            if (localCanvas != null && _canvas != localCanvas)
            {
                localCanvas.enabled = false;
            }
        }

        // Two art requests from the 2026-09-21 playtest, both additive so they respect the rule
        // that scene-authored canvas layout is never reasserted by code:
        //
        //   "wave_banner.png is not in use for the WAVE text. I want wave_banner.png"
        //   "resource_primary_icon.png - use it for the resource"
        //
        // Both are inserted as siblings of the label they decorate, copying that label's rect rather
        // than positioning themselves, and pushed behind it in the hierarchy so the text stays on
        // top. If the authored layout moves, they move with it.
        // The Factory's whole payout was invisible. An Amplifier that quietly multiplies the
        // chain, and a chain that quietly climbs tiers, are both exactly the kind of system vision
        // Section 5.5 says the player must be able to *recognise* is working - and neither had any
        // presence on screen at all. A transient toast does not satisfy that either, because it is
        // gone by the time the player looks.
        //
        // Appended to the Intermission line rather than given its own widget: the Intermission is
        // the only moment the player is not under pressure and is already reading this text, and a
        // new HUD element would cost readability during combat for information that is only
        // actionable between waves.
        private string ResolveFactoryStatusSuffix()
        {
            if (_factoryService == null)
            {
                _factoryService = FindFirstObjectByType<Corebound.Gameplay.Factory.FactoryService>();
            }

            if (_factoryService == null)
            {
                return string.Empty;
            }

            var summary = _factoryService.DescribeActiveProduction();

            return string.IsNullOrEmpty(summary) ? string.Empty : "  |  " + summary;
        }

        private void EnsureHudDecorations()
        {
            EnsureWaveBannerBacking();
            EnsureCurrencyIcon();
        }

        private void EnsureWaveBannerBacking()
        {
            if (_waveText == null || _waveBannerSprite == null)
            {
                return;
            }

            var parent = _waveText.rectTransform.parent as RectTransform;

            if (parent == null || parent.Find(WaveBannerBackingName) != null)
            {
                return;
            }

            // Retired 2026-09-22: 9-slicing wave_banner.png behind the one-line HUD wave text had
            // the same squash problem as the announcement banner (end caps only, no frame), and at
            // that size the art cannot be shown whole. The art now lives on the announcement banner.
            if (_waveBannerSprite != null)
            {
                return;
            }

            var backing = CreateImage(WaveBannerBackingName, parent, _waveBannerSprite, new Color(1f, 1f, 1f, 0.92f), ResolvePanelType(_waveBannerSprite));
            var source = _waveText.rectTransform;
            var rect = backing.rectTransform;

            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.anchoredPosition = source.anchoredPosition;

            // A little wider and taller than the text it sits behind, so the banner frames the label
            // instead of being clipped to it.
            rect.sizeDelta = source.sizeDelta + new Vector2(18f, 6f);
            rect.offsetMin = source.offsetMin - new Vector2(9f, 3f);
            rect.offsetMax = source.offsetMax + new Vector2(9f, 3f);

            backing.raycastTarget = false;

            // Behind the text, and behind it specifically rather than at index 0, so it does not
            // slide under an unrelated sibling that is meant to be further back.
            rect.SetSiblingIndex(source.GetSiblingIndex());
        }

        private void EnsureCurrencyIcon()
        {
            if (_currencyText == null)
            {
                return;
            }

            var parent = _currencyText.rectTransform.parent as RectTransform;

            if (parent == null || parent.Find(CurrencyIconName) != null)
            {
                return;
            }

            var sprite = DebugSpriteFactory.LoadProjectSprite(CurrencyIconPath);

            if (sprite == null)
            {
                return;
            }

            var icon = CreateImage(CurrencyIconName, parent, sprite, Color.white, Image.Type.Simple);
            var source = _currencyText.rectTransform;
            var rect = icon.rectTransform;

            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMin;
            rect.pivot = new Vector2(1f, 0.5f);

            // Sits immediately left of the currency label, vertically centred on it.
            rect.anchoredPosition = source.anchoredPosition + new Vector2(-4f, 0f);
            rect.sizeDelta = new Vector2(CurrencyIconSize, CurrencyIconSize);

            icon.preserveAspect = true;
            icon.raycastTarget = false;
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
                    var bannerPanel = CreateImage("WaveBanner", overlayParent, _waveBannerSprite != null ? _waveBannerSprite : _statusPanelSprite, new Color(1f, 1f, 1f, 0.98f), ResolveBannerImageType());
                    var bannerRect = bannerPanel.rectTransform;
                    bannerRect.anchorMin = new Vector2(0.5f, 1f);
                    bannerRect.anchorMax = new Vector2(0.5f, 1f);
                    bannerRect.pivot = new Vector2(0.5f, 1f);
                    bannerRect.anchoredPosition = new Vector2(0f, -28f);
                    bannerRect.sizeDelta = new Vector2(BannerWidth, BannerHeight);
                    _bannerImage = bannerPanel;

                    _bannerText = CreateText("WaveBannerText", bannerRect, 30, TextAnchor.MiddleCenter, new Color(0.1f, 0.12f, 0.16f, 0.95f));
                    StretchToParent(_bannerText.rectTransform, Vector2.zero, Vector2.one, BannerTextInsetMin, BannerTextInsetMax);
                }

                // Re-apply the sprite/layout unconditionally, even when reusing an existing banner
                // found by name above - a banner Image left over from before the runtime sprites
                // loaded correctly would otherwise keep a stale/null sprite forever (rendering as
                // a blank white rectangle) since the create-new branch above only runs when no
                // existing banner was found at all.
                var reusedBannerSprite = _waveBannerSprite != null ? _waveBannerSprite : _statusPanelSprite;
                _bannerImage.sprite = reusedBannerSprite;
                _bannerImage.type = ResolveBannerImageType();
                _bannerImage.preserveAspect = _waveBannerSprite != null;
                var reusedBannerRect = _bannerImage.rectTransform;
                reusedBannerRect.anchorMin = new Vector2(0.5f, 1f);
                reusedBannerRect.anchorMax = new Vector2(0.5f, 1f);
                reusedBannerRect.pivot = new Vector2(0.5f, 1f);
                reusedBannerRect.anchoredPosition = new Vector2(0f, -28f);
                reusedBannerRect.sizeDelta = new Vector2(BannerWidth, BannerHeight);

                if (_bannerText != null)
                {
                    StretchToParent(_bannerText.rectTransform, Vector2.zero, Vector2.one, BannerTextInsetMin, BannerTextInsetMax);
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
            _overlayOpenedAt = Time.unscaledTime;

            // Ownership of the freeze lives in GameplayTimeScaleGuard now (god-object refactor
            // step 1). The assignment used to sit here bare, a hundred lines from its matching
            // restore, with every overlay path able to route between them.
            GameplayTimeScaleGuard.TryAcquire(this);
            LayoutPauseOverlay(overlayMode == OverlayMode.Pause);

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
            GameplayTimeScaleGuard.Release(this);

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
            _waveBannerSprite = _waveBannerTexture != null && _waveBannerTexture.width >= WaveBannerArtRect.xMax && _waveBannerTexture.height >= WaveBannerArtRect.yMax
                ? CreateRuntimeSpriteRect(_waveBannerTexture, WaveBannerArtRect, Vector4.zero)
                : CreateRuntimeSprite(_waveBannerTexture, ResolveWaveBannerBorder());
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

        // The wave banner art is shown whole; only the status-panel fallback is a 9-slice frame.
        private Image.Type ResolveBannerImageType()
        {
            return _waveBannerSprite != null ? Image.Type.Simple : ResolvePanelType(_statusPanelSprite);
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
            var renderer = child.gameObject.GetOrAdd<CanvasRenderer>();
            var image = child.gameObject.GetOrAdd<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private Text EnsureEditorText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax, TextAnchor alignment)
        {
            var child = EnsureEditorUiChild(parent, name, anchorMin, anchorMax, pivot, Vector2.zero, Vector2.zero);
            StretchToParent(child, anchorMin, anchorMax, offsetMin, offsetMax);
            var renderer = child.gameObject.GetOrAdd<CanvasRenderer>();
            var text = child.gameObject.GetOrAdd<Text>();
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

