using System;
using Corebound.Gameplay.Economy;
using Corebound.Input;
using Corebound.Gameplay.Waves;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // Owner decision 2026-09-21: the Core stays stationary and cannot be stat-upgraded, but the
    // player may pay currency to repair it.
    //
    // This is the smallest thing that makes vision Section 5.3's promise - "Invest in offense or
    // strengthen the Core's defense?" - an actual choice. Until now there was no way to spend
    // anything on the Core at all, so that decision existed only in the document.
    //
    // Deliberately a repair and not an upgrade: repairing competes with towers and weapons for the
    // same Scrap without raising any ceiling, so it can never become a power creep sink. A player
    // who has taken no Core damage has nothing to buy here, which is the correct incentive.
    public sealed class CoreRepairService : MonoBehaviour, IInteractHandler
    {
        [SerializeField] private WaveManager _waveManager;
        [SerializeField] private EconomyService _economyService;
        [SerializeField] private CombatHitReceiver _coreHitReceiver;
        [SerializeField] private UnityEngine.Object _inputReaderSource;
        [SerializeField] private Transform _playerTransform;

        // Repair is triggered by standing near the Core and pressing Interact - the same input
        // that builds a tower when standing on a slot. Reusing one binding by proximity keeps
        // the control scheme from growing a key per verb, and it gives the Core a physical
        // place the player has to go to, which is better than a menu entry for something the
        // vision calls a defensive investment.
        [SerializeField] private float _interactRadius = 2.4f;

        // One purchase restores this fraction of the Core's maximum health. A fraction rather than
        // a flat amount so the repair stays meaningful as Core health is tuned.
        [SerializeField] [Range(0.05f, 1f)] private float _repairFraction = 0.25f;

        [SerializeField] private int _baseRepairCost = 14;

        // Each repair within the same run costs more than the last, so a player cannot simply
        // out-spend Core damage indefinitely and ignore defending it.
        [SerializeField] [Range(1f, 3f)] private float _repeatCostMultiplier = 1.5f;

        private int _repairsThisRun;

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        public bool IsPlayerInRange => ResolvePlayerDistanceToCore() <= _interactRadius;

        public event Action StateChanged;

        public int RepairsThisRun => _repairsThisRun;

        public bool IsCoreDamaged => _coreHitReceiver != null && _coreHitReceiver.CurrentHealthNormalized < 0.999f;

        // Repairing is an Intermission action, like building and shopping. Allowing it mid-combat
        // would turn every Core hit into a pause-and-pay rather than a mistake with consequences.
        public bool CanRepair => IsCoreDamaged
            && _waveManager != null
            && _waveManager.State == WaveState.Intermission
            && IsPlayerInRange
            && CanAfford;

        private const string FreeRepairRuleKey = "skill_field_repair";

        // Field Repair: the first repair each stage is free, shown as a cost of 0 until used.
        private bool IsFreeRepairAvailable
        {
            get
            {
                var progression = Corebound.MetaProgression.RunProgressionSnapshot.Current;
                return progression.Skills.HasRule(Corebound.MetaProgression.SkillEffectTarget.FreeFirstRepairPerStage)
                    && progression.IsRuleAvailableThisStage(FreeRepairRuleKey);
            }
        }

        public int CurrentRepairCost => IsFreeRepairAvailable ? 0 : Mathf.Max(
            1,
            Mathf.RoundToInt(_baseRepairCost * Mathf.Pow(_repeatCostMultiplier, _repairsThisRun)
                * Mathf.Max(0.1f, Corebound.MetaProgression.RunProgressionSnapshot.Current.Skills[Corebound.MetaProgression.SkillEffectTarget.CoreRepairCostMultiplier])));

        private bool CanAfford => _economyService != null && _economyService.CanAfford(CurrentRepairCost);

        private void Awake()
        {
            if (_waveManager == null)
            {
                _waveManager = GetComponent<WaveManager>();
            }

            if (_economyService == null)
            {
                _economyService = GetComponent<EconomyService>();
            }

            ResolveCoreHitReceiver();
        }

        private void OnEnable()
        {
            if (InputReader != null)
            {
                InputReader.InteractPressed += OnInteractPressed;
                InteractRouter.Register(this);
            }
        }

        private void OnDisable()
        {
            if (InputReader != null)
            {
                InputReader.InteractPressed -= OnInteractPressed;
                InteractRouter.Unregister(this);
            }
        }

        private void OnInteractPressed()
        {
            InteractRouter.Dispatch();
        }

        // IInteractHandler (spec review A1): below a tower slot, above the Core guard.
        public InteractPriority InteractPriority => InteractPriority.CoreRepair;

        public bool CanHandleInteract => CanRepair;

        public void HandleInteract()
        {
            TryRepair();
        }

        // The repair had no prompt at all - a player had to know that Interact near the Core does
        // something. Asset 94 (core_repair_icon.png) now marks it: shown above the Core during an
        // Intermission while the player is in range and the Core is damaged, with the price.
        private const string RepairIconPath = "Assets/Art/UI/HUD/core_repair_icon.png";
        private GameObject _prompt;
        private TextMesh _promptText;

        private void Update()
        {
            // Core Aegis (NEW_CARD_MECHANICS C3-1): the Core mends itself between waves.
            var regen = RunCombatEffects.CoreIntermissionRegen;

            if (regen > 0f && _waveManager != null && _waveManager.State == WaveState.Intermission && Time.timeScale > 0f)
            {
                ResolveCoreHitReceiver();

                if (_coreHitReceiver != null && _coreHitReceiver.TryRepairFraction(regen * Time.deltaTime))
                {
                    StateChanged?.Invoke();
                }
            }
        }

        private void LateUpdate()
        {
            ResolveCoreHitReceiver();
            var show = _coreHitReceiver != null && IsCoreDamaged && IsPlayerInRange
                && _waveManager != null && _waveManager.State == WaveState.Intermission && Time.timeScale > 0f;

            if (!show)
            {
                if (_prompt != null && _prompt.activeSelf)
                {
                    _prompt.SetActive(false);
                }

                return;
            }

            EnsurePrompt();
            _prompt.SetActive(true);
            _prompt.transform.position = _coreHitReceiver.transform.position + new Vector3(0f, 1.6f, 0f);
            var cost = CurrentRepairCost;
            var key = ResolveInteractKeyLabel();
            _promptText.text = key + LocalizationService.Resolve(": Repair +", ": Reparieren +") + Mathf.RoundToInt(_repairFraction * 100f) + "%  ·  "
                + (cost == 0 ? LocalizationService.Resolve("free", "gratis") : cost + LocalizationService.Resolve(" Scrap", " Schrott"));
            _promptText.color = CanAfford ? new Color(0.95f, 0.97f, 1f, 1f) : new Color(1f, 0.5f, 0.45f, 1f);
        }

        private void EnsurePrompt()
        {
            if (_prompt != null)
            {
                return;
            }

            // A scene-root object: world-space, and never a child of CombatRuntime (see the shared
            // GameObject Canvas gotcha - this is not a canvas, but it stays independent anyway).
            _prompt = new GameObject("CoreRepairPrompt");
            var icon = new GameObject("Icon");
            icon.transform.SetParent(_prompt.transform, false);
            icon.transform.localPosition = new Vector3(-1.35f, 0f, 0f);
            var sprite = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(RepairIconPath);
            var renderer = icon.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 58;

            if (sprite != null)
            {
                Corebound.Gameplay.Shared.DebugSpriteFactory.FitTransformToSprite(icon.transform, sprite, new Vector2(0.6f, 0.6f));
            }

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(_prompt.transform, false);
            textObject.transform.localPosition = new Vector3(0.15f, 0f, 0f);
            _promptText = textObject.AddComponent<TextMesh>();
            _promptText.fontSize = 40;
            _promptText.characterSize = 0.05f;
            _promptText.anchor = TextAnchor.MiddleCenter;
            _promptText.fontStyle = FontStyle.Bold;
            var font = Corebound.UI.UIFontResolver.Resolve();

            if (font != null)
            {
                _promptText.font = font;
                textObject.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }

            textObject.GetComponent<MeshRenderer>().sortingOrder = 58;
        }

        private string ResolveInteractKeyLabel()
        {
            if (_inputReaderSource is CoreboundInputReader reader)
            {
                var display = reader.GetBindingDisplayString("Gameplay", "Interact", reader.IsUsingGamepad ? "Gamepad" : "Keyboard&Mouse");

                if (!string.IsNullOrWhiteSpace(display))
                {
                    return display.ToUpperInvariant();
                }

                return reader.IsUsingGamepad ? "Y" : "E";
            }

            return "E";
        }

        private void OnDestroy()
        {
            if (_prompt != null)
            {
                Destroy(_prompt);
            }
        }

        private float ResolvePlayerDistanceToCore()
        {
            if (_playerTransform == null)
            {
                var playerCombat = FindFirstObjectByType<PlayerCombatController>();
                _playerTransform = playerCombat != null ? playerCombat.transform : null;
            }

            ResolveCoreHitReceiver();

            if (_playerTransform == null || _coreHitReceiver == null)
            {
                return float.MaxValue;
            }

            return Vector2.Distance(_playerTransform.position, _coreHitReceiver.transform.position);
        }

        // The Core lives on its own anchor object, not on this component's GameObject, and it is
        // rebuilt on a stage change - so it is resolved by name the same way every other system
        // finds it, and re-resolved if the reference has gone stale.
        private void ResolveCoreHitReceiver()
        {
            if (_coreHitReceiver != null)
            {
                return;
            }

            var coreAnchor = GameObject.Find(CombatHitReceiver.CoreAnchorObjectName);
            _coreHitReceiver = coreAnchor != null ? coreAnchor.GetComponent<CombatHitReceiver>() : null;
        }

        public bool TryRepair()
        {
            ResolveCoreHitReceiver();

            if (!CanRepair)
            {
                return false;
            }

            // ResolveCost applies the PurchaseCostMultiplier and raises PriceModified once, which
            // is correct at the moment of an actual purchase. CurrentRepairCost is the base price;
            // TryPurchase expects the resolved one.
            var isFree = IsFreeRepairAvailable;
            var cost = isFree ? 0 : _economyService.ResolveCost(CurrentRepairCost);

            if (!isFree && !_economyService.TryPurchase(cost))
            {
                return false;
            }

            if (!_coreHitReceiver.TryRepairFraction(_repairFraction))
            {
                // Payment already went through, so roll it back. Same correctness guard
                // ShopService.RefundPurchase exists for, and for the same reason.
                _economyService.Refund(cost);
                return false;
            }

            if (isFree)
            {
                Corebound.MetaProgression.RunProgressionSnapshot.Current.TryConsumeOncePerStage(FreeRepairRuleKey);
            }
            else
            {
                // A free repair does not escalate the price of the next one.
                _repairsThisRun++;
            }

            StateChanged?.Invoke();
            return true;
        }

        // Called when a run starts, so the escalating cost does not carry across runs.
        public void ResetForNewRun()
        {
            _repairsThisRun = 0;
            StateChanged?.Invoke();
        }
    }
}
