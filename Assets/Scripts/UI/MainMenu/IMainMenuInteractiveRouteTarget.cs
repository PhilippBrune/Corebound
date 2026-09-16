using System;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    public interface IMainMenuInteractiveRouteTarget
    {
        event Action TargetChanged;

        bool IsOpen { get; }

        MainMenuRoute Route { get; }

        void Open();

        void Close();

        bool TryHandleNavigation(Vector2 navigationInput);

        bool TryHandleConfirm();

        bool TryHandleCancel();
    }
}