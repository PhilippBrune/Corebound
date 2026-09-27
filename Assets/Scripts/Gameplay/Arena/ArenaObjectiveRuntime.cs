using System;
using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Arena
{
    // The world object for one arena objective: a body that pulses, a ring that drains as the
    // lifetime runs out, and a walk-over claim (owner chose D3 option 1 - no channel, no button).
    //
    // Deliberately NOT built on ResourcePickupRuntime, and deliberately not registered with
    // ResourcePickupPool. That pool's ForceCollectAll() vacuums every live pickup into the player
    // when a wave ends, which would hand the player every cache they ignored - silently, and only
    // in a full playthrough (D8). An objective that expires has to actually be lost.
    public sealed class ArenaObjectiveRuntime : MonoBehaviour
    {
        private const string BodySpritePath = "Assets/Art/Environment/Arena/arena_salvage_cache.png";
        private const string RingSpritePath = "Assets/Art/Environment/Arena/arena_objective_ring.png";
        private const float PulseBeatsPerSecond = 1.1f;
        private const float UrgentBeatsPerSecond = 3.2f;
        private const float RingOuterScale = 1.85f;
        private static readonly Color UrgentColor = new Color(1f, 0.38f, 0.3f, 1f);

        private ArenaObjectiveData _data;
        private Transform _playerTarget;
        private Action<ArenaObjectiveRuntime> _claimed;
        private Action<ArenaObjectiveRuntime> _expired;

        private SpriteRenderer _body;
        private SpriteRenderer _ring;
        private Vector3 _bodyBaseScale;
        private float _ringBaseScale = 1f;
        private float _remainingSeconds;
        private float _lifetimeSeconds;
        private bool _isLive;

        public ArenaObjectiveData Data => _data;

        public float RemainingSeconds => _remainingSeconds;

        public void Initialize(
            ArenaObjectiveData data,
            Vector2 position,
            float lifetimeSeconds,
            Transform playerTarget,
            Action<ArenaObjectiveRuntime> onClaimed,
            Action<ArenaObjectiveRuntime> onExpired)
        {
            _data = data;
            _playerTarget = playerTarget;
            _claimed = onClaimed;
            _expired = onExpired;
            _lifetimeSeconds = Mathf.Max(0.5f, lifetimeSeconds);
            _remainingSeconds = _lifetimeSeconds;
            _isLive = true;

            EnsureVisuals();
            transform.position = position;
            gameObject.SetActive(true);
        }

        // Called when the wave ends before the objective did. Silent: this is neither a claim nor
        // an expiry, so neither callback fires and nothing is credited.
        public void Dismiss()
        {
            _isLive = false;
            gameObject.SetActive(false);
        }

        private void EnsureVisuals()
        {
            if (_body != null)
            {
                return;
            }

            var size = _data != null ? _data.WorldSize : 1.1f;
            var tint = _data != null ? _data.Tint : Color.yellow;

            _body = gameObject.GetComponent<SpriteRenderer>();

            if (_body == null)
            {
                _body = gameObject.AddComponent<SpriteRenderer>();
            }

            // Both files are a ~515px subject on a 1422x702 canvas, so they are fitted by their
            // opaque region - fitting the file would scale the padding and render the cache at
            // roughly a third of the intended size, which is the same trap the Core fell into.
            var bodyTexture = DebugSpriteFactory.LoadTextureFromProjectFile(BodySpritePath);
            var bodySprite = bodyTexture != null ? DebugSpriteFactory.CreateSprite(bodyTexture, 100f) : null;

            if (bodySprite != null)
            {
                _body.sprite = bodySprite;
                _body.color = Color.white;
                DebugSpriteFactory.FitTransformToSpriteContent(transform, bodySprite, bodyTexture, Vector2.one * size);
            }
            else
            {
                _body.sprite = DebugSpriteFactory.CreateSquare(100f);
                _body.color = tint;
                transform.localScale = new Vector3(size, size, 1f);
            }

            // Above the arena floor and props, below projectiles and the player.
            _body.sortingOrder = -3;
            _bodyBaseScale = transform.localScale;

            var ringObject = new GameObject("ClaimRing");
            ringObject.transform.SetParent(transform, false);
            _ring = ringObject.AddComponent<SpriteRenderer>();
            var ringTexture = DebugSpriteFactory.LoadTextureFromProjectFile(RingSpritePath);
            var ringSprite = ringTexture != null ? DebugSpriteFactory.CreateSprite(ringTexture, 100f) : null;
            _ring.sprite = ringSprite != null ? ringSprite : DebugSpriteFactory.CreateSquare(100f);
            _ring.color = new Color(1f, 1f, 1f, 0.85f);
            _ring.sortingOrder = -4;

            // The ring is a child, so its scale multiplies the body's fit. Normalising by the body
            // scale keeps the ring's world size independent of how the cache art happened to be
            // padded, which is what lets RingOuterScale stay a readable "how much bigger" number.
            var bodyScale = Mathf.Max(0.0001f, transform.localScale.x);
            var ringFit = ringSprite != null && ringTexture != null
                ? size / Mathf.Max(0.0001f, DebugSpriteFactory.GetOpaqueSizePixels(ringTexture).x / 100f) / bodyScale
                : 1f;
            ringObject.transform.localScale = new Vector3(ringFit * RingOuterScale, ringFit * RingOuterScale, 1f);
            _ringBaseScale = ringFit;
        }

        private void Update()
        {
            if (!_isLive || _data == null)
            {
                return;
            }

            // Unscaled would keep running under a pause overlay; this must not. Scaled delta is
            // correct here precisely because a paused game should not burn the cache's lifetime.
            _remainingSeconds -= Time.deltaTime;

            if (_remainingSeconds <= 0f)
            {
                _isLive = false;
                gameObject.SetActive(false);
                _expired?.Invoke(this);
                return;
            }

            UpdatePresentation();

            if (_playerTarget == null)
            {
                return;
            }

            var toPlayer = (Vector2)_playerTarget.position - (Vector2)transform.position;

            if (toPlayer.sqrMagnitude <= _data.ClaimRadius * _data.ClaimRadius)
            {
                _isLive = false;
                gameObject.SetActive(false);
                _claimed?.Invoke(this);
            }
        }

        private void UpdatePresentation()
        {
            var isUrgent = _remainingSeconds <= _data.UrgentSeconds;
            var beats = isUrgent ? UrgentBeatsPerSecond : PulseBeatsPerSecond;
            var beat = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f * beats);
            var tint = _data.Tint;

            if (_body != null)
            {
                _body.color = isUrgent ? Color.Lerp(tint, UrgentColor, beat) : tint;
                var scale = 1f + 0.08f * beat;
                transform.localScale = _bodyBaseScale * scale;
            }

            if (_ring != null)
            {
                // The ring closes in as the lifetime drains, so "how long have I got" is readable
                // at arena scale, where a digit would not be.
                var normalized = Mathf.Clamp01(_remainingSeconds / _lifetimeSeconds);
                var ringScale = _ringBaseScale * Mathf.Lerp(1.05f, RingOuterScale, normalized);
                _ring.transform.localScale = new Vector3(ringScale, ringScale, 1f);
                var ringColor = isUrgent ? UrgentColor : tint;
                _ring.color = new Color(ringColor.r, ringColor.g, ringColor.b, 0.18f + 0.2f * beat);
            }
        }
    }
}
