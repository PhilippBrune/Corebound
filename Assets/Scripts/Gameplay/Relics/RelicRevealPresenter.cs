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
        private const float FadeInSeconds = 0.25f;
        private const float HoldSeconds = 3.2f;
        private const float FadeOutSeconds = 0.5f;

        private CanvasGroup _canvasGroup;
        private Image _flashImage;
        private Text _kindText;
        private Text _nameText;
        private Text _descriptionText;
        private float _timer;
        private bool _isShowing;

        // A relic granted while "Boss Defeated" is still on screen waits here until it fades.
        private RelicGrantedEventArgs? _pendingReveal;

        private void Awake()
        {
            TryBind();
        }

        private void OnEnable()
        {
            RelicEvents.RelicGranted += OnRelicGranted;
            AdditiveCanvasSceneLoader.CanvasSceneReady += OnCanvasSceneReady;
        }

        private void OnDisable()
        {
            RelicEvents.RelicGranted -= OnRelicGranted;
            AdditiveCanvasSceneLoader.CanvasSceneReady -= OnCanvasSceneReady;
        }

        private void Update()
        {
            if (_pendingReveal.HasValue && !Corebound.Gameplay.Bosses.BossEncounterState.IsDefeatNoticeShowing)
            {
                var pending = _pendingReveal.Value;
                _pendingReveal = null;
                Reveal(pending);
            }

            if (!_isShowing || _canvasGroup == null)
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
            // The boss's relic is granted in the same call that defeats the boss, a frame before
            // the Boss HUD raises its notice, so this also checks the encounter flag itself.
            if (Corebound.Gameplay.Bosses.BossEncounterState.IsDefeatNoticeShowing || Corebound.Gameplay.Bosses.BossEncounterState.IsActive)
            {
                _pendingReveal = eventArgs;
                return;
            }

            Reveal(eventArgs);
        }

        private void Reveal(RelicGrantedEventArgs eventArgs)
        {
            if (eventArgs.Relic == null || _canvasGroup == null || _nameText == null || _descriptionText == null || _kindText == null)
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

        // Open Topics 4 (b), 2026-09-30: the panel lives in OverlayCanvas.unity. The two images
        // hold no sprite in the scene and get theirs here from the art pipeline, like the HUD's
        // runtime-art Images - finding elements and repairing sprites, never layout.
        private void TryBind()
        {
            if (_canvasGroup != null)
            {
                return;
            }

            var root = AdditiveCanvasSceneLoader.FindRoot(AdditiveCanvasSceneLoader.OverlayCanvasSceneName, "RelicRevealCanvas");
            var panel = root != null ? root.transform.Find("RelicRevealPanel") as RectTransform : null;

            if (panel == null)
            {
                return;
            }

            _canvasGroup = panel.GetComponent<CanvasGroup>();
            _flashImage = FindComponent<Image>(panel, "RevealFlash");
            var frameImage = FindComponent<Image>(panel, "Frame");
            _kindText = FindComponent<Text>(panel, "KindText");
            _nameText = FindComponent<Text>(panel, "NameText");
            _descriptionText = FindComponent<Text>(panel, "DescriptionText");

            if (_flashImage != null && _flashImage.sprite == null)
            {
                _flashImage.sprite = DebugSpriteFactory.LoadProjectSprite(RevealFlashPath);
            }

            if (frameImage != null && frameImage.sprite == null)
            {
                frameImage.sprite = DebugSpriteFactory.LoadProjectSpriteRect(LegendaryFramePath, FrameContentRect, 100f, FrameBorder);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                UIManager.RegisterPanel(panel, "relic_reveal", UIPanelLayer.HUD, false, false, _canvasGroup);
            }
        }

        private static T FindComponent<T>(Transform parent, string childName) where T : Component
        {
            var child = parent.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private void OnCanvasSceneReady(string sceneName)
        {
            if (sceneName == AdditiveCanvasSceneLoader.OverlayCanvasSceneName)
            {
                TryBind();
            }
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
    }
}
