using Corebound.Gameplay.Weapons;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public sealed class PlayerAimResolver
    {
        // Reused across calls so the once-per-shot soft-aim scan doesn't allocate a fresh
        // Collider2D[] every time a gamepad player fires.
        private static readonly Collider2D[] SoftAimHitBuffer = new Collider2D[16];

        public Vector2 ResolveAimDirection(
            Vector2 moveInput,
            Vector2 aimInput,
            Vector2 origin,
            bool isUsingGamepad,
            CombatBalanceData combatBalance,
            WeaponAimAssistProfile aimAssistProfile)
        {
            var baseDirection = ResolveBaseDirection(moveInput, aimInput, origin, isUsingGamepad);

            if (!isUsingGamepad || combatBalance == null || baseDirection.sqrMagnitude < Mathf.Epsilon)
            {
                return baseDirection;
            }

            var target = FindSoftAimTarget(origin, baseDirection, combatBalance, aimAssistProfile);

            if (target == null)
            {
                return baseDirection;
            }

            var targetDirection = ((Vector2)target.position - origin).normalized;
            return Vector2.Lerp(baseDirection, targetDirection, combatBalance.ControllerSoftAimMagnetism * ResolveAssistMultiplier(aimAssistProfile)).normalized;
        }

        private static Vector2 ResolveBaseDirection(Vector2 moveInput, Vector2 aimInput, Vector2 origin, bool isUsingGamepad)
        {
            if (isUsingGamepad)
            {
                if (aimInput.sqrMagnitude >= Mathf.Epsilon)
                {
                    return aimInput.normalized;
                }

                if (moveInput.sqrMagnitude >= Mathf.Epsilon)
                {
                    return moveInput.normalized;
                }

                return Vector2.right;
            }

            if (aimInput.sqrMagnitude >= Mathf.Epsilon)
            {
                var mainCamera = Camera.main;

                if (mainCamera != null)
                {
                    var worldAimPosition = (Vector2)mainCamera.ScreenToWorldPoint(aimInput);
                    var worldDirection = worldAimPosition - origin;

                    if (worldDirection.sqrMagnitude >= Mathf.Epsilon)
                    {
                        return worldDirection.normalized;
                    }
                }
            }

            if (moveInput.sqrMagnitude >= Mathf.Epsilon)
            {
                return moveInput.normalized;
            }

            return Vector2.right;
        }

        private static Transform FindSoftAimTarget(Vector2 origin, Vector2 baseDirection, CombatBalanceData combatBalance, WeaponAimAssistProfile aimAssistProfile)
        {
            var assistMultiplier = ResolveAssistMultiplier(aimAssistProfile);
            var hitCount = Physics2D.OverlapCircleNonAlloc(origin, combatBalance.ControllerSoftAimRange * assistMultiplier, SoftAimHitBuffer, combatBalance.EnemyLayerMask);
            var bestScore = float.MinValue;
            Transform bestTarget = null;

            for (var index = 0; index < hitCount; index++)
            {
                var candidate = SoftAimHitBuffer[index];

                if (candidate == null)
                {
                    continue;
                }

                var direction = ((Vector2)candidate.transform.position - origin).normalized;
                var angle = Vector2.Angle(baseDirection, direction);

                if (angle > combatBalance.ControllerSoftAimAngleDegrees * assistMultiplier)
                {
                    continue;
                }

                var distance = Vector2.Distance(origin, candidate.transform.position);

                if (distance <= 0.05f)
                {
                    continue;
                }

                var score = (combatBalance.ControllerSoftAimRange - distance) - angle * 0.1f;

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestTarget = candidate.transform;
            }

            return bestTarget;
        }

        private static float ResolveAssistMultiplier(WeaponAimAssistProfile aimAssistProfile)
        {
            switch (aimAssistProfile)
            {
                case WeaponAimAssistProfile.Low:
                    return 0.8f;
                case WeaponAimAssistProfile.High:
                    return 1.2f;
                default:
                    return 1f;
            }
        }
    }
}