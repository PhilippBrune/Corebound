using UnityEngine;

namespace Corebound.Gameplay.Shared
{
    // Small shared math helpers used by hostile combatant controllers (bosses, enemies) that
    // would otherwise each reimplement the same trigonometry independently.
    public static class CombatMath
    {
        public static Vector2 Rotate(Vector2 direction, float angleDegrees)
        {
            var radians = angleDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos).normalized;
        }
    }
}
