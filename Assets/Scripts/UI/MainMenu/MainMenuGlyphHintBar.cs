using Corebound.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.MainMenu
{
    public sealed class MainMenuGlyphHintBar : MonoBehaviour
    {
        private const float HintWidth = 200f;
        private const float HintHeight = 68f;
        private const int GlyphFontSize = 22;
        private const int ActionFontSize = 16;

        private readonly Image[] _frameImages = new Image[3];
        private readonly Text[] _glyphTexts = new Text[3];
        private readonly Text[] _actionTexts = new Text[3];

        private HorizontalLayoutGroup _layoutGroup;

        public void Initialize(Font font, Sprite frameSprite, MainMenuSkinData skinData)
        {
            EnsureRoot();

            for (var index = 0; index < _frameImages.Length; index++)
            {
                CreateHint(index, font, frameSprite, skinData);
            }
        }

        public void Refresh(bool isUsingGamepad, bool isIntroVisible, ControllerGlyphData glyphData, MainMenuSkinData skinData)
        {
            var canRender = glyphData != null && skinData != null;
            gameObject.SetActive(canRender);

            if (!canRender)
            {
                return;
            }

            ApplyHint(0, true, glyphData.MoveGlyphKey, glyphData.GetMoveLabel(isUsingGamepad), skinData, !isIntroVisible);
            ApplyHint(1, true, glyphData.ConfirmGlyphKey, glyphData.GetConfirmLabel(isUsingGamepad), skinData, true);
            ApplyHint(2, true, glyphData.CancelGlyphKey, glyphData.GetCancelLabel(isUsingGamepad), skinData, true);
        }

        private void EnsureRoot()
        {
            var rectTransform = gameObject.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            _layoutGroup = gameObject.GetComponent<HorizontalLayoutGroup>();

            if (_layoutGroup == null)
            {
                _layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            _layoutGroup.spacing = 12f;
            _layoutGroup.childAlignment = TextAnchor.LowerRight;
            _layoutGroup.childControlWidth = false;
            _layoutGroup.childControlHeight = false;
            _layoutGroup.childForceExpandWidth = false;
            _layoutGroup.childForceExpandHeight = false;
        }

        private void CreateHint(int index, Font font, Sprite frameSprite, MainMenuSkinData skinData)
        {
            var hintObject = new GameObject("Hint" + index, typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            hintObject.transform.SetParent(transform, false);

            var layoutElement = hintObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = HintWidth;
            layoutElement.preferredHeight = HintHeight;
            layoutElement.minWidth = HintWidth;
            layoutElement.minHeight = HintHeight;

            var frameImage = hintObject.GetComponent<Image>();
            frameImage.sprite = frameSprite;
            frameImage.type = frameSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            frameImage.raycastTarget = false;
            frameImage.color = skinData != null ? skinData.GlyphHintFrameTint : Color.white;
            _frameImages[index] = frameImage;

            var glyphText = CreateText("Glyph", hintObject.transform as RectTransform, font, GlyphFontSize, FontStyle.Bold);
            glyphText.alignment = TextAnchor.MiddleRight;
            glyphText.rectTransform.anchorMin = new Vector2(0f, 0f);
            glyphText.rectTransform.anchorMax = new Vector2(1f, 1f);
            glyphText.rectTransform.offsetMin = new Vector2(16f, 28f);
            glyphText.rectTransform.offsetMax = new Vector2(-16f, -10f);
            _glyphTexts[index] = glyphText;

            var actionText = CreateText("Action", hintObject.transform as RectTransform, font, ActionFontSize, FontStyle.Normal);
            actionText.alignment = TextAnchor.MiddleLeft;
            actionText.rectTransform.anchorMin = new Vector2(0f, 0f);
            actionText.rectTransform.anchorMax = new Vector2(1f, 1f);
            actionText.rectTransform.offsetMin = new Vector2(16f, 10f);
            actionText.rectTransform.offsetMax = new Vector2(-16f, -34f);
            _actionTexts[index] = actionText;
        }

        private void ApplyHint(int index, bool isVisible, string actionKey, string deviceLabel, MainMenuSkinData skinData, bool isEnabled)
        {
            var frameImage = _frameImages[index];
            var glyphText = _glyphTexts[index];
            var actionText = _actionTexts[index];

            if (frameImage == null || glyphText == null || actionText == null)
            {
                return;
            }

            frameImage.gameObject.SetActive(isVisible);

            if (!isVisible)
            {
                return;
            }

            frameImage.color = isEnabled ? skinData.GlyphHintFrameTint : skinData.IntroLockedFrameTint;
            glyphText.color = isEnabled ? skinData.GlyphHintKeyColor : skinData.IdleLabelColor;
            actionText.color = isEnabled ? skinData.GlyphHintActionColor : skinData.IdleLabelColor;
            glyphText.text = deviceLabel;
            actionText.text = actionKey != null ? actionKey.ToUpperInvariant() : string.Empty;
        }

        private static Text CreateText(string name, RectTransform parent, Font font, int fontSize, FontStyle fontStyle)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}