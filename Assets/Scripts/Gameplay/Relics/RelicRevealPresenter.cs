using Corebound.Gameplay.Shared;
using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.Gameplay.Relics
{
    // Relics.md's "Visual" section calls for relics to use "eine eigenständige, hochwertige
    // visuelle Sprache" distinct from Cards, and its own changelog flagged the plain
    // NotificationPresenter toast as a known, deliberately deferred gap. This is that dedicated
    // reveal: a larger, centered, longer-held banner using the Legendary card frame (relics are
    // meant to read as more significant than even a Legendary card) and the card-reveal flash VFX,
    // instead of the small top-of-screen toast every other system shares.
    //
    // Deliberately non-blocking (no Time.timeScale pause, no input capture): a relic is granted
    // synchronously inside WaveManager.CompleteBossEncounter(), which in the same call also opens
    // the boss's Card Reward screen - a modal relic reveal would fight that flow for input context.
    // A passive, auto-dismissing banner sidesteps that entirely.
    public sealed class RelicRevealPresenter : MonoBehaviour
    {
        private const string LegendaryFramePath = "Assets/Art/Cards/Frames/card_frame_legendary.png";
        private const string RevealFlashPath = "Assets/Art/Combat/VFX/vfx_card_reveal_flash.png";
        private static readonly Rect FrameContentRect = new Rect(335f, 25f, 465f, 560f);
        private static readonly Vector4 FrameBorder = new Vector4(27f, 27f, 27f, 27f);
        private const float PanelWidth = 520f;
        private const float PanelHeight = 340f;
        private const float FadeInSeconds = 0.25f;
        private const float HoldSeconds = 3.2f;
        private const float FadeOutSeconds = 0.5f;

        private CanvasGroup _canvasGroup;
        private Image _flashImage;
        private Text _kindText;
        private Text _nameText;
        private Text _descriptionText;
        private Font _font;
        private float _timer;
        private bool _isShowing;

        private void Awake()
        {
            _font = UIFontResolver.Resolve();
            BuildUi();
        }

        private void OnEnable()
        {
            RelicEvents.RelicGranted += OnRelicGranted;
        }

        private void OnDisable()
        {
            RelicEvents.RelicGranted -= OnRelicGranted;
        }

        private void Update()
        {
            if (!_isShowing)
            {
                return;
            }

            _timer += Time.deltaTime;

            if (_timer < FadeInSeconds)
            {
                _canvasGroup.alpha = Mathf.Clamp01(_timer / FadeInSeconds);
            }
            else if (_timer < FadeInSeconds + HoldSeconds)
            {
                _canvasGroup.alpha = 1f;
            }
            else if (_timer < FadeInSeconds + HoldSeconds + FadeOutSeconds)
            {
                var fadeOutElapsed = _timer - FadeInSeconds - HoldSeconds;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(fadeOutElapsed / FadeOutSeconds);
            }
            else
            {
                _canvasGroup.alpha = 0f;
                _isShowing = false;
            }
        }

        private void OnRelicGranted(RelicGrantedEventArgs eventArgs)
        {
            if (eventArgs.Relic == null)
            {
                return;
            }

            _kindText.text = eventArgs.IsEvolution
                ? LocalizationService.Resolve("Relic Evolved", "Relikt entwickelt")
                : LocalizationService.Resolve("Relic Gained", "Relikt erhalten");
            _nameText.text = eventArgs.IsEvolution ? eventArgs.Relic.EvolvedDisplayName : eventArgs.Relic.DisplayName;
            _descriptionText.text = eventArgs.IsEvolution ? eventArgs.Relic.EvolvedDescription : eventArgs.Relic.Description;

            // Playtest 2026-09-28: "relic text not fitting inside the frame". Truncate kept the
            // text inside the box by cutting it off, which is not the same as fitting - a
            // build-defining effect the player cannot finish reading is the one thing this panel
            // exists to prevent. Both labels now step their size down until they genuinely fit.
            FitTextToRect(_nameText, 26, 15);
            FitTextToRect(_descriptionText, 14, 9);
            _timer = 0f;
            _isShowing = true;
            _canvasGroup.alpha = 0f;
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("RelicRevealCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above the HUD/wave banner but below modal overlays (Pause, Card Reward, Shop) that
            // actively block input - a relic reveal is a passive callout, not a blocker.
            canvas.sortingOrder = 450;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panelObject = new GameObject("RelicRevealPanel", typeof(RectTransform), typeof(CanvasGroup));
            panelObject.transform.SetParent(canvasObject.transform, false);
            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.72f);
            panelRect.anchorMax = new Vector2(0.5f, 0.72f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            _canvasGroup = panelObject.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            _flashImage = CreateImage("RevealFlash", panelRect, DebugSpriteFactory.LoadProjectSprite(RevealFlashPath), Color.white);
            StretchToParent(_flashImage.rectTransform, new Vector2(-0.25f, -0.2f), new Vector2(1.25f, 1.2f));
            _flashImage.raycastTarget = false;

            var frameSprite = DebugSpriteFactory.LoadProjectSpriteRect(LegendaryFramePath, FrameContentRect, 100f, FrameBorder);
            var frameImage = CreateImage("Frame", panelRect, frameSprite, Color.white);
            frameImage.type = Image.Type.Sliced;
            StretchToParent(frameImage.rectTransform, Vector2.zero, Vector2.one);
            frameImage.raycastTarget = false;

            _kindText = CreateText("KindText", panelRect, 18, FontStyle.BoldAndItalic, new Color(1f, 0.82f, 0.4f, 1f), TextAnchor.MiddleCenter);
            StretchToParent(_kindText.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 0.96f));

            _nameText = CreateText("NameText", panelRect, 26, FontStyle.Bold, new Color(0.16f, 0.14f, 0.11f, 0.98f), TextAnchor.MiddleCenter);
            StretchToParent(_nameText.rectTransform, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.82f));

            _descriptionText = CreateText("DescriptionText", panelRect, 14, FontStyle.Normal, new Color(0.2f, 0.18f, 0.14f, 0.95f), TextAnchor.UpperCenter, bestFit: true);
            StretchToParent(_descriptionText.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.64f));

            UIManager.RegisterPanel(panelRect, "relic_reveal", UIPanelLayer.HUD, false, false, _canvasGroup);
        }

        // Measured, not best-fit. resizeTextForBestFit rasterises through a dynamic glyph atlas,
        // which under this canvas's ScaleWithScreenSize stretch is what made text blurry elsewhere
        // in the project. Stepping the size down and asking preferredHeight each time gives the
        // same result deterministically, at a handful of layout queries on a panel that appears
        // once per relic.
        private static void FitTextToRect(Text text, int maxSize, int minSize)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
            {
                return;
            }

            var available = text.rectTransform.rect.height;

            if (available <= 1f)
            {
                return;
            }

            for (var size = maxSize; size >= minSize; size--)
            {
                text.fontSize = size;

                if (text.preferredHeight <= available)
                {
                    return;
                }
            }

            text.fontSize = minSize;
        }

        private Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private Text CreateText(string name, RectTransform parent, int fontSize, FontStyle fontStyle, Color color, TextAnchor anchor, bool bestFit = false)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.raycastTarget = false;

            if (bestFit)
            {
                // Relic descriptions vary a lot in length (Relics.md's whole point is build-defining
                // effects, which need real explanatory text) - a fixed font size on a fixed-height
                // box meant longer descriptions overflowed straight past the frame's bottom edge
                // (2026-09-15 playtest: "Overdriven Barrel" spilled out from under the parchment).
                // Deliberately NOT using resizeTextForBestFit here: this canvas uses CanvasScaler.
                // ScaleWithScreenSize, and best-fit's dynamic glyph-atlas rasterization combined with
                // that scaler's non-1:1 stretch is exactly what made scene-authored text blurry
                // elsewhere (see CardRewardPresenter.EnsureEditorText's own comment on the same
                // issue) - so this leans on a smaller fixed size plus Truncate as a hard safety net
                // instead of ever spilling past the frame.
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }
            else
            {
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }

            return text;
        }

        private static void StretchToParent(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
