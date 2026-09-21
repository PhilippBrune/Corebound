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

        // Frames the arena's full height rather than using one fixed zoom for every stage.
        [SerializeField] private bool _frameArenaHeight = true;
        [SerializeField] private float _framingMargin = 0.4f;
        [SerializeField] private float _minOrthographicSize = 5f;
        [SerializeField] private float _maxOrthographicSize = 7.5f;
        [SerializeField] private float _zoomSpeed = 4f;

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

            UpdateFramingZoom();

            var targetPosition = ResolveClampedTargetPosition();
            var currentZ = transform.position.z;
            var smoothed = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _followSmoothTime);
            transform.position = new Vector3(smoothed.x, smoothed.y, currentZ);
        }

        // Playtest 2026-09-21: "stage 5 arena is too big, player cannot see the edge."
        //
        // Before the Dynamic Arena Slice every arena was 16x10 and a fixed orthographic size framed
        // it exactly. Now arenas differ per stage - Stage 5 is 22x12 - and the same fixed zoom shows
        // a 10-unit-tall window onto a 12-unit-tall arena, so the top and bottom walls are simply
        // never on screen. That breaks vision Section 5.5's requirement that the player can see the
        // Core's condition and where the arena ends.
        //
        // Fixing it by shrinking Stage 5 would throw away the per-stage variation the slice exists
        // to provide. Zooming to frame the arena's height instead fixes every stage at once, and
        // scales automatically to any arena authored later. At 16:9 a 12-unit-tall arena framed
        // vertically also shows 21.3 units horizontally, which covers all but the last half unit of
        // Stage 5's width.
        //
        // The cap matters: without it a very large future arena would zoom out until the player is
        // an unreadable speck, which trades one readability problem for a worse one.
        private void UpdateFramingZoom()
        {
            if (!_frameArenaHeight || _camera == null)
            {
                return;
            }

            var arenaSize = _arenaPresenter != null ? _arenaPresenter.ArenaSize : Vector2.zero;

            if (arenaSize.y <= 0f)
            {
                return;
            }

            var requiredSize = arenaSize.y * 0.5f + _framingMargin;
            var targetSize = Mathf.Clamp(requiredSize, _minOrthographicSize, _maxOrthographicSize);

            _camera.orthographicSize = Mathf.Approximately(_camera.orthographicSize, targetSize)
                ? targetSize
                : Mathf.MoveTowards(_camera.orthographicSize, targetSize, _zoomSpeed * Time.deltaTime);
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
