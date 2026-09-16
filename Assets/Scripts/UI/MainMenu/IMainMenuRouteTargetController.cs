using System;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    public interface IMainMenuRouteTargetController
    {
        event Action RouteTargetChanged;

        bool IsRouteTargetOpen { get; }

        MainMenuRoutePanelData ActivePanelData { get; }

        bool TryHandleNavigation(Vector2 navigationInput);

        bool TryHandleConfirm();

        bool TryHandleCancel();
    }
}