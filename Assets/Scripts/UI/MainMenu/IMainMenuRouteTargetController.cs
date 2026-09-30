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

        // Index of the highlighted row in that override, or -1 when the panel is plain text.
        int ActivePanelSelectedIndex { get; }

        // A title and summary that replace the authored ones while a panel shows something else
        // (the story's premise page in front of New Run). Null keeps the authored text.
        string ActivePanelTitleOverride { get; }

        string ActivePanelSubtitleOverride { get; }

        string ActivePanelSummaryOverride { get; }

        // Mouse selection of a panel row; false when this panel has no selectable rows.
        bool TrySelectPanelRow(int index);

        bool TryHandleNavigation(Vector2 navigationInput);

        bool TryHandleConfirm();

        bool TryHandleCancel();

        // Q / LB and Tab / RB: switches tabs on a tabbed target, and doubles as the profile
        // panel's delete (which has no tabs to switch).
        bool TryHandleTabSwitch(int direction);
    }
}