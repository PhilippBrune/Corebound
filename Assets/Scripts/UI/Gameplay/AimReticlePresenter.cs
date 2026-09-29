using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Shared;
using Corebound.Input;
using Corebound.Gameplay.Weapons;
using UnityEngine;

namespace Corebound.UI.Gameplay
{
    // Replaces the OS mouse pointer with a reticle that reports weapon state (OPEN_TOPICS 3.1,
    // owner chose option 3).
    //
    // The point is not decoration. The reticle opens while the weapon is cooling and closes as it
    // becomes ready, so reload state is readable at the place the player is already looking -
    // information the HUD otherwise has to carry, and which a player in a firefight does not have
    // the attention to go and read. It also tints when the aim point is over a live enemy, which
    // answers "will this hit anything" without a hit-scan preview.
    //
    // Drawn in world space rather than as a hardware cursor because a hardware cursor cannot change
    // with game state. That costs a frame of latency, which is the honest trade named in the
    // proposal; the OS cursor is hidden while this is active so there are never two pointers.
    //
    // On a gamepad the OS pointer is meaningless and the aim comes from the stick, so the reticle
    // follows the resolved aim direction instead of the pointer - and the Windows arrow, which used
    // to sit parked in the arena during controller play, is hidden outright.
    public sealed class AimReticlePresenter : MonoBehaviour
    {
        private const float OrbitRadius = 1.35f;
        private const float ReadySize = 0.42f;
        private const float CoolingSize = 0.78f;
        private const int SortingOrder = 60;

        private static readonly Color ReadyColor = new Color(1f, 0.78f, 0.35f, 0.95f);
        private static readonly Color CoolingColor = new Color(0.72f, 0.78f, 0.88f, 0.7f);
        private static readonly Color TargetColor = new Color(1f, 0.42f, 0.35f, 0.98f);

        [SerializeField] private PlayerCombatController _playerCombatController;
        [SerializeField] private UnityEngine.Object _inputReaderSource;

        // Matches the enemy layer test the soft-aim scan already uses: anything with a
        // CombatHitReceiver that is alive and is not the player.
        [SerializeField] private float _targetProbeRadius = 0.55f;

        private readonly Collider2D[] _probeBuffer = new Collider2D[8];

        private Transform[] _petals;
        private Transform _root;
        private Camera _camera;
        private bool _cursorHidden;

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private void Awake()
        {
            if (_playerCombatController == null)
            {
                _playerCombatController = GetComponent<PlayerCombatController>() ?? FindFirstObjectByType<PlayerCombatController>();
            }

            if (_inputReaderSource == null)
            {
                _inputReaderSource = FindFirstObjectByType<CoreboundInputReader>();
            }

            BuildReticle();
        }

        private void OnDisable()
        {
            RestoreCursor();
        }

        private void BuildReticle()
        {
            var rootObject = new GameObject("AimReticle");
            _root = rootObject.transform;
            // One-unit quad, so each petal's localScale below is its real world size.
            var sprite = DebugSpriteFactory.CreateSquare(DebugSpriteFactory.UnitPixelsPerUnit);
            _petals = new Transform[4];

            // Four petals arranged around the centre. They spread apart while cooling and draw in
            // as the weapon becomes ready, which reads as "closing" without needing an animation.
            for (var index = 0; index < _petals.Length; index++)
            {
                var petalObject = new GameObject("Petal" + index);
                petalObject.transform.SetParent(_root, false);
                var renderer = petalObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = SortingOrder;
                petalObject.transform.localScale = new Vector3(0.06f, 0.2f, 1f);
                petalObject.transform.localRotation = Quaternion.Euler(0f, 0f, index * 90f);
                _petals[index] = petalObject.transform;
            }
        }

        private void LateUpdate()
        {
            if (_playerCombatController == null || _root == null)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            var reader = InputReader;
            var usingGamepad = reader != null && reader.IsUsingGamepad;
            var origin = (Vector2)_playerCombatController.transform.position;
            var aimPoint = ResolveAimPoint(reader, usingGamepad, origin);

            HideCursorWhileActive();
            _root.position = aimPoint;

            var readiness = _playerCombatController.WeaponReadinessState;
            var cooldown = Mathf.Clamp01(_playerCombatController.FireCooldownNormalized);

            // FireCooldownNormalized runs 1 -> 0 as the weapon recovers, so the spread follows it
            // directly: wide right after a shot, tight when ready.
            var spread = readiness == WeaponReadinessState.Ready
                ? Mathf.Lerp(ReadySize, CoolingSize, cooldown)
                : CoolingSize;

            var color = readiness == WeaponReadinessState.Unavailable
                ? CoolingColor
                : (IsAimingAtEnemy(aimPoint) ? TargetColor : (cooldown > 0.02f ? CoolingColor : ReadyColor));

            for (var index = 0; index < _petals.Length; index++)
            {
                var angle = (index * 90f + 45f) * Mathf.Deg2Rad;
                _petals[index].localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spread;
                var renderer = _petals[index].GetComponent<SpriteRenderer>();

                if (renderer != null)
                {
                    renderer.color = color;
                }
            }
        }

        private Vector2 ResolveAimPoint(IInputReader reader, bool usingGamepad, Vector2 origin)
        {
            if (reader == null)
            {
                return origin;
            }

            // PlayerAimResolver reads Aim as a *screen position* in mouse mode and as a direction on
            // a stick - the same distinction that once made every gamepad shot fly at a screen
            // corner. Keeping both branches explicit here avoids repeating that mistake.
            if (usingGamepad)
            {
                var direction = reader.Aim;
                return direction.sqrMagnitude > 0.01f
                    ? origin + direction.normalized * OrbitRadius
                    : origin + Vector2.right * OrbitRadius;
            }

            if (_camera == null)
            {
                return origin;
            }

            var world = _camera.ScreenToWorldPoint(new Vector3(reader.Aim.x, reader.Aim.y, -_camera.transform.position.z));
            return new Vector2(world.x, world.y);
        }

        private bool IsAimingAtEnemy(Vector2 aimPoint)
        {
            var count = Physics2D.OverlapCircleNonAlloc(aimPoint, _targetProbeRadius, _probeBuffer);

            for (var index = 0; index < count; index++)
            {
                var collider = _probeBuffer[index];

                if (collider == null)
                {
                    continue;
                }

                var receiver = (collider.attachedRigidbody != null ? collider.attachedRigidbody.GetComponent<CombatHitReceiver>() : null)
                    ?? collider.GetComponent<CombatHitReceiver>();

                if (receiver != null && receiver.IsAlive && receiver.gameObject != _playerCombatController.gameObject)
                {
                    return true;
                }
            }

            return false;
        }

        private void HideCursorWhileActive()
        {
            if (_cursorHidden)
            {
                return;
            }

            Cursor.visible = false;
            _cursorHidden = true;
        }

        private void RestoreCursor()
        {
            if (!_cursorHidden)
            {
                return;
            }

            // Restored on disable so leaving the gameplay scene never strands the player without a
            // pointer in the menus.
            Cursor.visible = true;
            _cursorHidden = false;
        }
    }
}
