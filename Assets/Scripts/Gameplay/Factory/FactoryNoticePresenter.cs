using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Factory
{
    // Deliberately reuses the existing system-notice surface (NotificationPresenter) instead of
    // building a second procedural UI hierarchy: Factory.md explicitly requires production
    // events to stay a supporting signal, not a dominating one ("Produktionsereignisse werden als
    // unterstützende, nicht dominierende Signale dargestellt"), and a small shared toast already
    // satisfies that without duplicating the CreateText/CreateImage boilerplate every other
    // presenter in this codebase repeats.
    public sealed class FactoryNoticePresenter : MonoBehaviour
    {
        [SerializeField] private float _minNoticeIntervalSeconds = 4f;

        private float _cooldownTimer;
        private FactoryService _factoryService;

        private void OnEnable()
        {
            FactoryEvents.ResourceGenerated += OnResourceGenerated;
        }

        private void OnDisable()
        {
            FactoryEvents.ResourceGenerated -= OnResourceGenerated;
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        private void OnResourceGenerated(ResourceGeneratedEventArgs eventArgs)
        {
            if (_cooldownTimer > 0f || eventArgs.Amount <= 0)
            {
                return;
            }

            _cooldownTimer = Mathf.Max(0f, _minNoticeIntervalSeconds);

            // OPEN_TOPICS 2.2, half two: "did it just pay?". The amount already *was* amplified -
            // FactoryService applies amplifiers before raising this - but the toast never said so,
            // so a player saw a bigger number with no reason attached and could not connect it to
            // the towers they had built. Naming the multiplier is what closes that loop.
            if (_factoryService == null)
            {
                _factoryService = FindFirstObjectByType<FactoryService>();
            }

            var multiplier = _factoryService != null ? _factoryService.ActiveAmplifierMultiplier : 1f;
            var amplified = multiplier > 1.001f;

            NotificationPresenter.Show("+" + eventArgs.Amount + " · " + eventArgs.NodeDisplayName
                + (amplified ? "  (x" + multiplier.ToString("0.0#") + ")" : string.Empty));

            if (amplified)
            {
                // A short pulse at the Core ties the payout to a place in the arena, so the system
                // is visible even when the toast is missed.
                var core = GameObject.Find(Corebound.Gameplay.Combat.CombatHitReceiver.CoreAnchorObjectName);

                if (core != null)
                {
                    Corebound.Gameplay.Combat.CombatVfxSpawner.SpawnTowerActivationPulse(core.transform.position);
                }
            }
        }
    }
}
