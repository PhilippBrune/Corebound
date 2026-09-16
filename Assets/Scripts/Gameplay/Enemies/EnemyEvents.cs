using System;
using UnityEngine;

namespace Corebound.Gameplay.Enemies
{
    public readonly struct EnemyLifecycleEventArgs
    {
        public EnemyLifecycleEventArgs(GameObject enemyObject, EnemyData enemyData, EnemyEliteModifierData eliteModifierData)
        {
            EnemyObject = enemyObject;
            EnemyData = enemyData;
            EliteModifierData = eliteModifierData;
        }

        public GameObject EnemyObject { get; }

        public EnemyData EnemyData { get; }

        public EnemyEliteModifierData EliteModifierData { get; }
    }

    public readonly struct EnemyTargetChangedEventArgs
    {
        public EnemyTargetChangedEventArgs(GameObject enemyObject, EnemyData enemyData, Transform target, bool isTargetingPlayer)
        {
            EnemyObject = enemyObject;
            EnemyData = enemyData;
            Target = target;
            IsTargetingPlayer = isTargetingPlayer;
        }

        public GameObject EnemyObject { get; }

        public EnemyData EnemyData { get; }

        public Transform Target { get; }

        public bool IsTargetingPlayer { get; }
    }

    public static class EnemyEvents
    {
        public static event Action<EnemyLifecycleEventArgs> EnemySpawned;

        public static event Action<EnemyLifecycleEventArgs> EnemyKilled;

        public static event Action<EnemyTargetChangedEventArgs> EnemyTargetChanged;

        public static void RaiseEnemySpawned(EnemyLifecycleEventArgs eventArgs)
        {
            EnemySpawned?.Invoke(eventArgs);
        }

        public static void RaiseEnemyKilled(EnemyLifecycleEventArgs eventArgs)
        {
            EnemyKilled?.Invoke(eventArgs);
        }

        public static void RaiseEnemyTargetChanged(EnemyTargetChangedEventArgs eventArgs)
        {
            EnemyTargetChanged?.Invoke(eventArgs);
        }
    }
}