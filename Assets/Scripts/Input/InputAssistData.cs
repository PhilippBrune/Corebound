using UnityEngine;

namespace Corebound.Input
{
    [CreateAssetMenu(menuName = "Corebound/Input/Input Assist Data", fileName = "InputAssistData")]
    public sealed class InputAssistData : ScriptableObject
    {
        // Mirrors _menuNavigateThreshold's own default below, so every call site that falls
        // back to this constant when no InputAssistData is assigned stays in sync with it.
        public const float DefaultMenuNavigateThreshold = 0.5f;

        [SerializeField] [Range(0f, 1f)] private float _controllerAimAssistStrength = 0.5f;
        [SerializeField] [Range(0.1f, 1f)] private float _menuNavigateThreshold = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float _leftStickDeadzone = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float _rightStickDeadzone = 0.2f;

        public float ControllerAimAssistStrength => _controllerAimAssistStrength;

        public float MenuNavigateThreshold => _menuNavigateThreshold;

        public float LeftStickDeadzone => _leftStickDeadzone;

        public float RightStickDeadzone => _rightStickDeadzone;
    }
}