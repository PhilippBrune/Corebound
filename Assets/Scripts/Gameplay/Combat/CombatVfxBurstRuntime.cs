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

        public void Initialize(float targetSize, float lifetimeSeconds)
        {
            _targetSize = Mathf.Max(0.05f, targetSize);
            _lifetimeSeconds = Mathf.Max(0.05f, lifetimeSeconds);
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _baseColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
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
            }

            if (_elapsedSeconds >= _lifetimeSeconds)
            {
                Destroy(gameObject);
            }
        }
    }
}
