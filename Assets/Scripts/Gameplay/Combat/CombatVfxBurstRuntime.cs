using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // Drives a single spawned burst through a short expand-then-fade curve, then self-destructs.
    // Kept as a plain per-instance component rather than pooled: combat VFX bursts are short-lived
    // and low-frequency enough (one per hit/death, not per frame) that GC churn here is not a
    // realistic performance concern at this project's scale.
    internal sealed class CombatVfxBurstRuntime : MonoBehaviour
    {
        private float _targetSize;
        private float _lifetimeSeconds;
        private float _elapsedSeconds;
        private SpriteRenderer _spriteRenderer;
        private Color _baseColor;

        // An optional frame sequence, played across the same lifetime as the scale/fade curve.
        // Added 2026-09-27 so the delivered multi-frame sheets can be used at all: every VFX before
        // this was a single still that only grew and faded, so a four-frame flame or a five-frame
        // shockwave had nowhere to go and sat unwired on disk.
        private Sprite[] _frames;

        public void Initialize(float targetSize, float lifetimeSeconds)
        {
            Initialize(targetSize, lifetimeSeconds, null);
        }

        public void Initialize(float targetSize, float lifetimeSeconds, Sprite[] frames)
        {
            _targetSize = Mathf.Max(0.05f, targetSize);
            _lifetimeSeconds = Mathf.Max(0.05f, lifetimeSeconds);
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _baseColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
            _frames = frames != null && frames.Length > 0 ? frames : null;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            _elapsedSeconds += Time.deltaTime;
            var progress = Mathf.Clamp01(_elapsedSeconds / _lifetimeSeconds);
            var eased = 1f - (1f - progress) * (1f - progress);
            var scale = Mathf.Lerp(0.05f, _targetSize, eased);
            transform.localScale = new Vector3(scale, scale, 1f);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, Mathf.Lerp(_baseColor.a, 0f, progress));

                if (_frames != null)
                {
                    // Min() rather than a modulo: the sequence plays once and holds its last frame
                    // while the fade finishes, instead of looping back to frame 0 as it disappears.
                    var frame = Mathf.Min(_frames.Length - 1, Mathf.FloorToInt(progress * _frames.Length));
                    _spriteRenderer.sprite = _frames[frame];
                }
            }

            if (_elapsedSeconds >= _lifetimeSeconds)
            {
                Destroy(gameObject);
            }
        }
    }
}
