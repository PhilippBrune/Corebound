using System;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    [CreateAssetMenu(menuName = "Corebound/Main Menu/Main Menu Flow Data", fileName = "MainMenuFlowData")]
    public sealed class MainMenuFlowData : ScriptableObject
    {
        [SerializeField] private bool _showPressStartIntro = true;
        [SerializeField] private MenuEntryData[] _entries = Array.Empty<MenuEntryData>();

        public bool ShowPressStartIntro => _showPressStartIntro;

        public MenuEntryData[] Entries => _entries;
    }
}