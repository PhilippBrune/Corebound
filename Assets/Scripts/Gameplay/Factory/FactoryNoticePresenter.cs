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
            NotificationPresenter.Show("+" + eventArgs.Amount + " · " + eventArgs.NodeDisplayName);
        }
    }
}
