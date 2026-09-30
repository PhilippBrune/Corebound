using System.Collections.Generic;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Waves;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // The player-side half of the NEW_CARD_MECHANICS support cards (C3-1, 2026-09-29): Field
    // Dressing, Leech Rounds, Reactive Plating and the Sentry Drone. Attached by
    // PlayerCombatController the same way the reticle is, so no scene edit is needed. Every value
    // is pulled from CardService each frame, like the weapon modifiers, so a card picked mid-run
    // or restored from a checkpoint applies without anyone having to notify this.
    //
    // The Core and tower halves (Core Aegis, Overclock Beacon) live with the Core and the towers;
    // this only publishes where the player is, for the tower aura.
    public sealed class PlayerSupportEffects : MonoBehaviour
    {
        private const string DroneSourceId = "sentry_drone";
        private const string DroneSpritePath = "Assets/Art/Combat/Player/sentry_drone.png";

        private static readonly Color ShieldColor = new Color(0.45f, 0.85f, 1f, 0.75f);
        private static readonly Color DroneColor = new Color(0.55f, 0.95f, 0.9f, 1f);
        private static readonly Collider2D[] TargetBuffer = new Collider2D[32];

        private PlayerCombatController _player;
        private CardService _cardService;
        private int _healingKillsThisWave;
        private bool _shieldReady;
        private float _shieldRechargeRemaining;
        private SpriteRenderer _shieldRing;
        private readonly List<Drone> _drones = new List<Drone>();
        private float _orbitAngle;
        private static Sprite _droneSprite;
        private static bool _droneSpriteLoaded;

        public bool IsShieldReady => _shieldReady;

#if UNITY_EDITOR
        // Read by the autopilot's cardsN variant; a shield block or a heal is otherwise invisible
        // in the log.
        public static int DebugShieldBlocks;
        public static int DebugKillHeals;
        public static int DebugWaveHeals;
#endif

        public void Bind(PlayerCombatController player, CardService cardService)
        {
            _player = player;
            _cardService = cardService;
        }

        private void OnEnable()
        {
            WaveEvents.WaveStarted += OnWaveStarted;
            WaveEvents.WaveCompleted += OnWaveCompleted;
            CombatEvents.CombatantDied += OnCombatantDied;
        }

        private void OnDisable()
        {
            WaveEvents.WaveStarted -= OnWaveStarted;
            WaveEvents.WaveCompleted -= OnWaveCompleted;
            CombatEvents.CombatantDied -= OnCombatantDied;

            if (RunCombatEffects.AuraCenter == transform)
            {
                RunCombatEffects.AuraCenter = null;
            }
        }

        private void OnDestroy()
        {
            for (var index = 0; index < _drones.Count; index++)
            {
                if (_drones[index].Renderer != null)
                {
                    Destroy(_drones[index].Renderer.gameObject);
                }
            }

            _drones.Clear();
        }

        private void Update()
        {
            if (_player == null || _cardService == null)
            {
                return;
            }

            var alive = _player.IsAlive;
            RunCombatEffects.AuraCenter = alive ? transform : null;
            UpdateShield(alive);
            UpdateDrones(alive);
        }

        // ---- Field Dressing / Leech Rounds ------------------------------------------------------

        private void OnWaveStarted(WaveProgressEventArgs eventArgs)
        {
            _healingKillsThisWave = 0;
        }

        private void OnWaveCompleted(WaveProgressEventArgs eventArgs)
        {
            var fraction = _cardService != null ? _cardService.HealOnWaveClear : 0f;

            if (fraction > 0f && _player != null && _player.TryRestoreHealthFraction(fraction))
            {
#if UNITY_EDITOR
                DebugWaveHeals++;
#endif
            }
        }

        private void OnCombatantDied(DeathEventArgs eventArgs)
        {
            var amount = _cardService != null ? _cardService.HealPerKill : 0f;

            if (amount <= 0f || _player == null || _healingKillsThisWave >= PlayerSupportRules.HealPerKillCapPerWave)
            {
                return;
            }

            if (eventArgs.DamageContext.SourceType != DamageSourceType.Player || !IsHostile(eventArgs.Victim))
            {
                return;
            }

            if (_player.TryHealAmount(amount))
            {
                _healingKillsThisWave++;
#if UNITY_EDITOR
                DebugKillHeals++;
#endif
            }
        }

        // ---- Reactive Plating --------------------------------------------------------------------

        // Called by PlayerCombatController before a hit lands. True means the shield took it.
        public bool TryAbsorbHit()
        {
            if (!_shieldReady || _cardService == null || _cardService.ShieldStacks <= 0)
            {
                return false;
            }

            _shieldReady = false;
            _shieldRechargeRemaining = PlayerSupportRules.ResolveShieldRechargeSeconds(_cardService.ShieldStacks);
#if UNITY_EDITOR
            DebugShieldBlocks++;
#endif
            CombatVfxSpawner.SpawnShieldBreak(transform.position);
            return true;
        }

        private void UpdateShield(bool alive)
        {
            var stacks = _cardService.ShieldStacks;

            if (stacks <= 0)
            {
                _shieldReady = false;
                _shieldRechargeRemaining = 0f;
            }
            else if (!_shieldReady)
            {
                _shieldRechargeRemaining -= Time.deltaTime;

                if (_shieldRechargeRemaining <= 0f)
                {
                    _shieldReady = true;
                }
            }

            var show = _shieldReady && alive;

            if (!show && _shieldRing == null)
            {
                return;
            }

            if (_shieldRing == null)
            {
                // Not parented: the player object rotates to face the aim, and a ring that turns
                // with it would read as a spinning thing rather than a shell.
                var ringObject = new GameObject("PlayerShieldRing");
                _shieldRing = ringObject.AddComponent<SpriteRenderer>();
                _shieldRing.sprite = Corebound.Gameplay.Shared.DebugSpriteFactory.ResolveRingSprite();
                _shieldRing.color = ShieldColor;
                _shieldRing.sortingOrder = 30;
                ringObject.transform.localScale = Vector3.one * 1.3f;
            }

            if (_shieldRing.gameObject.activeSelf != show)
            {
                _shieldRing.gameObject.SetActive(show);
            }

            if (show)
            {
                _shieldRing.transform.position = transform.position;
                var pulse = 0.65f + 0.15f * Mathf.Sin(Time.time * 4f);
                _shieldRing.color = new Color(ShieldColor.r, ShieldColor.g, ShieldColor.b, pulse);
            }
        }

        // ---- Sentry Drone ------------------------------------------------------------------------

        private sealed class Drone
        {
            public SpriteRenderer Renderer;
            public float Cooldown;
        }

        private void UpdateDrones(bool alive)
        {
            var wanted = alive ? Mathf.Clamp(_cardService.SentryDrones, 0, 4) : 0;

            while (_drones.Count < wanted)
            {
                _drones.Add(CreateDrone(_drones.Count));
            }

            while (_drones.Count > wanted)
            {
                var last = _drones[_drones.Count - 1];
                _drones.RemoveAt(_drones.Count - 1);

                if (last.Renderer != null)
                {
                    Destroy(last.Renderer.gameObject);
                }
            }

            if (_drones.Count == 0)
            {
                return;
            }

            _orbitAngle = Mathf.Repeat(_orbitAngle + PlayerSupportRules.DroneOrbitDegreesPerSecond * Time.deltaTime, 360f);
            var centre = (Vector2)transform.position;

            for (var index = 0; index < _drones.Count; index++)
            {
                var drone = _drones[index];
                var angle = (_orbitAngle + index * 360f / _drones.Count) * Mathf.Deg2Rad;
                var position = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * PlayerSupportRules.DroneOrbitRadius;
                drone.Renderer.transform.position = position;
                drone.Cooldown -= Time.deltaTime;

                if (drone.Cooldown > 0f || Time.timeScale <= 0f)
                {
                    continue;
                }

                var target = FindNearestHostile(position);

                if (target == null)
                {
                    continue;
                }

                var direction = ((Vector2)target.position - position).normalized;

                if (_player.FireSupportProjectile(position, direction, PlayerSupportRules.DroneDamageFraction, DroneSourceId))
                {
                    drone.Cooldown = PlayerSupportRules.DroneFireIntervalSeconds;
                    // sentry_drone.png (Asset 109) is drawn with its gun pointing down, like the player.
                    drone.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f);
                }
            }
        }

        private Drone CreateDrone(int index)
        {
            var droneObject = new GameObject("SentryDrone_" + index);
            var renderer = droneObject.AddComponent<SpriteRenderer>();
            var sprite = ResolveDroneSprite();

            if (sprite != null)
            {
                renderer.sprite = sprite;

                // Measured by the opaque drone, not the 911 px canvas it sits in.
                Corebound.Gameplay.Shared.DebugSpriteFactory.FitTransformToSpriteContent(droneObject.transform, sprite, sprite.texture, new Vector2(0.5f, 0.5f));
            }
            else
            {
                // Only if sentry_drone.png (Asset 109) cannot be read: a small bright diamond.
                renderer.sprite = Corebound.Gameplay.Shared.DebugSpriteFactory.CreateSquare(Corebound.Gameplay.Shared.DebugSpriteFactory.UnitPixelsPerUnit);
                renderer.color = DroneColor;
                droneObject.transform.localScale = Vector3.one * 0.28f;
                droneObject.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            }

            renderer.sortingOrder = 31;
            droneObject.transform.position = transform.position;
            return new Drone { Renderer = renderer, Cooldown = PlayerSupportRules.DroneFireIntervalSeconds * (1f + index * 0.5f) };
        }

        private static Sprite ResolveDroneSprite()
        {
            if (!_droneSpriteLoaded)
            {
                _droneSpriteLoaded = true;
                _droneSprite = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(DroneSpritePath);
            }

            return _droneSprite;
        }

        private static Transform FindNearestHostile(Vector2 position)
        {
            var count = Physics2D.OverlapCircleNonAlloc(position, PlayerSupportRules.DroneRange, TargetBuffer);
            Transform nearest = null;
            var nearestDistance = float.MaxValue;

            for (var index = 0; index < count; index++)
            {
                var collider = TargetBuffer[index];

                if (collider == null)
                {
                    continue;
                }

                var owner = collider.attachedRigidbody != null ? collider.attachedRigidbody.gameObject : collider.gameObject;

                if (!IsHostile(owner))
                {
                    continue;
                }

                var receiver = owner.GetComponent<CombatHitReceiver>();

                if (receiver == null || !receiver.IsAlive)
                {
                    continue;
                }

                var distance = ((Vector2)owner.transform.position - position).sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = owner.transform;
                }
            }

            return nearest;
        }

        private static bool IsHostile(GameObject candidate)
        {
            return candidate != null
                && (candidate.GetComponent<Corebound.Gameplay.Enemies.EnemyRuntimeController>() != null
                    || candidate.GetComponent<Corebound.Gameplay.Bosses.BossRuntimeController>() != null);
        }
    }
}
