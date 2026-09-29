using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI
{
    // Wheel scrolling and "keep the selected row visible" for the hand-hit-tested lists
    // (Progression, Settings). No EventSystem raycaster reaches those canvases, so the ScrollRect
    // never scrolls itself; these lists drive it directly.
    //
    // Playtest 2026-09-29: "scrolling in the quest section is not working correctly". Two faults, in
    // both lists. The wheel handler moved a fixed step for *any* non-zero scroll value, so a touchpad
    // or a smooth-scrolling mouse - which report small values every frame rather than one value per
    // notch - sent the list flying. And keeping the selection in view measured a row from its
    // centre, not its top, so the selected row sat half hidden at the edges.
    public static class UIScrollUtility
    {
        public const float PixelsPerNotch = 96f;

        // Windows reports 120 per wheel notch; some drivers and the Input System's uniform scroll
        // setting report 1. Either way one notch is one PixelsPerNotch, and fractions scroll a
        // fraction.
        public static float WheelToPixels(float rawScroll)
        {
            if (Mathf.Abs(rawScroll) < 0.001f)
            {
                return 0f;
            }

            var notches = Mathf.Abs(rawScroll) >= 20f ? rawScroll / 120f : rawScroll;
            return notches * PixelsPerNotch;
        }

        // Positive pixels scroll the content up, i.e. towards the top of the list.
        public static void ScrollBy(ScrollRect scrollRect, float pixels)
        {
            if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null || Mathf.Abs(pixels) < 0.01f)
            {
                return;
            }

            var content = scrollRect.content;
            var maxScroll = Mathf.Max(0f, content.rect.height - scrollRect.viewport.rect.height);
            var position = content.anchoredPosition;
            position.y = Mathf.Clamp(position.y - pixels, 0f, maxScroll);
            content.anchoredPosition = position;
        }

        public static void ScrollToShow(ScrollRect scrollRect, RectTransform rowRect)
        {
            if (scrollRect == null || scrollRect.viewport == null || scrollRect.content == null || rowRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            var content = scrollRect.content;
            var viewportHeight = scrollRect.viewport.rect.height;
            var rowTop = -rowRect.anchoredPosition.y - rowRect.rect.height * (1f - rowRect.pivot.y);
            var rowBottom = rowTop + rowRect.rect.height;
            var currentOffset = content.anchoredPosition.y;

            var targetOffset = Mathf.Clamp(currentOffset, rowBottom - viewportHeight, rowTop);
            var maxScrollable = Mathf.Max(0f, content.rect.height - viewportHeight);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Clamp(targetOffset, 0f, maxScrollable));
        }
    }
}
