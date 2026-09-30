using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // Arc Coil's chain (NEW_CARD_MECHANICS C5-1): a short jagged bolt from the enemy that was hit
    // to each enemy the shock jumped to. Without it a chain kill looks like an enemy dying for no
    // reason. There is no painted art for this, so the bolt
    // is built from stretched one-unit quads, which render with the default sprite material in
    // URP 2D - a LineRenderer would need a material that has to be found by shader name.
    public sealed class CombatShockArcRuntime : MonoBehaviour
    {
        private const int Segments = 4;
        private const float Thickness = 0.07f;
        private const float JitterFraction = 0.18f;
        private const float LifetimeSeconds = 0.14f;

        private static Sprite _unitSprite;
        private static readonly Color ArcColor = new Color(0.62f, 0.9f, 1f, 1f);

        private SpriteRenderer[] _segments;
        private float _age;

        public static void Spawn(Vector2 from, Vector2 to)
        {
            if ((to - from).sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (_unitSprite == null)
            {
                _unitSprite = Corebound.Gameplay.Shared.DebugSpriteFactory.CreateSquare(Corebound.Gameplay.Shared.DebugSpriteFactory.UnitPixelsPerUnit);
            }

            var arcObject = new GameObject("ShockArc");
            arcObject.AddComponent<CombatShockArcRuntime>().Build(from, to);
        }

        private void Build(Vector2 from, Vector2 to)
        {
            var points = new Vector2[Segments + 1];
            var span = to - from;
            var normal = new Vector2(-span.y, span.x).normalized;
            var jitter = span.magnitude * JitterFraction;
            points[0] = from;
            points[Segments] = to;

            for (var index = 1; index < Segments; index++)
            {
                points[index] = from + span * (index / (float)Segments) + normal * Random.Range(-jitter, jitter);
            }

            _segments = new SpriteRenderer[Segments];

            for (var index = 0; index < Segments; index++)
            {
                var start = points[index];
                var end = points[index + 1];
                var segment = new GameObject("Segment");
                segment.transform.SetParent(transform, false);
                segment.transform.position = (start + end) * 0.5f;
                segment.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
                segment.transform.localScale = new Vector3(Vector2.Distance(start, end) + Thickness, Thickness, 1f);
                var renderer = segment.AddComponent<SpriteRenderer>();
                renderer.sprite = _unitSprite;
                renderer.color = ArcColor;
                renderer.sortingOrder = 46;
                _segments[index] = renderer;
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var alpha = 1f - Mathf.Clamp01(_age / LifetimeSeconds);

            if (alpha <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            for (var index = 0; index < _segments.Length; index++)
            {
                var color = ArcColor;
                color.a = alpha;
                _segments[index].color = color;
            }
        }
    }
}
