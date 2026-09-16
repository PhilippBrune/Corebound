using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Combat/Combat Balance", fileName = "CombatBalance")]
    public sealed class CombatBalanceData : ScriptableObject
    {
        [SerializeField] private float _playerInvulnerabilitySeconds = 0.35f;
        [SerializeField] private float _controllerSoftAimAngleDegrees = 18f;
        [SerializeField] private float _controllerSoftAimRange = 9f;
        [SerializeField] private float _controllerSoftAimMagnetism = 0.65f;
        [SerializeField] private LayerMask _enemyLayerMask = Physics2D.DefaultRaycastLayers;

        public float PlayerInvulnerabilitySeconds => _playerInvulnerabilitySeconds;

        public float ControllerSoftAimAngleDegrees => _controllerSoftAimAngleDegrees;

        public float ControllerSoftAimRange => _controllerSoftAimRange;

        public float ControllerSoftAimMagnetism => _controllerSoftAimMagnetism;

        public LayerMask EnemyLayerMask => _enemyLayerMask;
    }
}