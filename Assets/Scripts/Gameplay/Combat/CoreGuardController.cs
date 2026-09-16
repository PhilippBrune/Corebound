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
    public sealed class CoreGuardController : MonoBehaviour
    {
        private const string RingSpritePath = "Assets/Art/Towers/tower_upgrade_ring.png";
        private const float GuardRadius = 2.2f;
        private const float GuardDamageMultiplier = 0.15f;
        private static readonly Color GuardRingColor = new Color(0.35f, 0.85f, 1f, 0.85f);

        private Transform _playerTarget;
        private CombatHitReceiver _coreHitReceiver;
        private IInputReader _inputReader;
        private SpriteRenderer _ringRenderer;
        private bool _isGuarding;
        private bool _hasShownHint;

        public bool IsGuarding => _isGuarding;

        public void Configure(Transform playerTarget, CombatHitReceiver coreHitReceiver, IInputReader inputReader)
        {
            _playerTarget = playerTarget;
            _coreHitReceiver = coreHitReceiver;
            _inputReader = inputReader;
            EnsureRingRenderer();
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
                NotificationPresenter.Show("Hold Interact near the Core to raise a Guard shield");
            }

            var wantsGuard = _inputReader.IsInteractHeld && isInRange;

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
            DebugSpriteFactory.FitTransformToSprite(ringObject.transform, _ringRenderer.sprite, Vector2.one * 2.6f);
            ringObject.SetActive(false);
        }
    }
}
