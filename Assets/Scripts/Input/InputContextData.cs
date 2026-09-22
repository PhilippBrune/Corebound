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

        // For panels that need a context no asset describes (the loadout panel: UI and System only,
        // so the player neither walks nor fires while choosing).
        public static InputContextData CreateRuntime(bool gameplay, bool build, bool ui, bool system)
        {
            var context = CreateInstance<InputContextData>();
            context._enableGameplay = gameplay;
            context._enableBuild = build;
            context._enableUi = ui;
            context._enableSystem = system;
            context.hideFlags = HideFlags.DontSave;
            return context;
        }
    }
}