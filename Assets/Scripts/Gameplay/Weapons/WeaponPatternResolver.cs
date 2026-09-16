using System;
using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    public sealed class WeaponPatternResolver
    {
        public void ForEachShotDirection(Vector2 forwardDirection, WeaponAttackPatternData attackPattern, Action<Vector2> shotDirectionHandler)
        {
            if (shotDirectionHandler == null)
            {
                return;
            }

            var baseDirection = forwardDirection.sqrMagnitude < Mathf.Epsilon ? Vector2.right : forwardDirection.normalized;

            if (attackPattern == null)
            {
                shotDirectionHandler(baseDirection);
                return;
            }

            var projectileCount = attackPattern.ProjectileCount;

            if (projectileCount <= 1)
            {
                shotDirectionHandler(baseDirection);
                return;
            }

            var spreadAngle = attackPattern.SpreadAngleDegrees;
            var startAngle = -spreadAngle * 0.5f;
            var stepAngle = projectileCount > 1 ? spreadAngle / (projectileCount - 1) : 0f;

            for (var index = 0; index < projectileCount; index++)
            {
                var shotAngle = startAngle + stepAngle * index;
                shotDirectionHandler(CombatMath.Rotate(baseDirection, shotAngle));
            }
        }
    }
}