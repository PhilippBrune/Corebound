using UnityEngine;

namespace Corebound.UI
{
    // Shrinks a fixed-size panel uniformly when the canvas is narrower or shorter than it.
    //
    // The Progression, Records and Settings panels are laid out at 1500-1800 x 940-1020 reference
    // units for a 16:9 screen. Every canvas scales with match 0.5, so on a squarer window - a
    // maximised Game view, a 16:10 or 4:3 monitor - the canvas is narrower than 1920 units and the
    // panel ran off both sides (seen 2026-09-29 at 1734 x 1399). Scaling the panel keeps its whole
    // layout intact instead of re-flowing every row.
    public static class UIPanelFit
    {
        public static void Fit(RectTransform panel, Vector2 designSize, float margin = 0.96f)
        {
            var parent = panel != null ? panel.parent as RectTransform : null;

            if (parent == null || designSize.x <= 0f || designSize.y <= 0f)
            {
                return;
            }

            var available = parent.rect.size * margin;
            var scale = Mathf.Min(1f, Mathf.Min(available.x / designSize.x, available.y / designSize.y));

            if (scale > 0f && !Mathf.Approximately(panel.localScale.x, scale))
            {
                panel.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
