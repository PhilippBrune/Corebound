using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // A short world-space "CRIT 24" that rises and fades above the hit (playtest 2026-09-22: crit
    // hit feedback). TextMesh rather than a UI canvas: it lives in the world at the impact point and
    // needs no canvas per popup. Capped, so a Spread Perfected ring of crits cannot spawn fifty.
    public sealed class CriticalHitPopup : MonoBehaviour
    {
        private const float LifetimeSeconds = 0.65f;
        private const float RiseDistance = 0.7f;
        private const int MaxLivePopups = 12;

        private static readonly Color PopupColor = new Color(1f, 0.85f, 0.3f, 1f);
        private static int _livePopups;

        private TextMesh _text;
        private Vector3 _start;
        private float _age;

        public static void Spawn(Vector2 position, float damage)
        {
            if (_livePopups >= MaxLivePopups)
            {
                return;
            }

            var popupObject = new GameObject("CriticalHitPopup");
            var jitter = new Vector3(Random.Range(-0.2f, 0.2f), 0.25f, 0f);
            popupObject.transform.position = (Vector3)position + jitter;
            var text = popupObject.AddComponent<TextMesh>();
            text.text = "CRIT " + Mathf.RoundToInt(damage);
            text.fontSize = 48;
            text.characterSize = 0.06f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.color = PopupColor;

            // A TextMesh made in code has no font and no material and would draw nothing.
            var font = Corebound.UI.UIFontResolver.Resolve();
            var renderer = popupObject.GetComponent<MeshRenderer>();

            if (font != null)
            {
                text.font = font;
                renderer.sharedMaterial = font.material;
            }

            // Above enemies and projectiles: this is feedback, and it is gone in under a second.
            renderer.sortingOrder = 60;
            var popup = popupObject.AddComponent<CriticalHitPopup>();
            popup._text = text;
            popup._start = popupObject.transform.position;
            _livePopups++;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var progress = Mathf.Clamp01(_age / LifetimeSeconds);
            transform.position = _start + Vector3.up * (RiseDistance * progress);

            // Pops in large, then settles, then fades - reads as an impact rather than a label.
            var scale = progress < 0.15f ? Mathf.Lerp(1.5f, 1f, progress / 0.15f) : 1f;
            transform.localScale = Vector3.one * scale;
            var color = PopupColor;
            color.a = 1f - Mathf.SmoothStep(0.4f, 1f, progress);
            _text.color = color;

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            _livePopups = Mathf.Max(0, _livePopups - 1);
        }
    }
}
