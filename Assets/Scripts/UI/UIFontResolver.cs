using UnityEngine;

namespace Corebound.UI
{
    // Shared home for the "resolve a runtime font" logic previously copy-pasted identically
    // across ten presenter classes. Also wires in UIThemeData.Font (via UIManager.Instance),
    // which UIThemeData exposed but no presenter actually read - a UITheme_Default.asset with a
    // custom font now takes effect everywhere instead of every presenter always falling back to
    // the Unity builtin font.
    public static class UIFontResolver
    {
        public static Font Resolve()
        {
            var themeFont = UIManager.Instance != null && UIManager.Instance.DefaultTheme != null ? UIManager.Instance.DefaultTheme.Font : null;

            if (themeFont != null)
            {
                return themeFont;
            }

            var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return builtinFont != null ? builtinFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
