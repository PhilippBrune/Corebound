using Corebound.Gameplay.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.Gameplay.Tutorial
{
    // Open Topics 2.1 (2026-10-01): a tutorial beat gets its own panel - TutorialBeatCanvas in the
    // authored OverlayCanvas scene, drawn with the tutorial prompt panel (Asset 06) and the Confirm
    // tile of the tutorial glyph strip (Asset 28) - instead of borrowing the pause overlay, so a
    // beat no longer looks like the game was paused by the player.
    //
    // Only where the beat is drawn moved. WaveStatePresenter still opens it as its Tutorial overlay
    // mode: it freezes time, takes Confirm and calls the tutorial back, exactly as before. Layout
    // is the scene's; this finds the elements and gives the images their sprites, like every other
    // runtime-art Image in the canvas scenes.
    public static class TutorialBeatPresenter
    {
        private const string CanvasName = "TutorialBeatCanvas";
        private const string PanelPath = "Assets/Art/UI/Tutorial/tutorial_prompt_panel.png";
        private const string GlyphSheetName = "SpriteSheet_TutorialGlyphs";

        // The panel's opaque area inside its 1088 x 606 canvas, and a border that keeps the four
        // lit corners whole at any size.
        private static readonly Rect PanelContentRect = new Rect(121f, 72f, 846f, 449f);
        private static readonly Vector4 PanelBorder = new Vector4(120f, 110f, 120f, 110f);

        private static GameObject _panel;
        private static Text _title;
        private static Text _body;

        public static bool TryShow(string title, string body)
        {
            if (!Resolve())
            {
                return false;
            }

            _title.text = title;
            _body.text = body;
            _panel.SetActive(true);
            return true;
        }

        public static void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private static bool Resolve()
        {
            if (_panel != null && _title != null && _body != null)
            {
                return true;
            }

            var root = AdditiveCanvasSceneLoader.FindRoot(AdditiveCanvasSceneLoader.OverlayCanvasSceneName, CanvasName);
            var panel = root != null ? root.transform.Find("TutorialBeatPanel") : null;

            if (panel == null)
            {
                return false;
            }

            _panel = panel.gameObject;
            _title = panel.Find("BeatTitle") != null ? panel.Find("BeatTitle").GetComponent<Text>() : null;
            _body = panel.Find("BeatBody") != null ? panel.Find("BeatBody").GetComponent<Text>() : null;

            var frame = panel.GetComponent<Image>();

            if (frame != null && frame.sprite == null)
            {
                frame.sprite = DebugSpriteFactory.LoadProjectSpriteRect(PanelPath, PanelContentRect, 100f, PanelBorder);
                frame.type = frame.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                frame.color = frame.sprite != null ? Color.white : frame.color;
            }

            var glyph = panel.Find("ConfirmGlyph") != null ? panel.Find("ConfirmGlyph").GetComponent<Image>() : null;
            var sheet = SpriteSheetData.Find(GlyphSheetName);

            if (glyph != null && glyph.sprite == null && sheet != null)
            {
                glyph.sprite = sheet.LoadCell("confirm");
                glyph.preserveAspect = true;
                glyph.enabled = glyph.sprite != null;
            }

            return _title != null && _body != null;
        }
    }
}
