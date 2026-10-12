using Corebound.Gameplay.Shared;
using Corebound.Input;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // Project Vision's whole premise is defending a central Core, but before this the Core had no
    // player-facing defense beyond passive towers - Action First calls for the player to be an
    // active participant, not a bystander while towers do the work alone. Holding Interact while
    // standing near the Core channels a temporary damage-reduction shield, creating a genuine
    // moment-to-moment choice: break off from fighting enemies to actively protect the Core, or
    // keep attacking and trust towers/positioning to hold the line instead.
    //
    // CORE_PROTECTION_FEEL_2026-10-07, C7 (owner decision P3-1): guarding used to be waiting. Every
    // hit the raised Guard blocks now charges it, and the fifth releases the Custodian's shockwave
    // from the Core - the guard becomes a counter-attack. The charge is kept while the Guard is
    // lowered, so stepping out to shoot does not throw it away.
    public sealed class CoreGuardController : MonoBehaviour
    {
        private const string RingSpritePath = "Assets/Art/Towers/tower_upgrade_ring.png";
        private const float GuardRadius = 2.2f;
        private const float GuardDamageMultiplier = 0.15f;
        private static readonly Color GuardRingColor = new Color(0.35f, 0.85f, 1f, 0.85f);
        private static readonly Color ChargedRingColor = new Color(0.9f, 0.97f, 1f, 1f);
        private static readonly Color PulseLabelColor = new Color(0.55f, 0.88f, 1f, 1f);
        private const float RingSize = 2.6f;

        private Transform _playerTarget;
        private CombatHitReceiver _coreHitReceiver;
        private IInputReader _inputReader;
        private SpriteRenderer _ringRenderer;
        private bool _isGuarding;
        private bool _hasShownHint;
        private int _charge;
        private float _chargeFlash;

#if UNITY_EDITOR
        // Autopilot "core" probe: raise the Guard without walking there or holding a key, so the
        // charge and the pulse can be counted in a scripted run.
        public static bool DebugForceGuard;
        public static int DebugBlockedHits;
        public static int DebugPulses;
        public static int DebugPulseEnemiesReached;
#endif

        public bool IsGuarding => _isGuarding;

        public int Charge => _charge;

        public void Configure(Transform playerTarget, CombatHitReceiver coreHitReceiver, IInputReader inputReader)
        {
            _playerTarget = playerTarget;
            _coreHitReceiver = coreHitReceiver;
            _inputReader = inputReader;
            EnsureRingRenderer();
        }

        private void OnEnable()
        {
            CoreController.CoreHit += OnCoreHit;
        }

        private void Update()
        {
            if (_playerTarget == null || _coreHitReceiver == null || _inputReader == null)
            {
                return;
            }

            var distance = Vector2.Distance(_playerTarget.position, _coreHitReceiver.transform.position);
            var isInRange = distance <= GuardRadius;

            // Fires once, the first time the player wanders into guard range without already
            // holding Interact - a brand new mechanic with zero on-screen indication is exactly the
            // discoverability mistake already found and fixed once this session for tower-type
            // switching (TowerService's build HUD hint).
            if (isInRange && !_hasShownHint && !_inputReader.IsInteractHeld)
            {
                _hasShownHint = true;
                NotificationPresenter.Show(LocalizationService.Resolve(
                    "Hold Interact near the Core to raise a Guard - every hit it blocks charges a counter-pulse",
                    "Halte Interagieren am Kern für einen Schild - jeder abgewehrte Treffer lädt einen Gegenstoß"));
            }

            if (!_inputReader.IsInteractHeld)
            {
                Corebound.Input.InteractRouter.ReleaseHold();
            }

            // Spec review A1: the guard is the lowest-priority Interact verb. A press that built a
            // tower or bought a Core repair must not also raise the shield, so a hold whose press
            // was already spent stays inert until the button is released.
            var wantsGuard = _inputReader.IsInteractHeld && isInRange && !Corebound.Input.InteractRouter.HoldConsumed;
#if UNITY_EDITOR
            wantsGuard |= DebugForceGuard;
#endif

            UpdateRingLook();

            if (wantsGuard == _isGuarding)
            {
                return;
            }

            _isGuarding = wantsGuard;
            _coreHitReceiver.SetGuardDamageMultiplier(_isGuarding ? GuardDamageMultiplier : 1f);

            if (_ringRenderer != null)
            {
                _ringRenderer.gameObject.SetActive(_isGuarding);
            }
        }

        private void OnDisable()
        {
            CoreController.CoreHit -= OnCoreHit;

            if (_coreHitReceiver != null)
            {
                _coreHitReceiver.SetGuardDamageMultiplier(1f);
            }

            if (_ringRenderer != null)
            {
                _ringRenderer.gameObject.SetActive(false);
            }

            _isGuarding = false;
        }

        private void OnCoreHit(CoreHitInfo hit)
        {
            if (!_isGuarding || !hit.Guarded || _coreHitReceiver == null || !_coreHitReceiver.IsAlive)
            {
                return;
            }

#if UNITY_EDITOR
            DebugBlockedHits++;
#endif
            _charge++;
            _chargeFlash = 1f;
            var tuning = CoreController.Tuning;

            if (_charge < tuning.GuardPulseHits)
            {
                return;
            }

            _charge = 0;
            var reached = CoreShockwave.Release(gameObject, _coreHitReceiver.transform.position,
                tuning.GuardPulseRadius, tuning.GuardPulseDamage, tuning.GuardPulseImpulse, "core_guard_pulse");
            CriticalHitPopup.SpawnLabel((Vector2)_coreHitReceiver.transform.position + Vector2.up * 0.6f,
                LocalizationService.Resolve("Guard pulse", "Gegenstoß"), PulseLabelColor, 1.1f);
#if UNITY_EDITOR
            DebugPulses++;
            DebugPulseEnemiesReached += reached;
#endif
        }

        // The ring fills toward white with each blocked hit and swells briefly on the hit itself,
        // so the player can see the counter-pulse coming.
        private void UpdateRingLook()
        {
            if (_ringRenderer == null || !_ringRenderer.gameObject.activeSelf)
            {
                return;
            }

            _chargeFlash = Mathf.MoveTowards(_chargeFlash, 0f, Time.deltaTime * 4f);
            var fill = Mathf.Clamp01(_charge / (float)CoreController.Tuning.GuardPulseHits);
            _ringRenderer.color = Color.Lerp(GuardRingColor, ChargedRingColor, Mathf.Max(fill, _chargeFlash * 0.8f));
            _ringRenderer.transform.localScale = _baseRingScale * (1f + 0.12f * _chargeFlash);
        }

        private Vector3 _baseRingScale = Vector3.one;

        private void EnsureRingRenderer()
        {
            if (_ringRenderer != null || _coreHitReceiver == null)
            {
                return;
            }

            var ringObject = new GameObject("CoreGuardRing");
            ringObject.transform.SetParent(_coreHitReceiver.transform, false);
            ringObject.transform.localPosition = Vector3.zero;
            _ringRenderer = ringObject.AddComponent<SpriteRenderer>();
            _ringRenderer.sprite = DebugSpriteFactory.LoadProjectSprite(RingSpritePath);
            _ringRenderer.color = GuardRingColor;
            _ringRenderer.sortingOrder = -1;
            DebugSpriteFactory.FitTransformToSprite(ringObject.transform, _ringRenderer.sprite, Vector2.one * RingSize);
            _baseRingScale = ringObject.transform.localScale;
            ringObject.SetActive(false);
        }
    }
}
