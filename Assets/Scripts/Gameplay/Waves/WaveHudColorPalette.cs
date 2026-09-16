using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    // Extracted from WaveStatePresenter (God-object split, 2026-09-08): pure HUD color lookups
    // with no dependency on the presenter's live data binding, procedural construction, or
    // editor-scaffolding state.
    internal static class WaveHudColorPalette
    {
        public static Color ResolveBannerColor(bool isEliteEncounter, bool forceWarning)
        {
            if (forceWarning)
            {
                return new Color(1f, 0.9f, 0.62f, 0.98f);
            }

            if (isEliteEncounter)
            {
                return new Color(1f, 0.86f, 0.58f, 0.98f);
            }

            return new Color(0.92f, 0.96f, 1f, 0.98f);
        }
    }
}
