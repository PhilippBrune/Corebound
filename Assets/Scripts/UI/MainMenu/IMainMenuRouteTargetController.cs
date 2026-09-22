using System;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    public interface IMainMenuRouteTargetController
    {
        event Action RouteTargetChanged;

        bool IsRouteTargetOpen { get; }

        MainMenuRoutePanelData ActivePanelData { get; }

        // Live rows for a panel whose content is state, not authored text (the profile panel).
        // Null for every other panel, which keeps its authored detail lines.
        string[] ActivePanelDetailLinesOverride { get; }

        bool TryHandleNavigation(Vector2 navigationInput);

        bool TryHandleConfirm();

        bool TryHandleCancel();
    }
}