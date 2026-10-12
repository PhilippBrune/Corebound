using System.Collections.Generic;
using Corebound.Gameplay.Resources;
using Corebound.Gameplay.Towers;
using Corebound.Gameplay.Waves;
using Corebound.Input;
using Corebound.MetaProgression;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // The parts of a class signature that need the player's position or input every frame
    // (CHARACTER_CLASSES_2026-09-30). Attached by PlayerCombatController like the support effects.
    // Everything else a signature does is a number in CharacterEffects that combat reads directly.
    //
    // Engineer: hold Interact next to a tower during a wave to channel an overcharge.
    // Custodian: a tether to the Core while close; stepping out of it sends a shockwave.
    // Salvager: at each wave start, Scrap held past the threshold becomes damage for that wave.
    public sealed class CharacterSignatureRuntime : MonoBehaviour
    {
        private static readonly Color ChannelColor = new Color(1f, 0.82f, 0.35f, 0.9f);
        private static readonly Color TetherColor = new Color(0.6f, 0.9f, 1f, 0.55f);

        private PlayerCombatController _player;
        private IInputReader _input;
        private WaveManager _waveManager;

        // Engineer
        private readonly Dictionary<TowerRuntimeController, int> _overchargesUsed = new Dictionary<TowerRuntimeController, int>();
        private TowerRuntimeController _channelTarget;
        private float _channelTimer;
        private SpriteRenderer _channelRing;
        private bool _overchargeHintShown;
        private float _nextHintCheck;

        // Custodian
        private Transform _core;
        private float _shockwaveCooldown;
        private SpriteRenderer[] _tetherSegments;

#if UNITY_EDITOR
        public static int DebugOverchargeCount;
        public static int DebugShockwaveCount;
#endif

        public void Bind(PlayerCombatController player, IInputReader input)
        {
            _player = player;
            _input = input;
        }

        private void OnEnable()
        {
            WaveEvents.WaveStarted += OnWaveStarted;
            WaveEvents.WaveCompleted += OnWaveCompleted;
        }

        private void OnDisable()
        {
            WaveEvents.WaveStarted -= OnWaveStarted;
            WaveEvents.WaveCompleted -= OnWaveCompleted;
            CharacterEffects.IsTethered = false;
        }

        private void OnDestroy()
        {
            if (_channelRing != null)
            {
                Destroy(_channelRing.gameObject);
            }

            if (_tetherSegments != null && _tetherSegments.Length > 0 && _tetherSegments[0] != null)
            {
                Destroy(_tetherSegments[0].transform.parent.gameObject);
            }
        }

        private void OnWaveStarted(WaveProgressEventArgs eventArgs)
        {
            _overchargesUsed.Clear();

            if (CharacterEffects.Signature != CharacterSignature.Salvager)
            {
                CharacterEffects.HoardDamageMultiplier = 1f;
                return;
            }

            var wallet = FindFirstObjectByType<ResourceWalletService>();
            var held = wallet != null ? wallet.CurrentAmount : 0;
            var excessTens = Mathf.Max(0, held - CharacterEffects.HoardThreshold) / 10;
            CharacterEffects.HoardDamageMultiplier = 1f + excessTens * CharacterEffects.HoardDamagePerTen;

            if (excessTens > 0)
            {
                NotificationPresenter.Show(LocalizationService.Resolve("Hoard: +", "Hort: +")
                    + Mathf.RoundToInt((CharacterEffects.HoardDamageMultiplier - 1f) * 100f)
                    + LocalizationService.Resolve("% damage this wave", " % Schaden in dieser Welle"));
            }
        }

        private void OnWaveCompleted(WaveProgressEventArgs eventArgs)
        {
            CharacterEffects.HoardDamageMultiplier = 1f;
        }

        private void Update()
        {
            if (_player == null)
            {
                return;
            }

            if (_waveManager == null)
            {
                _waveManager = FindFirstObjectByType<WaveManager>();
            }

            var inCombat = _waveManager != null
                && (_waveManager.State == WaveState.Combat || _waveManager.State == WaveState.BossCombat)
                && Time.timeScale > 0f;

            switch (CharacterEffects.Signature)
            {
                case CharacterSignature.Engineer:
                    TickOvercharge(inCombat);
                    break;
                case CharacterSignature.Custodian:
                    TickTether();
                    break;
            }
        }

        // ---- Engineer ----------------------------------------------------------------------------

        private void TickOvercharge(bool inCombat)
        {
            // A new verb with no on-screen indication is not discoverable (the Core guard's lesson):
            // the first time a tower is in reach during combat, say what Interact does there.
            if (!_overchargeHintShown && inCombat && Time.time >= _nextHintCheck)
            {
                _nextHintCheck = Time.time + 0.5f;

                if (FindTowerInReach() != null)
                {
                    _overchargeHintShown = true;
                    NotificationPresenter.Show(LocalizationService.Resolve(
                        "Hold Interact next to a tower to overcharge it",
                        "Halte Interagieren an einem Turm, um ihn zu überladen"));
                }
            }

            var holding = inCombat && _player.IsAlive && _input != null && _input.IsInteractHeld;
            var target = holding ? FindTowerInReach() : null;

            if (target == null || target != _channelTarget || !HasChargeLeft(target))
            {
                _channelTarget = target != null && HasChargeLeft(target) ? target : null;
                _channelTimer = 0f;
            }

            if (_channelTarget == null)
            {
                SetChannelRing(null, 0f);
                return;
            }

            Corebound.Input.InteractRouter.ConsumeHold();
            _channelTimer += Time.deltaTime;
            SetChannelRing(_channelTarget.transform, _channelTimer / CharacterEffects.OverchargeChannelSeconds);

            if (_channelTimer < CharacterEffects.OverchargeChannelSeconds)
            {
                return;
            }

            _overchargesUsed.TryGetValue(_channelTarget, out var used);
            _overchargesUsed[_channelTarget] = used + 1;
            _channelTarget.Overcharge(CharacterEffects.OverchargeSeconds);
            CombatVfxSpawner.SpawnTowerActivationPulse(_channelTarget.transform.position);
#if UNITY_EDITOR
            DebugOverchargeCount++;
#endif
            _channelTarget = null;
            _channelTimer = 0f;
            SetChannelRing(null, 0f);
        }

        private bool HasChargeLeft(TowerRuntimeController tower)
        {
            _overchargesUsed.TryGetValue(tower, out var used);
            return used < CharacterEffects.OverchargeCharges;
        }

        private TowerRuntimeController FindTowerInReach()
        {
            var towers = FindObjectsByType<TowerRuntimeController>(FindObjectsSortMode.None);
            TowerRuntimeController best = null;
            var bestDistance = CharacterEffects.OverchargeReach * CharacterEffects.OverchargeReach;
            var origin = (Vector2)transform.position;

            foreach (var tower in towers)
            {
                if (tower == null || !tower.isActiveAndEnabled)
                {
                    continue;
                }

                var distance = ((Vector2)tower.transform.position - origin).sqrMagnitude;

                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = tower;
                }
            }

            return best;
        }

        // A ring closing in on the tower as the channel fills.
        private void SetChannelRing(Transform tower, float progress)
        {
            if (tower == null)
            {
                if (_channelRing != null && _channelRing.gameObject.activeSelf)
                {
                    _channelRing.gameObject.SetActive(false);
                }

                return;
            }

            if (_channelRing == null)
            {
                var ringObject = new GameObject("OverchargeChannelRing");
                _channelRing = ringObject.AddComponent<SpriteRenderer>();
                _channelRing.sprite = Corebound.Gameplay.Shared.DebugSpriteFactory.ResolveRingSprite();
                _channelRing.sortingOrder = 32;
            }

            _channelRing.gameObject.SetActive(true);
            _channelRing.transform.position = tower.position;
            _channelRing.transform.localScale = Vector3.one * Mathf.Lerp(2.4f, 0.9f, Mathf.Clamp01(progress));
            _channelRing.color = new Color(ChannelColor.r, ChannelColor.g, ChannelColor.b, Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(progress)));
        }

        // ---- Custodian ---------------------------------------------------------------------------

        private void TickTether()
        {
            if (_core == null)
            {
                var coreObject = GameObject.Find(CombatHitReceiver.CoreAnchorObjectName);
                _core = coreObject != null ? coreObject.transform : null;
            }

            _shockwaveCooldown = Mathf.Max(0f, _shockwaveCooldown - Time.deltaTime);

            if (_core == null)
            {
                CharacterEffects.IsTethered = false;
                SetTether(false);
                return;
            }

            var radius = CharacterEffects.TetherRadius;
            var tethered = _player.IsAlive && ((Vector2)(_core.position - transform.position)).sqrMagnitude <= radius * radius;

            if (CharacterEffects.IsTethered && !tethered && _shockwaveCooldown <= 0f)
            {
                ReleaseShockwave();
            }

            CharacterEffects.IsTethered = tethered;
            SetTether(tethered);
        }

        // Stepping out of the tether: the Core throws back everything near it.
        private void ReleaseShockwave()
        {
            _shockwaveCooldown = CharacterEffects.ShockwaveCooldownSeconds;
#if UNITY_EDITOR
            DebugShockwaveCount++;
#endif
            CoreShockwave.Release(gameObject, _core.position, CharacterEffects.ShockwaveRadius,
                CharacterEffects.ShockwaveDamage * CharacterEffects.SignaturePower, CharacterEffects.ShockwaveImpulse, "custodian_shockwave");
        }

        // A thin beam of quads from the player to the Core while tethered.
        private void SetTether(bool visible)
        {
            const int Segments = 6;

            if (!visible)
            {
                if (_tetherSegments != null && _tetherSegments[0] != null && _tetherSegments[0].transform.parent.gameObject.activeSelf)
                {
                    _tetherSegments[0].transform.parent.gameObject.SetActive(false);
                }

                return;
            }

            if (_tetherSegments == null)
            {
                var root = new GameObject("CustodianTether");
                _tetherSegments = new SpriteRenderer[Segments];
                var unit = Corebound.Gameplay.Shared.DebugSpriteFactory.CreateSquare(Corebound.Gameplay.Shared.DebugSpriteFactory.UnitPixelsPerUnit);

                for (var index = 0; index < Segments; index++)
                {
                    var segment = new GameObject("Segment" + index);
                    segment.transform.SetParent(root.transform, false);
                    _tetherSegments[index] = segment.AddComponent<SpriteRenderer>();
                    _tetherSegments[index].sprite = unit;
                    _tetherSegments[index].sortingOrder = 28;
                }
            }

            var parent = _tetherSegments[0].transform.parent.gameObject;

            if (!parent.activeSelf)
            {
                parent.SetActive(true);
            }

            // A little shimmer along the beam so it reads as energy, not a wire.
            var from = (Vector2)transform.position;
            var to = (Vector2)_core.position;

            for (var index = 0; index < Segments; index++)
            {
                var start = Vector2.Lerp(from, to, index / (float)Segments);
                var end = Vector2.Lerp(from, to, (index + 1) / (float)Segments);
                var segment = _tetherSegments[index].transform;
                segment.position = (start + end) * 0.5f;
                segment.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
                segment.localScale = new Vector3(Vector2.Distance(start, end), 0.06f, 1f);
                var alpha = TetherColor.a * (0.7f + 0.3f * Mathf.Sin(Time.time * 6f + index));
                _tetherSegments[index].color = new Color(TetherColor.r, TetherColor.g, TetherColor.b, alpha);
            }
        }
    }
}
