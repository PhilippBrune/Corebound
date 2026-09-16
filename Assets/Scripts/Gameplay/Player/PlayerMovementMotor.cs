using UnityEngine;

namespace Corebound.Gameplay.Player
{
    public sealed class PlayerMovementMotor
    {
        public Vector2 ComputeVelocity(Vector2 moveInput, float moveSpeed)
        {
            if (moveInput.sqrMagnitude < Mathf.Epsilon)
            {
                return Vector2.zero;
            }

            return moveInput.normalized * moveSpeed;
        }
    }
}