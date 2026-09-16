using UnityEngine;

namespace Corebound.UI.MainMenu
{
    public sealed class SerializedMainMenuStateProvider : MonoBehaviour, IMainMenuStateProvider
    {
        [SerializeField] private MainMenuState _state = new MainMenuState(false, false, false);

        public MainMenuState GetCurrentState()
        {
            return _state;
        }

        public void SetState(MainMenuState state)
        {
            _state = state;
        }
    }
}