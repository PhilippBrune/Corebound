using UnityEngine;

namespace Corebound.Input
{
    [CreateAssetMenu(menuName = "Corebound/Input/Input Context Data", fileName = "InputContextData")]
    public sealed class InputContextData : ScriptableObject
    {
        [SerializeField] private bool _enableGameplay;
        [SerializeField] private bool _enableBuild;
        [SerializeField] private bool _enableUi;
        [SerializeField] private bool _enableSystem = true;

        public bool EnableGameplay => _enableGameplay;

        public bool EnableBuild => _enableBuild;

        public bool EnableUi => _enableUi;

        public bool EnableSystem => _enableSystem;
    }
}