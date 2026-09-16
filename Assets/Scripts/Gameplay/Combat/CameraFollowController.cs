using Corebound.Gameplay.Player;
using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // The Main Camera used to sit static at world origin (confirmed via Inspector) while the arena
    // extends further than its fixed orthographic view in some directions/stages, letting the
    // player run near/off screen. This clamps the camera to the player each LateUpdate, bounded by
    // the arena's actual size (StageArenaArtPresenter.ArenaSize) so the camera never shows past the
    // boundary walls, and simply re-centers (no clamp) on any axis where the arena is smaller than
    // the camera's view.
    public sealed class CameraFollowController : MonoBehaviour
    {
        [SerializeField] private Transform _playerTarget;
        [SerializeField] private StageArenaArtPresenter _arenaPresenter;
        [SerializeField] private float _followSmoothTime = 0.12f;

        private Camera _camera;
        private Vector3 _velocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (_playerTarget == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                _playerTarget = player != null ? player.transform : null;
            }

            if (_arenaPresenter == null)
            {
                _arenaPresenter = FindFirstObjectByType<StageArenaArtPresenter>();
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || _playerTarget == null)
            {
                return;
            }

            var targetPosition = ResolveClampedTargetPosition();
            var currentZ = transform.position.z;
            var smoothed = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _followSmoothTime);
            transform.position = new Vector3(smoothed.x, smoothed.y, currentZ);
        }

        private Vector3 ResolveClampedTargetPosition()
        {
            var desired = _playerTarget.position;
            var arenaSize = _arenaPresenter != null ? _arenaPresenter.ArenaSize : Vector2.zero;

            if (arenaSize.x <= 0f || arenaSize.y <= 0f)
            {
                return new Vector3(desired.x, desired.y, desired.z);
            }

            var halfViewHeight = _camera.orthographicSize;
            var halfViewWidth = halfViewHeight * _camera.aspect;
            var halfArena = arenaSize * 0.5f;

            var clampX = ClampAxis(desired.x, halfArena.x, halfViewWidth);
            var clampY = ClampAxis(desired.y, halfArena.y, halfViewHeight);

            return new Vector3(clampX, clampY, desired.z);
        }

        // When the arena is smaller than the camera's view on an axis, clamping to
        // [-(halfArena-halfView), halfArena-halfView] would invert min/max - re-center on that axis
        // (arena origin, 0) instead of clamping, since there's nowhere useful to pan.
        private static float ClampAxis(float desired, float halfArena, float halfView)
        {
            var limit = halfArena - halfView;

            if (limit <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp(desired, -limit, limit);
        }
    }
}
