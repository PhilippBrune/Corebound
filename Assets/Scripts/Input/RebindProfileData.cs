using System;
using UnityEngine;

namespace Corebound.Input
{
    [CreateAssetMenu(menuName = "Corebound/Input/Rebind Profile Data", fileName = "RebindProfileData")]
    public sealed class RebindProfileData : ScriptableObject
    {
        [SerializeField] private BindingOverrideEntry[] _bindingOverrides = Array.Empty<BindingOverrideEntry>();

        public BindingOverrideEntry[] BindingOverrides => _bindingOverrides;
    }

    [Serializable]
    public struct BindingOverrideEntry
    {
        [SerializeField] private string _mapName;
        [SerializeField] private string _actionName;
        [SerializeField] private int _bindingIndex;
        [SerializeField] private string _bindingPath;

        public string MapName => _mapName;

        public string ActionName => _actionName;

        public int BindingIndex => _bindingIndex;

        public string BindingPath => _bindingPath;
    }
}