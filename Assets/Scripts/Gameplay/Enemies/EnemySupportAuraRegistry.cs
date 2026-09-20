using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Gameplay.Enemies
{
    // Enemies.md names four example roles: "direkte Annaeherung, Fernkampfdruck, Flaechenkontrolle
    // oder Support fuer andere Enemies". The Support role needs nearby enemies to read a buff that
    // is not theirs, which is the one thing the existing per-enemy data model cannot express.
    //
    // A registry rather than a physics overlap or a per-frame FindObjectsOfType: emitters are few
    // (usually zero or one per wave), readers are many, and reads happen every FixedUpdate on every
    // enemy. Registration is by emitter, so an enemy returned to the pool stops contributing the
    // moment it is disabled.
    //
    // The multipliers compose multiplicatively with EnemyEliteModifierData's, read live at the same
    // places - a support aura is deliberately the same *kind* of thing as an elite modifier, so an
    // elite enemy standing in an aura gets both.
    public static class EnemySupportAuraRegistry
    {
        private readonly struct Emitter
        {
            public Emitter(Transform source, float radius, float speedMultiplier, float damageMultiplier)
            {
                Source = source;
                Radius = radius;
                SpeedMultiplier = speedMultiplier;
                DamageMultiplier = damageMultiplier;
            }

            public Transform Source { get; }

            public float Radius { get; }

            public float SpeedMultiplier { get; }

            public float DamageMultiplier { get; }
        }

        private static readonly List<Emitter> Emitters = new List<Emitter>();

        public static bool HasEmitters => Emitters.Count > 0;

        public static void Register(Transform source, float radius, float speedMultiplier, float damageMultiplier)
        {
            if (source == null || radius <= 0f)
            {
                return;
            }

            Unregister(source);
            Emitters.Add(new Emitter(source, radius, speedMultiplier, damageMultiplier));
        }

        public static void Unregister(Transform source)
        {
            for (var index = Emitters.Count - 1; index >= 0; index--)
            {
                if (Emitters[index].Source == null || Emitters[index].Source == source)
                {
                    Emitters.RemoveAt(index);
                }
            }
        }

        // Returns 1 when nothing is in range, so callers can multiply unconditionally.
        public static float ResolveSpeedMultiplier(Vector2 position, Transform self)
        {
            return Resolve(position, self, true);
        }

        public static float ResolveDamageMultiplier(Vector2 position, Transform self)
        {
            return Resolve(position, self, false);
        }

        // Finds the nearest emitter other than `self`, so a Support enemy can steer toward the
        // group it is meant to be reinforcing. Returns null when it is the only one.
        public static Transform FindNearestEmitter(Vector2 position, Transform self)
        {
            Transform nearest = null;
            var nearestDistanceSquared = float.MaxValue;

            for (var index = 0; index < Emitters.Count; index++)
            {
                var emitter = Emitters[index];

                if (emitter.Source == null || emitter.Source == self)
                {
                    continue;
                }

                var distanceSquared = ((Vector2)emitter.Source.position - position).sqrMagnitude;

                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                nearest = emitter.Source;
            }

            return nearest;
        }

        // Called when a run ends, so a stale Transform from a destroyed scene cannot survive into
        // the next run. Static state outliving a scene load is exactly the class of bug that cost
        // this project a session already.
        public static void Clear()
        {
            Emitters.Clear();
        }

        private static float Resolve(Vector2 position, Transform self, bool isSpeed)
        {
            if (Emitters.Count == 0)
            {
                return 1f;
            }

            var multiplier = 1f;

            for (var index = Emitters.Count - 1; index >= 0; index--)
            {
                var emitter = Emitters[index];

                if (emitter.Source == null)
                {
                    Emitters.RemoveAt(index);
                    continue;
                }

                // A Support enemy does not buff itself; two of them standing together do buff
                // each other, which is the intended "they are worse in pairs" pressure.
                if (emitter.Source == self)
                {
                    continue;
                }

                if (((Vector2)emitter.Source.position - position).sqrMagnitude > emitter.Radius * emitter.Radius)
                {
                    continue;
                }

                multiplier *= isSpeed ? emitter.SpeedMultiplier : emitter.DamageMultiplier;
            }

            return multiplier;
        }
    }
}
