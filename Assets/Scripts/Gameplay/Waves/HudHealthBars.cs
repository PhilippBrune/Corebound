using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.Gameplay.Waves
{
    // Open Topics 3.3, step 4 of 4 (2026-10-01): the player and Core health bars, cut out of
    // WaveStatePresenter, which still owns the HUD labels and tells this what to show.
    //
    // Owner 2026-09-25: "make instead of % a HP bar with feedback on hit for player and core". The
    // bars hang under their existing labels, which keep the exact numbers - a bar answers "how bad
    // is it" at a glance, the number answers "how much exactly".
    public sealed class HudHealthBars
    {
        private static readonly Color PlayerColor = new Color(0.45f, 0.85f, 0.5f, 1f);
        private static readonly Color CoreColor = new Color(0.45f, 0.72f, 1f, 1f);
        private static readonly Color TrackColor = new Color(0.05f, 0.07f, 0.1f, 0.85f);
        private static readonly Color HitFlashColor = new Color(1f, 0.35f, 0.3f, 1f);
        private const float HitFlashSeconds = 0.35f;
        private const float Height = 8f;

        private readonly Bar _player;
        private readonly Bar _core;

        private HudHealthBars(Text playerLabel, Text coreLabel)
        {
            _player = new Bar(CreateFill(playerLabel, PlayerColor), PlayerColor);
            _core = new Bar(CreateFill(coreLabel, CoreColor), CoreColor);
        }

        // Null until both labels exist; the HUD canvas scene loads additively a frame late.
        public static HudHealthBars TryCreate(Text playerLabel, Text coreLabel)
        {
            return playerLabel != null && coreLabel != null ? new HudHealthBars(playerLabel, coreLabel) : null;
        }

        // False once the HUD the bars hang from has been rebuilt; the presenter then makes new ones.
        public bool IsAlive => _player.Fill != null;

        public void Show(float playerHealth, float coreHealth)
        {
            _player.Apply(playerHealth);
            _core.Apply(coreHealth);
        }

        // The bar hangs off the bottom edge of its label's rect, so it follows the HUD layout the
        // scene owns instead of introducing a second set of coordinates.
        private static Image CreateFill(Text label, Color fillColor)
        {
            var track = ProceduralUIFactory.CreateImage(label.name + "BarTrack", label.rectTransform, null, TrackColor, Image.Type.Simple);
            track.raycastTarget = false;
            var trackRect = track.rectTransform;
            trackRect.anchorMin = new Vector2(0.08f, 0f);
            trackRect.anchorMax = new Vector2(0.92f, 0f);
            trackRect.pivot = new Vector2(0.5f, 1f);
            trackRect.anchoredPosition = new Vector2(0f, 1f);
            trackRect.sizeDelta = new Vector2(0f, Height);

            var fill = ProceduralUIFactory.CreateImage("Fill", trackRect, null, fillColor, Image.Type.Simple);
            fill.raycastTarget = false;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            return fill;
        }

        private sealed class Bar
        {
            private readonly Color _baseColor;
            private float _shown = 1f;
            private float _flashTimer;

            public Bar(Image fill, Color baseColor)
            {
                Fill = fill;
                _baseColor = baseColor;
            }

            public Image Fill { get; }

            // The fill drains toward the real value rather than snapping, and flashes red on any
            // drop, so a hit is visible even when it costs a sliver. Unscaled time: the bar should
            // finish draining behind a pause menu opened on the hit that caused it.
            public void Apply(float target)
            {
                if (Fill == null)
                {
                    return;
                }

                target = Mathf.Clamp01(target);

                if (target < _shown - 0.0005f)
                {
                    _flashTimer = HitFlashSeconds;
                }

                _shown = Mathf.MoveTowards(_shown, target, Mathf.Max(0.35f, Mathf.Abs(_shown - target) * 6f) * Time.unscaledDeltaTime);
                Fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_shown), 1f);

                if (_flashTimer > 0f)
                {
                    _flashTimer = Mathf.Max(0f, _flashTimer - Time.unscaledDeltaTime);
                    Fill.color = Color.Lerp(_baseColor, HitFlashColor, _flashTimer / HitFlashSeconds);
                    return;
                }

                // Low health reads as a warning colour without needing a second element.
                Fill.color = _shown <= 0.3f ? Color.Lerp(HitFlashColor, _baseColor, _shown / 0.3f) : _baseColor;
            }
        }
    }
}
