using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI
{
    // Consolidates the CreateText/CreateImage procedural-UI-builder pattern that was duplicated,
    // byte-for-byte identical, across 8 [ExecuteAlways] presenters (ShopPresenter,
    // CardRewardPresenter, WaveStatePresenter, BossHudPresenter, MainMenuCanvasPresenter,
    // MainMenuRoutePanelPresenter, SettingsRouteTargetPresenter, MetaProgressionRouteTargetPresenter).
    // MainMenuGlyphHintBar's CreateText is deliberately not routed through here - it has a
    // genuinely different signature (no color/alignment, Overflow instead of Wrap) rather than an
    // accidental duplicate, so forcing it through this shared method would change its behavior.
    internal static class ProceduralUIFactory
    {
        // Find-existing-by-name-first instead of always creating a new GameObject: every one of the
        // 8 presenters listed above calls this from Awake()/a BuildPanel() fallback that is only
        // meant to run once, but [ExecuteAlways] components re-run Awake() on every script recompile
        // (domain reload) in the Editor, and a caller's authored-layout binding can fail across some
        // of those cycles. ShopPresenter's ShopPresenterRoot accumulated six duplicate Overlay/Panel
        // pairs this way over one long editing session before its authored layout took over. Reusing
        // an existing same-named child and reapplying properties is behaviorally identical to a fresh
        // create for the normal single-call case, and just avoids leaving duplicates behind otherwise.
        public static Text CreateText(string name, RectTransform parent, Font font, int fontSize, FontStyle fontStyle, Color color, TextAnchor anchor)
        {
            var existing = parent.Find(name);
            var gameObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Text));

            if (existing == null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            var text = gameObject.GetOrAdd<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color, Image.Type imageType)
        {
            var existing = parent.Find(name);
            var gameObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));

            if (existing == null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            var image = gameObject.GetOrAdd<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = imageType;
            image.raycastTarget = false;
            return image;
        }
    }
}
