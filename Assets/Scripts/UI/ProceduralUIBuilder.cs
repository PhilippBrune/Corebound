using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI
{
    // OPEN_TOPICS 2.1: one place that builds procedural UI and reads UIThemeData.
    //
    // UIThemeData has carried PrimaryTextColor, SecondaryTextColor, AccentColor and PanelFrameSprite
    // since it was written, and nothing has ever read any of them - only .Font reached presenters,
    // through UIFontResolver. Meanwhile CreateText and CreateImage were copy-pasted into at least
    // six presenters and drifted apart, so the theme asset describes a look the game does not have.
    //
    // This closes that by giving the colours a resolver in the same shape UIFontResolver already
    // uses, and by putting the shared construction in one place. Presenters migrate onto it one at
    // a time, verified against a screenshot between each - a single sweep across six presenters
    // with no EventSystem and scene-authored layouts is how the 2026-09-22 canvas regressions
    // happened, and this is explicitly not that.
    //
    // Every Resolve* method falls back to the value presenters hardcode today, so a presenter that
    // migrates before the theme asset is filled in looks exactly as it did.
    public static class ProceduralUIBuilder
    {
        private static readonly Color FallbackPrimaryText = new Color(0.95f, 0.97f, 1f, 1f);
        private static readonly Color FallbackSecondaryText = new Color(0.78f, 0.84f, 0.92f, 1f);
        private static readonly Color FallbackAccent = new Color(1f, 0.78f, 0.35f, 1f);

        private static UIThemeData Theme => UIManager.Instance != null ? UIManager.Instance.DefaultTheme : null;

        public static Font ResolveFont()
        {
            return UIFontResolver.Resolve();
        }

        // A theme colour left at default(Color) is fully transparent black, which would render as
        // nothing - treated as "unset" so an half-filled theme asset degrades to the old look
        // rather than to invisible text.
        public static Color ResolvePrimaryTextColor()
        {
            return Pick(Theme != null ? Theme.PrimaryTextColor : default, FallbackPrimaryText);
        }

        public static Color ResolveSecondaryTextColor()
        {
            return Pick(Theme != null ? Theme.SecondaryTextColor : default, FallbackSecondaryText);
        }

        public static Color ResolveAccentColor()
        {
            return Pick(Theme != null ? Theme.AccentColor : default, FallbackAccent);
        }

        public static Sprite ResolvePanelFrameSprite()
        {
            return Theme != null ? Theme.PanelFrameSprite : null;
        }

        public static Text CreateText(
            string name,
            RectTransform parent,
            int fontSize,
            FontStyle fontStyle = FontStyle.Normal,
            TextAnchor anchor = TextAnchor.MiddleLeft,
            Color? color = null)
        {
            return ProceduralUIFactory.CreateText(
                name, parent, ResolveFont(), fontSize, fontStyle, color ?? ResolvePrimaryTextColor(), anchor);
        }

        public static Text CreateSecondaryText(
            string name,
            RectTransform parent,
            int fontSize,
            FontStyle fontStyle = FontStyle.Normal,
            TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            return CreateText(name, parent, fontSize, fontStyle, anchor, ResolveSecondaryTextColor());
        }

        public static Image CreateImage(
            string name,
            RectTransform parent,
            Sprite sprite = null,
            Color? color = null,
            Image.Type imageType = Image.Type.Simple)
        {
            return ProceduralUIFactory.CreateImage(name, parent, sprite, color ?? Color.white, imageType);
        }

        // A themed panel: the theme's frame sprite when it has one, sliced, and a plain fill
        // otherwise. This is the single most duplicated shape across the presenters.
        public static Image CreatePanel(string name, RectTransform parent, Color? tint = null)
        {
            var sprite = ResolvePanelFrameSprite();
            return CreateImage(name, parent, sprite, tint ?? Color.white, sprite != null ? Image.Type.Sliced : Image.Type.Simple);
        }

        public static void StretchToParent(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        public static void StretchToParent(RectTransform rectTransform)
        {
            StretchToParent(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static Color Pick(Color themeColor, Color fallback)
        {
            return themeColor.a > 0.001f ? themeColor : fallback;
        }
    }
}
