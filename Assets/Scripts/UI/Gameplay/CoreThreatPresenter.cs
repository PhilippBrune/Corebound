using System.Collections.Generic;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Enemies;
using Corebound.Gameplay.Waves;
using Corebound.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.Gameplay
{
    // The visible half of CORE_PROTECTION_FEEL_2026-10-07 (owner decisions P1-1 and P2-1): the
    // player defended a health bar in the corner, and learned the Core was in trouble when the bar
    // was already low. This draws the threat and the hit where the eye already is:
    // - A1 threat lines: a thin red line from every enemy going for the Core, for its first second
    //   of approach and again while it is close;
    // - A2 edge alert: a marker on the screen edge toward an off-screen Core with an enemy at it,
    //   red while the Core is being hit;
    // - A3 danger states: the Core's glow turns from blue to amber to red and flickers;
    // - B5 edge pulse: a Core-blue flash on the screen edge nearest the Core on every hit, stronger
    //   as its health drops.
    // Lines, alert and pulse follow the "Core Alerts" setting (Accessibility); the glow is the
    // Core's own look and always on. Added to the WaveManager's object at runtime; everything it
    // draws it builds itself - its overlay is its own canvas, never a child of a scene canvas.
    public sealed class CoreThreatPresenter : MonoBehaviour
    {
        private static readonly Color LineColor = new Color(1f, 0.3f, 0.24f, 1f);
        private static readonly Color CoreBlue = new Color(0.42f, 0.8f, 1f, 1f);
        private static readonly Color Amber = new Color(1f, 0.66f, 0.22f, 1f);
        private static readonly Color DangerRed = new Color(1f, 0.24f, 0.18f, 1f);

        private const float LineWidth = 0.05f;
        private const float LineApproachAlpha = 0.6f;
        private const float LineNearAlpha = 0.42f;
        private const float LineStartInset = 0.25f;
        private const float LineEndInset = 0.5f;
        private const int MaxLines = 24;
        private const int LineSortingOrder = -7;       // over the floor and the gates, under pickups and bodies
        private const int GlowSortingOrder = -3;       // just behind the Core (-2)
        private const float GlowCoreSizeMultiple = 2.4f;
        private const float PulseSeconds = 0.5f;
        private const float PulseMaxAlpha = 0.55f;
        private const float RecentHitSeconds = 0.75f;
        private const float EdgeThickness = 0.08f;     // of the screen, per side
        private const float AlertInset = 0.045f;
        private const float AlertPixels = 58f;
        private const int OverlaySortingOrder = 90;    // under the HUD canvas (100): the HUD stays readable on top

        private WaveManager _waveManager;
        private SettingsManager _settings;
        private float _nextSettingsLookup;
        private Camera _camera;

        private readonly List<EnemyRuntimeController> _enemies = new List<EnemyRuntimeController>();
        private readonly List<SpriteRenderer> _lines = new List<SpriteRenderer>();
        private readonly HashSet<EnemyRuntimeController> _shownLastFrame = new HashSet<EnemyRuntimeController>();
        private readonly HashSet<EnemyRuntimeController> _shownThisFrame = new HashSet<EnemyRuntimeController>();
        private Transform _lineRoot;

        private SpriteRenderer _glow;
        private Color _glowColor = CoreBlue;
        private float _glowFlash;

        private GameObject _overlayObject;
        private Image[] _edges;
        private Image _alert;
        private float _pulse;
        private float _pulseStrength;
        private Vector4 _edgeWeights;
        private float _lastHitAt = -10f;

        private static Sprite _pixelSprite;
        private static Sprite _glowSprite;
        private static Sprite _horizontalFadeSprite;
        private static Sprite _verticalFadeSprite;
        private static Sprite _arrowSprite;

#if UNITY_EDITOR
        // Autopilot "core" probe.
        public static int DebugLineActivations;
        public static int DebugMaxLinesAtOnce;
        public static int DebugEdgePulses;
        public static float DebugAlertSeconds;
        public static float DebugCoreOffscreenSeconds;
        public static int DebugDangerState;
        public static int DebugMaxDangerState;
        public static int LiveLineCount;

        // The arena camera shows every stage whole, so the Core is never off-screen in play; the
        // probe sets this to see the edge alert drawn at all (the Core treated as just past the
        // screen edge in its real direction).
        public static bool DebugForceOffscreen;
#endif

        public void Bind(WaveManager waveManager)
        {
            _waveManager = waveManager;

            foreach (var enemy in FindObjectsByType<EnemyRuntimeController>(FindObjectsSortMode.None))
            {
                Track(enemy);
            }
        }

        private void OnEnable()
        {
            EnemyEvents.EnemySpawned += OnEnemySpawned;
            CoreController.CoreHit += OnCoreHit;
        }

        private void OnDisable()
        {
            EnemyEvents.EnemySpawned -= OnEnemySpawned;
            CoreController.CoreHit -= OnCoreHit;
            SetLinesVisible(0);
            HideOverlay();
        }

        private void OnDestroy()
        {
            if (_lineRoot != null)
            {
                Destroy(_lineRoot.gameObject);
            }

            if (_overlayObject != null)
            {
                Destroy(_overlayObject);
            }

            if (_glow != null)
            {
                Destroy(_glow.gameObject);
            }
        }

        private void OnEnemySpawned(EnemyLifecycleEventArgs eventArgs)
        {
            Track(eventArgs.EnemyObject != null ? eventArgs.EnemyObject.GetComponent<EnemyRuntimeController>() : null);
        }

        private void Track(EnemyRuntimeController enemy)
        {
            if (enemy != null && !_enemies.Contains(enemy))
            {
                _enemies.Add(enemy);
            }
        }

        private void OnCoreHit(CoreHitInfo hit)
        {
            _lastHitAt = Time.time;
            _glowFlash = 1f;

            if (!AlertsEnabled || ResolveCamera() == null)
            {
                return;
            }

            // Toward the Core: an off-centre Core lights the edges on its side, a centred one all four
            // a little. Health decides how hard.
            var viewport = (Vector2)_camera.WorldToViewportPoint(hit.Position) - new Vector2(0.5f, 0.5f);
            const float Base = 0.3f;
            _edgeWeights = new Vector4(
                Base + Mathf.Clamp01(-viewport.x * 2.5f),
                Base + Mathf.Clamp01(viewport.x * 2.5f),
                Base + Mathf.Clamp01(-viewport.y * 2.5f),
                Base + Mathf.Clamp01(viewport.y * 2.5f));
            var max = Mathf.Max(Mathf.Max(_edgeWeights.x, _edgeWeights.y), Mathf.Max(_edgeWeights.z, _edgeWeights.w));
            _edgeWeights /= Mathf.Max(0.0001f, max);
            _pulseStrength = Mathf.Lerp(0.45f, 1f, 1f - Mathf.Clamp01(hit.HealthNormalized)) * EffectsScale;
            _pulse = 1f;
#if UNITY_EDITOR
            DebugEdgePulses++;
#endif
        }

        private void LateUpdate()
        {
            var core = CoreController.Active;

            if (core == null || core.HitReceiver == null)
            {
                SetLinesVisible(0);
                HideOverlay();
                return;
            }

            var state = _waveManager != null ? _waveManager.State : WaveState.Combat;
            var inFight = state == WaveState.Combat || state == WaveState.BossCombat;
            var alerts = inFight && AlertsEnabled;

            UpdateGlow(core);
            UpdateLines(core, alerts);
            UpdateOverlay(core, alerts && Time.timeScale > 0f);
        }

        // ---- A3: the Core's own glow ------------------------------------------------------------

        private void UpdateGlow(CoreController core)
        {
            var receiver = core.HitReceiver;

            if (_glow == null)
            {
                var glowObject = new GameObject("CoreDangerGlow");
                glowObject.transform.SetParent(core.transform, false);
                _glow = glowObject.AddComponent<SpriteRenderer>();
                _glow.sprite = GlowSprite;
                _glow.sortingOrder = GlowSortingOrder;
            }
            else if (_glow.transform.parent != core.transform)
            {
                _glow.transform.SetParent(core.transform, false);
            }

            if (!receiver.IsAlive)
            {
                _glow.enabled = false;
                return;
            }

            _glow.enabled = true;
            var tuning = CoreController.Tuning;
            var health = receiver.CurrentHealthNormalized;
            var time = Time.time;
            Color target;
            float alpha;
            int dangerState;

            if (health <= tuning.DangerRedThreshold)
            {
                target = DangerRed;
                alpha = 0.55f * (0.45f + 0.55f * Mathf.PerlinNoise(time * 9f, 0.3f));
                dangerState = 2;
            }
            else if (health <= tuning.DangerAmberThreshold)
            {
                target = Amber;
                alpha = 0.4f * (0.7f + 0.3f * Mathf.PerlinNoise(time * 3f, 0.7f));
                dangerState = 1;
            }
            else
            {
                target = CoreBlue;
                alpha = 0.22f + 0.04f * Mathf.Sin(time * 1.6f);
                dangerState = 0;
            }

#if UNITY_EDITOR
            DebugDangerState = dangerState;
            DebugMaxDangerState = Mathf.Max(DebugMaxDangerState, dangerState);
#endif
            _glowColor = Color.Lerp(_glowColor, target, Mathf.Clamp01(Time.deltaTime * 3f));
            _glowFlash = Mathf.MoveTowards(_glowFlash, 0f, Time.deltaTime * 3.5f);
            var color = Color.Lerp(_glowColor, Color.white, _glowFlash * 0.35f);
            color.a = Mathf.Clamp01(alpha + _glowFlash * 0.35f);
            _glow.color = color;

            // Sized to what the Core draws, whatever scale the stage gave its transform.
            var coreRenderer = core.GetComponent<SpriteRenderer>();
            var drawn = coreRenderer != null && coreRenderer.sprite != null ? Mathf.Max(coreRenderer.bounds.size.x, coreRenderer.bounds.size.y) : 1f;
            var world = Mathf.Max(0.5f, drawn) * GlowCoreSizeMultiple;
            var parentScale = core.transform.lossyScale;
            _glow.transform.localPosition = Vector3.zero;
            _glow.transform.localScale = new Vector3(
                world / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                world / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
                1f);
        }

        // ---- A1: threat lines ---------------------------------------------------------------------

        private void UpdateLines(CoreController core, bool show)
        {
            var used = 0;
            _shownThisFrame.Clear();

            if (show)
            {
                var tuning = CoreController.Tuning;
                var corePosition = (Vector2)core.transform.position;
                var time = Time.time;

                for (var index = _enemies.Count - 1; index >= 0; index--)
                {
                    var enemy = _enemies[index];

                    // Pooled enemies are disabled on death and announced again on their next spawn.
                    if (enemy == null || !enemy.isActiveAndEnabled)
                    {
                        _enemies.RemoveAt(index);
                        continue;
                    }

                    if (!enemy.IsHeadingForCore || used >= MaxLines)
                    {
                        continue;
                    }

                    var position = (Vector2)enemy.transform.position;
                    var since = time - enemy.CoreApproachStartedAt;
                    var distance = Vector2.Distance(position, corePosition);
                    var alpha = 0f;

                    if (tuning.ThreatLineSeconds > 0f && since <= tuning.ThreatLineSeconds)
                    {
                        alpha = LineApproachAlpha * (1f - 0.6f * since / tuning.ThreatLineSeconds);
                    }

                    if (distance <= tuning.ThreatLineNearDistance)
                    {
                        alpha = Mathf.Max(alpha, LineNearAlpha * (0.75f + 0.25f * Mathf.Sin(time * 8f + index)));
                    }

                    if (alpha <= 0.01f || distance <= LineStartInset + LineEndInset)
                    {
                        continue;
                    }

                    PlaceLine(GetLine(used++), position, corePosition, alpha);
                    _shownThisFrame.Add(enemy);

#if UNITY_EDITOR
                    if (!_shownLastFrame.Contains(enemy))
                    {
                        DebugLineActivations++;
                    }
#endif
                }
            }

            SetLinesVisible(used);
            _shownLastFrame.Clear();
            _shownLastFrame.UnionWith(_shownThisFrame);
#if UNITY_EDITOR
            LiveLineCount = used;
            DebugMaxLinesAtOnce = Mathf.Max(DebugMaxLinesAtOnce, used);
#endif
        }

        private SpriteRenderer GetLine(int index)
        {
            if (_lineRoot == null)
            {
                _lineRoot = new GameObject("CoreThreatLines").transform;
            }

            while (_lines.Count <= index)
            {
                var lineObject = new GameObject("ThreatLine");
                lineObject.transform.SetParent(_lineRoot, false);
                var renderer = lineObject.AddComponent<SpriteRenderer>();
                renderer.sprite = PixelSprite;
                renderer.sortingOrder = LineSortingOrder;
                _lines.Add(renderer);
            }

            return _lines[index];
        }

        private static void PlaceLine(SpriteRenderer line, Vector2 from, Vector2 to, float alpha)
        {
            var direction = (to - from).normalized;
            var start = from + direction * LineStartInset;
            var end = to - direction * LineEndInset;
            var length = Vector2.Distance(start, end);
            line.transform.position = (Vector3)((start + end) * 0.5f);
            line.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            line.transform.localScale = new Vector3(length, LineWidth, 1f);
            var color = LineColor;
            color.a = alpha;
            line.color = color;

            if (!line.gameObject.activeSelf)
            {
                line.gameObject.SetActive(true);
            }
        }

        private void SetLinesVisible(int count)
        {
            for (var index = count; index < _lines.Count; index++)
            {
                if (_lines[index] != null && _lines[index].gameObject.activeSelf)
                {
                    _lines[index].gameObject.SetActive(false);
                }
            }
        }

        // ---- A2 edge alert and B5 edge pulse -------------------------------------------------------

        private void UpdateOverlay(CoreController core, bool show)
        {
            if (!show || ResolveCamera() == null)
            {
                HideOverlay();
                _pulse = 0f;
                return;
            }

            EnsureOverlay();
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.deltaTime / PulseSeconds);
            var eased = _pulse * _pulse;
            SetEdge(0, _edgeWeights.x * eased);
            SetEdge(1, _edgeWeights.y * eased);
            SetEdge(2, _edgeWeights.z * eased);
            SetEdge(3, _edgeWeights.w * eased);

            var corePosition = (Vector2)core.transform.position;
            var viewport = (Vector2)_camera.WorldToViewportPoint(corePosition);
#if UNITY_EDITOR
            if (DebugForceOffscreen)
            {
                var outward = viewport - new Vector2(0.5f, 0.5f);
                viewport = new Vector2(0.5f, 0.5f) + (outward.sqrMagnitude > 0.0001f ? outward.normalized : Vector2.right) * 0.9f;
            }
#endif
            var offScreen = viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f;

            if (!offScreen || !core.HitReceiver.IsAlive)
            {
                _alert.gameObject.SetActive(false);
                return;
            }

#if UNITY_EDITOR
            DebugCoreOffscreenSeconds += Time.deltaTime;
#endif
            var recentlyHit = Time.time - _lastHitAt <= RecentHitSeconds;

            if (!recentlyHit && !IsEnemyNear(corePosition, CoreController.Tuning.EdgeAlertRadius))
            {
                _alert.gameObject.SetActive(false);
                return;
            }

#if UNITY_EDITOR
            DebugAlertSeconds += Time.deltaTime;
#endif
            // Where the line from the screen centre to the Core leaves the screen, kept inside it.
            var fromCentre = viewport - new Vector2(0.5f, 0.5f);
            var limit = 0.5f - AlertInset;
            var scale = Mathf.Min(
                Mathf.Abs(fromCentre.x) > 0.0001f ? limit / Mathf.Abs(fromCentre.x) : float.MaxValue,
                Mathf.Abs(fromCentre.y) > 0.0001f ? limit / Mathf.Abs(fromCentre.y) : float.MaxValue);
            var anchor = new Vector2(0.5f, 0.5f) + fromCentre * scale;
            var rect = _alert.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(fromCentre.y, fromCentre.x) * Mathf.Rad2Deg - 90f);
            var beat = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);
            rect.localScale = Vector3.one * (1f + 0.15f * beat);
            var color = recentlyHit ? DangerRed : CoreBlue;
            color.a = 0.65f + 0.35f * beat;
            _alert.color = color;
            _alert.gameObject.SetActive(true);
        }

        private bool IsEnemyNear(Vector2 corePosition, float radius)
        {
            var radiusSquared = radius * radius;

            for (var index = 0; index < _enemies.Count; index++)
            {
                var enemy = _enemies[index];

                if (enemy != null && enemy.isActiveAndEnabled && enemy.IsAlive
                    && ((Vector2)enemy.transform.position - corePosition).sqrMagnitude <= radiusSquared)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetEdge(int index, float weight)
        {
            var color = CoreBlue;
            color.a = Mathf.Clamp01(weight * _pulseStrength) * PulseMaxAlpha;
            _edges[index].color = color;
            _edges[index].enabled = color.a > 0.004f;
        }

        private void HideOverlay()
        {
            if (_overlayObject == null)
            {
                return;
            }

            for (var index = 0; index < _edges.Length; index++)
            {
                _edges[index].enabled = false;
            }

            _alert.gameObject.SetActive(false);
        }

        private void EnsureOverlay()
        {
            if (_overlayObject != null)
            {
                return;
            }

            // A root object of its own: a Canvas added to a gameplay object would rescale its
            // world-space children (memory: the CombatRuntime canvas incident).
            _overlayObject = new GameObject("CoreThreatOverlay", typeof(RectTransform));
            var canvas = _overlayObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;
            var root = (RectTransform)_overlayObject.transform;

            _edges = new[]
            {
                CreateEdge(root, "EdgeLeft", HorizontalFadeSprite, new Vector2(0f, 0f), new Vector2(EdgeThickness, 1f), Vector3.one),
                CreateEdge(root, "EdgeRight", HorizontalFadeSprite, new Vector2(1f - EdgeThickness, 0f), new Vector2(1f, 1f), new Vector3(-1f, 1f, 1f)),
                CreateEdge(root, "EdgeBottom", VerticalFadeSprite, new Vector2(0f, 0f), new Vector2(1f, EdgeThickness * 1.6f), Vector3.one),
                CreateEdge(root, "EdgeTop", VerticalFadeSprite, new Vector2(0f, 1f - EdgeThickness * 1.6f), new Vector2(1f, 1f), new Vector3(1f, -1f, 1f)),
            };

            var alertObject = new GameObject("CoreEdgeAlert", typeof(RectTransform));
            alertObject.transform.SetParent(root, false);
            _alert = alertObject.AddComponent<Image>();
            _alert.sprite = ArrowSprite;
            _alert.raycastTarget = false;
            _alert.rectTransform.sizeDelta = new Vector2(AlertPixels, AlertPixels);
            alertObject.SetActive(false);
        }

        private static Image CreateEdge(RectTransform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector3 scale)
        {
            var edgeObject = new GameObject(name, typeof(RectTransform));
            edgeObject.transform.SetParent(parent, false);
            var image = edgeObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.enabled = false;
            var rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = scale;
            return image;
        }

        // ---- settings, camera, sprites ---------------------------------------------------------------

        private bool AlertsEnabled
        {
            get
            {
                var settings = ResolveSettings();
                return settings == null || settings.RuntimeSettings == null || settings.RuntimeSettings.ShowCoreAlerts;
            }
        }

        private float EffectsScale
        {
            get
            {
                var settings = ResolveSettings();
                var intensity = settings != null && settings.RuntimeSettings != null ? settings.RuntimeSettings.EffectsIntensity : 1f;
                return Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(intensity));
            }
        }

        private SettingsManager ResolveSettings()
        {
            if (_settings == null && Time.unscaledTime >= _nextSettingsLookup)
            {
                _nextSettingsLookup = Time.unscaledTime + 2f;
                _settings = FindFirstObjectByType<SettingsManager>();
            }

            return _settings;
        }

        private Camera ResolveCamera()
        {
            if (_camera == null || !_camera.isActiveAndEnabled)
            {
                _camera = Camera.main;
            }

            return _camera;
        }

        private static Sprite PixelSprite => _pixelSprite != null ? _pixelSprite : (_pixelSprite = BuildSprite(4, 4, (x, y) => 1f, 4f));

        // A soft disc: full in the middle, gone at the rim.
        private static Sprite GlowSprite => _glowSprite != null ? _glowSprite : (_glowSprite = BuildSprite(128, 128, (x, y) =>
        {
            var dx = (x + 0.5f) / 64f - 1f;
            var dy = (y + 0.5f) / 64f - 1f;
            var falloff = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            return falloff * falloff;
        }, 128f));

        private static Sprite HorizontalFadeSprite => _horizontalFadeSprite != null ? _horizontalFadeSprite
            : (_horizontalFadeSprite = BuildSprite(64, 4, (x, y) => Mathf.Pow(1f - x / 63f, 2f), 64f));

        private static Sprite VerticalFadeSprite => _verticalFadeSprite != null ? _verticalFadeSprite
            : (_verticalFadeSprite = BuildSprite(4, 64, (x, y) => Mathf.Pow(1f - y / 63f, 2f), 64f));

        // An arrow pointing up, anti-aliased at its edges.
        private static Sprite ArrowSprite => _arrowSprite != null ? _arrowSprite : (_arrowSprite = BuildSprite(64, 64, (x, y) =>
        {
            var u = (x + 0.5f) / 64f - 0.5f;
            var v = (y + 0.5f) / 64f;
            const float Top = 0.94f;
            const float Base = 0.12f;

            if (v < Base || v > Top)
            {
                return 0f;
            }

            var halfWidth = 0.44f * (Top - v) / (Top - Base);
            return Mathf.Clamp01((halfWidth - Mathf.Abs(u)) * 64f);
        }, 64f));

        private static Sprite BuildSprite(int width, int height, System.Func<int, int, float> alpha, float pixelsPerUnit)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[width * height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
