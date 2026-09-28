using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corebound.UI.MainMenu
{
    public sealed class MainMenuEntryView : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        // Tightened from 14/12/34 per playtest feedback ("still a little incorrect") - the
        // brackets read as floating, disconnected shapes at that spacing; pulling them closer to
        // the button edge and sizing them down reads as a single focus outline instead.
        private const float FocusHorizontalPadding = 6f;
        private const float FocusVerticalPadding = 6f;
        private const float FocusCornerSize = 26f;
        private const float LabelHorizontalPadding = 56f;
        private const float LabelVerticalPadding = 18f;
        private const int LabelFontSize = 28;

        // Matches MainMenuCanvasPresenter.FocusMarkerCornerRects' crop order (top-left, top-right,
        // bottom-left, bottom-right).
        private static readonly Vector2[] FocusCornerAnchors =
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
        };

        private Image _frameImage;
        private Image[] _focusImages;
        private Text _labelText;
        private int _entryIndex;
        private Action<int> _onPointerEnter;
        private Action<int> _onPointerClick;

        public void Initialize(RectTransform parent, Font font, Sprite frameSprite, Sprite[] focusCornerSprites, MainMenuSkinData skinData, int entryIndex, Action<int> onPointerEnter, Action<int> onPointerClick)
        {
            transform.SetParent(parent, false);
            _entryIndex = entryIndex;
            _onPointerEnter = onPointerEnter;
            _onPointerClick = onPointerClick;
            EnsureFrame(frameSprite, skinData);
            EnsureLabel(font, skinData);
            EnsureFocus(focusCornerSprites, skinData);
        }

        // What this row currently shows. MainMenuCanvasPresenter compares it against the model so a
        // profile swap that trades one entry for another - same count, different menu - still forces
        // a rebuild instead of leaving the previous profile's rows on screen.
        public MenuEntryData AppliedEntry { get; private set; }

        public void Apply(MenuEntryData entry, bool isSelected, bool isIntroVisible, MainMenuSkinData skinData)
        {
            AppliedEntry = entry;

            if (_labelText == null || _frameImage == null || _focusImages == null || skinData == null)
            {
                return;
            }

            var highlightAllowed = !isIntroVisible && isSelected;
            _labelText.text = entry != null ? entry.Label : string.Empty;
            _labelText.color = highlightAllowed ? skinData.SelectedLabelColor : skinData.IdleLabelColor;
            _frameImage.color = highlightAllowed
                ? skinData.SelectedFrameTint
                : (isIntroVisible ? skinData.IntroLockedFrameTint : skinData.IdleFrameTint);

            for (var index = 0; index < _focusImages.Length; index++)
            {
                var focusImage = _focusImages[index];
                focusImage.enabled = highlightAllowed && focusImage.sprite != null;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _onPointerEnter?.Invoke(_entryIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onPointerClick?.Invoke(_entryIndex);
        }

        private void EnsureFrame(Sprite frameSprite, MainMenuSkinData skinData)
        {
            _frameImage = gameObject.GetComponent<Image>();

            if (_frameImage == null)
            {
                _frameImage = gameObject.AddComponent<Image>();
            }

            // Raycast target enabled (was false) so mouse hover/click can reach this entry via
            // the Canvas's GraphicRaycaster + an EventSystem - see MainMenuCanvasPresenter.
            _frameImage.raycastTarget = true;
            _frameImage.sprite = frameSprite;
            _frameImage.type = frameSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            _frameImage.color = skinData != null ? skinData.IdleFrameTint : Color.white;
        }

        private void EnsureLabel(Font font, MainMenuSkinData skinData)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline));
            labelObject.transform.SetParent(transform, false);

            _labelText = labelObject.GetComponent<Text>();
            _labelText.font = font;
            _labelText.fontSize = LabelFontSize;
            _labelText.fontStyle = FontStyle.Bold;
            _labelText.alignment = TextAnchor.MiddleLeft;
            _labelText.color = skinData != null ? skinData.IdleLabelColor : Color.white;
            _labelText.raycastTarget = false;

            // Entries shrink to fit short screens (MainMenuCanvasPresenter.ApplyEntrySizing), so the
            // label shrinks with them instead of being clipped, and never vanishes on overflow.
            _labelText.resizeTextForBestFit = true;
            _labelText.resizeTextMinSize = 18;
            _labelText.resizeTextMaxSize = LabelFontSize;
            _labelText.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = labelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.03f, 0.04f, 0.06f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            var rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(LabelHorizontalPadding, LabelVerticalPadding);
            rectTransform.offsetMax = new Vector2(-LabelHorizontalPadding, -LabelVerticalPadding);
        }

        // focus_marker.png is four disconnected corner-bracket glyphs, not one continuous frame
        // (see MainMenuCanvasPresenter.CreateFocusMarkerSprites) - stretching it as a single
        // Image.Type.Sliced graphic across the whole entry (the old approach) smeared those four
        // brackets into one distorted bar. Placed as four small, unstretched corner images instead,
        // each sitting just outside its corner of the entry, matching how a camera-focus reticle
        // reads.
        private void EnsureFocus(Sprite[] focusCornerSprites, MainMenuSkinData skinData)
        {
            _focusImages = new Image[FocusCornerAnchors.Length];

            for (var index = 0; index < FocusCornerAnchors.Length; index++)
            {
                var anchor = FocusCornerAnchors[index];
                var existing = transform.Find("FocusCorner" + index);
                var cornerObject = existing != null ? existing.gameObject : new GameObject("FocusCorner" + index, typeof(RectTransform), typeof(Image));

                if (existing == null)
                {
                    cornerObject.transform.SetParent(transform, false);
                }

                var image = cornerObject.GetComponent<Image>() ?? cornerObject.AddComponent<Image>();
                image.raycastTarget = false;
                image.sprite = focusCornerSprites != null && index < focusCornerSprites.Length ? focusCornerSprites[index] : null;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = skinData != null ? skinData.FocusMarkerTint : Color.white;
                image.enabled = false;

                var rectTransform = cornerObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = anchor;
                rectTransform.anchorMax = anchor;
                rectTransform.pivot = anchor;
                rectTransform.anchoredPosition = new Vector2(
                    anchor.x > 0.5f ? FocusHorizontalPadding : -FocusHorizontalPadding,
                    anchor.y > 0.5f ? FocusVerticalPadding : -FocusVerticalPadding);
                rectTransform.sizeDelta = new Vector2(FocusCornerSize, FocusCornerSize);

                _focusImages[index] = image;
            }
        }
    }
}
