using Corebound.Settings;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // Gives Settings.md's Screen Shake Intensity accessibility option a real effect: a short,
    // small kick on the Main Camera whenever the Player or the Core takes damage, scaled by the
    // player's chosen intensity (0 = fully off, matching the accessibility requirement that this
    // be disable-able).
    public sealed class CameraShake : MonoBehaviour
    {
        [SerializeField] private SettingsManager _settingsManager;
        [SerializeField] private float _hitShakeMagnitude = 0.14f;
        [SerializeField] private float _hitShakeDurationSeconds = 0.12f;

        private Vector3 _basePosition;
        private float _shakeTimer;
        private float _currentMagnitude;

        private void Awake()
        {
            _basePosition = transform.localPosition;
        }

        private void OnEnable()
        {
            CombatEvents.DamageTaken += OnDamageTaken;
        }

        private void OnDisable()
        {
            CombatEvents.DamageTaken -= OnDamageTaken;
            transform.localPosition = _basePosition;
        }

        private void OnDamageTaken(DamageEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return;
            }

            var isPlayer = eventArgs.Target.GetComponent<PlayerCombatController>() != null;
            var isCore = eventArgs.Target.name == CombatHitReceiver.CoreAnchorObjectName;

            if (!isPlayer && !isCore)
            {
                return;
            }

            var intensity = _settingsManager != null && _settingsManager.RuntimeSettings != null
                ? _settingsManager.RuntimeSettings.ScreenShakeIntensity
                : 1f;

            if (intensity <= 0f)
            {
                return;
            }

            _shakeTimer = _hitShakeDurationSeconds;
            _currentMagnitude = _hitShakeMagnitude * intensity;
        }

        private void LateUpdate()
        {
            if (_shakeTimer <= 0f)
            {
                transform.localPosition = _basePosition;
                return;
            }

            _shakeTimer -= Time.deltaTime;
            var offset = (Vector3)(Random.insideUnitCircle * _currentMagnitude);
            transform.localPosition = _basePosition + offset;
        }
    }
}
