using System.Collections.Generic;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Player;
using Corebound.Gameplay.Resources;
using Corebound.Gameplay.Towers;
using Corebound.Gameplay.Waves;
using Corebound.MetaProgression;
using Corebound.SaveSystem;
using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.Gameplay.Tutorial
{
    // Runs the guided first stage Tutorial.md calls for: it reuses the exact same systems and
    // scene as a normal run (no new core mechanic), only gates on RunSaveData.IsTutorialRun and
    // layers a lightweight step prompt + completion tracking on top of events those systems
    // already raise. Completing every mandatory step sets the persistent HasSeenTutorial flag
    // directly through the save domain, mirroring how gameplay code elsewhere already reaches
    // Meta Progression state without requiring the MainMenu-only MetaProgressionService.
    public sealed class TutorialService : MonoBehaviour
    {
        [SerializeField] private TutorialSequenceData _sequenceData;
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerCombatController _playerCombatController;
        [SerializeField] private ResourceWalletService _resourceWalletService;
        [SerializeField] private TowerService _towerService;
        [SerializeField] private CardService _cardService;

        private readonly HashSet<TutorialStepType> _completedSteps = new HashSet<TutorialStepType>();

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private bool _isActive;
        private bool _isComplete;
        private GameObject _promptRoot;
        private Text _promptText;
        // Index into _sequenceData.Steps of the next step a Blocking step is allowed to complete
        // as. Advances only past steps already completed (including any non-blocking ones finished
        // out of order), so a Blocking step's event is ignored until every step ahead of it in the
        // authored order is done.
        private int _currentStepIndex;

        private WaveStatePresenter _waveStatePresenter;
        private readonly HashSet<int> _shownBeatStepIndices = new HashSet<int>();
        private bool _isBeatShowing;

        // A blocking beat swallows Confirm until it is dismissed; the autopilot needs to see that
        // to get past it (it otherwise waits out the whole run cap in front of the first beat).
        public static bool IsBeatShowing { get; private set; }

        private void Awake()
        {
            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;
            _isActive = runSaveData != null && runSaveData.IsTutorialRun && _sequenceData != null && _sequenceData.Steps != null && _sequenceData.Steps.Length > 0;
            Corebound.Gameplay.Shared.SpriteSheetData.Register(_sequenceData != null ? _sequenceData.GlyphSheet : null);

            if (!_isActive)
            {
                return;
            }

            BuildPromptUi();
            RefreshPrompt();
        }

        // Deferred to Start() (not Awake()) so WaveStatePresenter - a sibling component - has
        // already finished its own Awake()/OnEnable() setup (its pause panel widgets in particular)
        // before this ever calls into it; Unity runs every object's Awake() and OnEnable() across
        // the whole scene before any Start(). See the codebase's established Start()-deferred
        // rebinding pattern used by CardRewardPresenter/ShopPresenter for the same class of issue.
        private void Start()
        {
            if (!_isActive)
            {
                return;
            }

            if (_waveStatePresenter == null)
            {
                _waveStatePresenter = GetComponent<WaveStatePresenter>();
            }

            TryShowBeatForCurrentStep();
        }

        // Retries showing the current step's paused explanation beat every frame until it
        // succeeds. Needed because WaveStatePresenter's own "Wave 1 | Confirm begins the
        // encounter" overlay may win the very first attempt (exact Awake/OnEnable/Start ordering
        // between sibling components isn't something this class should depend on) - once the
        // player dismisses whichever overlay got there first, the next tick opens this one.
        private void Update()
        {
            // B7: opening the loadout panel completes its step; watched here rather than in the
            // panel, so tutorial logic stays out of gameplay code.
            if (_isActive && Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.IsOpen)
            {
                MarkStepComplete(TutorialStepType.WeaponLoadout);
            }

            if (!_isActive || _isBeatShowing)
            {
                return;
            }

            TryShowBeatForCurrentStep();
        }

        private void TryShowBeatForCurrentStep()
        {
            if (_waveStatePresenter == null || _isComplete || _sequenceData.Steps == null)
            {
                return;
            }

            if (_currentStepIndex < 0 || _currentStepIndex >= _sequenceData.Steps.Length)
            {
                return;
            }

            if (_shownBeatStepIndices.Contains(_currentStepIndex))
            {
                return;
            }

            var step = _sequenceData.Steps[_currentStepIndex];

            if (step == null || _completedSteps.Contains(step.StepType))
            {
                return;
            }

            _isBeatShowing = _waveStatePresenter.TryShowTutorialBeat(step.BeatTitle, step.BeatBody, OnTutorialBeatDismissed);
            IsBeatShowing = _isBeatShowing;

            if (_isBeatShowing)
            {
                _shownBeatStepIndices.Add(_currentStepIndex);

                // Hide the persistent hint bar while the paused beat panel (a different Canvas,
                // sorting order unverified against this one without a live Editor check) has the
                // screen - avoids any risk of the two overlapping instead of relying on draw order.
                if (_promptRoot != null)
                {
                    _promptRoot.SetActive(false);
                }
            }
        }

        private void OnTutorialBeatDismissed()
        {
            _isBeatShowing = false;
            IsBeatShowing = false;
            IsBeatShowing = false;

            if (_promptRoot != null && !_isComplete)
            {
                _promptRoot.SetActive(true);
            }

            RefreshPrompt();
        }

        private void OnEnable()
        {
            if (!_isActive)
            {
                return;
            }

            if (_playerController != null)
            {
                _playerController.Moved += OnPlayerMoved;
            }

            if (_playerCombatController != null)
            {
                _playerCombatController.WeaponFired += OnWeaponFired;
            }

            if (_resourceWalletService != null)
            {
                _resourceWalletService.WalletChanged += OnWalletChanged;
            }

            if (_towerService != null)
            {
                _towerService.TowersChanged += OnTowersChanged;
            }

            if (_cardService != null)
            {
                _cardService.StateChanged += OnCardStateChanged;
            }

            WaveEvents.WaveCompleted += OnWaveCompleted;

            if (_coreRepairService != null)
            {
                _coreRepairService.StateChanged += OnCoreRepairChanged;
            }
        }

        private void OnDisable()
        {
            if (_playerController != null)
            {
                _playerController.Moved -= OnPlayerMoved;
            }

            if (_playerCombatController != null)
            {
                _playerCombatController.WeaponFired -= OnWeaponFired;
            }

            if (_resourceWalletService != null)
            {
                _resourceWalletService.WalletChanged -= OnWalletChanged;
            }

            if (_towerService != null)
            {
                _towerService.TowersChanged -= OnTowersChanged;
            }

            if (_cardService != null)
            {
                _cardService.StateChanged -= OnCardStateChanged;
            }

            WaveEvents.WaveCompleted -= OnWaveCompleted;

            if (_coreRepairService != null)
            {
                _coreRepairService.StateChanged -= OnCoreRepairChanged;
            }
        }

        // B7: the loadout panel is the only place a player meets Set Bonuses, tiers and selling,
        // and nothing taught it. Watching the presenter's own static open flag keeps this out of
        // the panel's code, per Tutorial.md's rule that the tutorial only observes.
        [SerializeField] private Corebound.Gameplay.Combat.CoreRepairService _coreRepairService;

        private void OnCoreRepairChanged()
        {
            MarkStepComplete(TutorialStepType.CoreRepair);
        }

        private void OnPlayerMoved()
        {
            MarkStepComplete(TutorialStepType.Movement);
        }

        private void OnWeaponFired()
        {
            MarkStepComplete(TutorialStepType.WeaponUse);
        }

        private void OnWalletChanged()
        {
            if (_resourceWalletService != null && _resourceWalletService.CurrentAmount > 0)
            {
                MarkStepComplete(TutorialStepType.ResourceCollect);
            }
        }

        private void OnTowersChanged()
        {
            if (_towerService != null && _towerService.BuiltTowerCount > 0)
            {
                MarkStepComplete(TutorialStepType.TowerBuild);
            }
        }

        private void OnCardStateChanged()
        {
            if (_cardService != null && _cardService.SelectedCards.Count > 0)
            {
                MarkStepComplete(TutorialStepType.CardSelect);
            }
        }

        private void OnWaveCompleted(WaveProgressEventArgs eventArgs)
        {
            // The first full Wave clear is the first concrete proof the player kept the Core
            // standing under real pressure - the simplest measurable reading of "Core-Verteidigung"
            // available from the existing Wave event surface.
            MarkStepComplete(TutorialStepType.CoreDefense);
        }

        private void MarkStepComplete(TutorialStepType stepType)
        {
            if (!_isActive || _isComplete || _completedSteps.Contains(stepType) || _sequenceData.Steps == null)
            {
                return;
            }

            var stepIndex = System.Array.FindIndex(_sequenceData.Steps, candidate => candidate != null && candidate.StepType == stepType);

            if (stepIndex < 0)
            {
                return;
            }

            var step = _sequenceData.Steps[stepIndex];

            // Blocking (the default) means this action only counts once every step ahead of it in
            // the authored order is done - firing the event early (e.g. shooting before the
            // Movement step is reached) is ignored instead of skipping the sequence ahead.
            // Non-blocking steps can complete whenever their event fires, regardless of position.
            if (step.IsBlocking && stepIndex > _currentStepIndex)
            {
                return;
            }

            _completedSteps.Add(stepType);
            AdvanceCurrentStepIndex();
            RefreshPrompt();

            if (_completedSteps.Count >= _sequenceData.Steps.Length)
            {
                CompleteTutorial();
                return;
            }

            TryShowBeatForCurrentStep();
        }

        private void AdvanceCurrentStepIndex()
        {
            while (_currentStepIndex < _sequenceData.Steps.Length
                && _sequenceData.Steps[_currentStepIndex] != null
                && _completedSteps.Contains(_sequenceData.Steps[_currentStepIndex].StepType))
            {
                _currentStepIndex++;
            }
        }

        private void CompleteTutorial()
        {
            _isComplete = true;
            HidePrompt();

            if (SaveManager == null)
            {
                return;
            }

            var metaSaveData = SaveManager.LoadMetaProgressionSaveData(new MetaProgressionSaveData());

            if (metaSaveData.HasSeenTutorial)
            {
                return;
            }

            metaSaveData.SetHasSeenTutorial(true);
            SaveManager.SaveMetaProgressionSaveData(metaSaveData);
        }

        private void RefreshPrompt()
        {
            if (_promptText == null || _sequenceData.Steps == null)
            {
                return;
            }

            for (var index = 0; index < _sequenceData.Steps.Length; index++)
            {
                var step = _sequenceData.Steps[index];

                if (step == null || _completedSteps.Contains(step.StepType))
                {
                    continue;
                }

                _promptText.text = step.PromptText;
                return;
            }

            HidePrompt();
        }

        private void HidePrompt()
        {
            if (_promptRoot != null)
            {
                _promptRoot.SetActive(false);
            }
        }

        private void BuildPromptUi()
        {
            var canvasObject = new GameObject("TutorialPromptCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panelObject = new GameObject("TutorialPromptPanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            var panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 56f);
            panelRect.sizeDelta = new Vector2(820f, 68f);
            var panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.07f, 0.1f, 0.8f);

            var textObject = new GameObject("TutorialPromptText");
            textObject.transform.SetParent(panelObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 6f);
            textRect.offsetMax = new Vector2(-24f, -6f);
            _promptText = textObject.AddComponent<Text>();
            _promptText.font = UIFontResolver.Resolve();
            _promptText.fontSize = 22;
            _promptText.alignment = TextAnchor.MiddleCenter;
            _promptText.color = Color.white;
            _promptText.horizontalOverflow = HorizontalWrapMode.Wrap;

            _promptRoot = canvasObject;
        }
    }
}
