using System;
using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    public sealed class WeaponPatternResolver
    {
        // A weapon authored as a single shot has no fan for Multishot to widen, so extra shots have
        // to introduce one - otherwise every added projectile travels the same line and reads as
        // one thicker bullet rather than more of them.
        private const float DefaultExtraSpreadDegrees = 9f;

        public void ForEachShotDirection(Vector2 forwardDirection, WeaponAttackPatternData attackPattern, Action<Vector2> shotDirectionHandler)
        {
            ForEachShotDirection(forwardDirection, attackPattern, 0, shotDirectionHandler);
        }

        // additionalProjectiles is WeaponModifierState.AdditionalProjectiles - Weapons.md's
        // "Multishot" hook.
        public void ForEachShotDirection(Vector2 forwardDirection, WeaponAttackPatternData attackPattern, int additionalProjectiles, Action<Vector2> shotDirectionHandler)
        {
            ForEachShotDirection(forwardDirection, attackPattern, additionalProjectiles, 1f, false, shotDirectionHandler);
        }

        // spreadMultiplier and fullRing are WeaponLoadout.md Set Bonus rules (Spread Aligned narrows
        // the fan; Spread Perfected fires a full ring). The fan count is unchanged either way.
        public void ForEachShotDirection(Vector2 forwardDirection, WeaponAttackPatternData attackPattern, int additionalProjectiles, float spreadMultiplier, bool fullRing, Action<Vector2> shotDirectionHandler)
        {
            if (shotDirectionHandler == null)
            {
                return;
            }

            if (fullRing)
            {
                var ringBase = forwardDirection.sqrMagnitude < Mathf.Epsilon ? Vector2.right : forwardDirection.normalized;
                var ringCount = Mathf.Max(6, (attackPattern != null ? attackPattern.ProjectileCount : 1) + Mathf.Max(0, additionalProjectiles));

                // Divides by count, not count - 1, so the first and last shot do not share a bearing.
                for (var index = 0; index < ringCount; index++)
                {
                    shotDirectionHandler(CombatMath.Rotate(ringBase, 360f * index / ringCount));
                }

                return;
            }

            var baseDirection = forwardDirection.sqrMagnitude < Mathf.Epsilon ? Vector2.right : forwardDirection.normalized;
            var extraProjectiles = Mathf.Max(0, additionalProjectiles);

            if (attackPattern == null)
            {
                EmitFan(baseDirection, 1 + extraProjectiles, DefaultExtraSpreadDegrees * extraProjectiles, shotDirectionHandler);
                return;
            }

            var projectileCount = attackPattern.ProjectileCount + extraProjectiles;

            if (projectileCount <= 1)
            {
                shotDirectionHandler(baseDirection);
                return;
            }

            var spreadAngle = attackPattern.SpreadAngleDegrees * Mathf.Max(0.05f, spreadMultiplier);

            if (spreadAngle <= Mathf.Epsilon)
            {
                spreadAngle = DefaultExtraSpreadDegrees * extraProjectiles;
            }

            EmitFan(baseDirection, projectileCount, spreadAngle, shotDirectionHandler);
        }

        private static void EmitFan(Vector2 baseDirection, int projectileCount, float spreadAngle, Action<Vector2> shotDirectionHandler)
        {
            if (projectileCount <= 1)
            {
                shotDirectionHandler(baseDirection);
                return;
            }

            var startAngle = -spreadAngle * 0.5f;
            var stepAngle = spreadAngle / (projectileCount - 1);

            for (var index = 0; index < projectileCount; index++)
            {
                var shotAngle = startAngle + stepAngle * index;
                shotDirectionHandler(CombatMath.Rotate(baseDirection, shotAngle));
            }
        }
    }
}
