using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Waves;
using Corebound.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.Gameplay
{
    // Replaces the OS mouse pointer with a reticle that reports weapon state (OPEN_TOPICS 3.1,
    // owner chose option 3).
    //
    // Second design, playtest 2026-09-29: "the mouse changes look strange - implement another
    // way / visual." The first reticle was four loose dashes in world space that spread apart
    // while the weapon cooled. At a glance they read as a glitch rather than a pointer, and their
    // size followed the camera, not the screen.
    //
    // This one is the shape players already read as "aim here": a dot inside a ring.
    //  - The dot is the exact aim point and never moves or changes size.
    //  - The ring's bright arc is the magazine. It drains as you fire and refills, grey, during a
    //    reload - so "am I about to run dry" is answered where the player is already looking.
    //  - Over a live enemy the whole reticle turns red and the ring tightens slightly.
    // It is drawn on its own screen-space canvas, so it is crisp and the same size at any zoom.
    //
    // While a menu is open (pause, reward, shop, loadout) the reticle hides and the Windows pointer
    // comes back. The world-space version stayed hidden under those overlays with the OS pointer
    // also hidden, which left a mouse player clicking blind.
    //
    // Third step, playtest 2026-09-29 ("the aim jumps for some frames"), owner chose OPEN_TOPICS 3.1
    // option 1: with a mouse the reticle is a *hardware* cursor. A canvas image is placed once per
    // rendered frame, so any frame hitch shows as the reticle lagging and then jumping; Windows draws
    // a hardware cursor itself at the display's refresh rate, whatever the game's frame time. The
    // weapon state survives: the cursor image is swapped between pre-drawn variants (magazine arc in
    // MagazineSteps steps, ready / on target / reloading), and only when the variant changes.
    // With a gamepad there is no pointer to replace, so the canvas reticle orbits the player as
    // before. The two root causes of the jumps are fixed at the source as well: stick drift no longer
    // flips the input device (CoreboundInputReader), and save writes no longer block the frame on
    // every kill (LocalProfileSaveManager).
    public sealed class AimReticlePresenter : MonoBehaviour
    {
        private const float OrbitRadius = 1.35f;
        private const int CanvasSortingOrder = 400;
        private const int RingTextureSize = 128;
        private const float RingSize = 46f;
        private const float DotSize = 8f;
        private const float TargetRingScale = 0.86f;

        private static readonly Color ReadyColor = new Color(1f, 0.8f, 0.38f, 1f);
        private static readonly Color ReloadColor = new Color(0.72f, 0.78f, 0.88f, 0.9f);
        private static readonly Color TargetColor = new Color(1f, 0.36f, 0.3f, 1f);
        private static readonly Color BackingColor = new Color(0f, 0f, 0f, 0.5f);

        [SerializeField] private PlayerCombatController _playerCombatController;
        [SerializeField] private UnityEngine.Object _inputReaderSource;

        // Matches the enemy layer test the soft-aim scan already uses: anything with a
        // CombatHitReceiver that is alive and is not the player.
        [SerializeField] private float _targetProbeRadius = 0.55f;

        private readonly Collider2D[] _probeBuffer = new Collider2D[8];

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _root;
        private Image _ringBacking;
        private Image _ammoArc;
        private Image _dot;
        private Image _dotBacking;
        private Texture2D _ringTexture;
        private Texture2D _dotTexture;
        private Camera _camera;
        private bool _cursorHidden;

        // Hardware cursor variants, built on first use. 32 px is the size every Windows cursor path
        // accepts without rescaling.
        private const int CursorSize = 32;
        private const int MagazineSteps = 12;
        private readonly System.Collections.Generic.Dictionary<int, Texture2D> _cursorTextures =
            new System.Collections.Generic.Dictionary<int, Texture2D>();
        private int _appliedCursorKey = -1;

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private void Awake()
        {
            if (_playerCombatController == null)
            {
                _playerCombatController = GetComponent<PlayerCombatController>() ?? FindFirstObjectByType<PlayerCombatController>();
            }

            if (_inputReaderSource == null)
            {
                _inputReaderSource = FindFirstObjectByType<CoreboundInputReader>();
            }

            BuildReticle();
        }

        private void OnDisable()
        {
            ApplyDefaultCursor();
            RestoreCursor();

            if (_canvas != null)
            {
                _canvas.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_canvas != null)
            {
                _canvas.enabled = true;
            }
        }

        private void OnDestroy()
        {
            ApplyDefaultCursor();
            RestoreCursor();

            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
            }

            if (_ringTexture != null)
            {
                Destroy(_ringTexture);
            }

            if (_dotTexture != null)
            {
                Destroy(_dotTexture);
            }

            foreach (var texture in _cursorTextures.Values)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }

            _cursorTextures.Clear();
        }

        private void BuildReticle()
        {
            // Its own canvas rather than a child of the HUD: the HUD canvases live in additive
            // scenes that load a frame late (CLAUDE.md), and the reticle must exist from frame one.
            // Parented to the player and built with its components in the constructor, exactly as
            // WeaponHudPresenter does it. The first version was a scene-root object with the
            // components added afterwards; it reported itself active, enabled and ordered and
            // still never drew a pixel (checked 2026-09-29 with a magenta test quad). The cause
            // was not isolated - this is the construction that is known to render.
            var canvasObject = new GameObject("AimReticleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = CanvasSortingOrder;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRect = canvasObject.GetComponent<RectTransform>();

            var rootObject = new GameObject("AimReticle", typeof(RectTransform));
            rootObject.transform.SetParent(_canvasRect, false);
            _root = rootObject.GetComponent<RectTransform>();
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = Vector2.zero;

            _ringTexture = CreateRingTexture(RingTextureSize, 0.36f, 0.46f);
            _dotTexture = CreateRingTexture(32, 0f, 0.42f);
            var ringSprite = Sprite.Create(_ringTexture, new Rect(0f, 0f, RingTextureSize, RingTextureSize), new Vector2(0.5f, 0.5f), 100f);
            var dotSprite = Sprite.Create(_dotTexture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 100f);

            // A dark copy one size up behind each bright part, so the reticle reads on the pale
            // stage floors as well as the dark ones.
            _ringBacking = CreateImage("RingBacking", ringSprite, RingSize + 5f, BackingColor);
            _ammoArc = CreateImage("AmmoArc", ringSprite, RingSize, ReadyColor);
            _ammoArc.type = Image.Type.Filled;
            _ammoArc.fillMethod = Image.FillMethod.Radial360;
            _ammoArc.fillOrigin = (int)Image.Origin360.Top;
            _ammoArc.fillClockwise = false;
            _dotBacking = CreateImage("DotBacking", dotSprite, DotSize + 4f, BackingColor);
            _dot = CreateImage("Dot", dotSprite, DotSize, ReadyColor);
        }

        private Image CreateImage(string name, Sprite sprite, float size, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(_root, false);
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        // An anti-aliased ring (or, with inner 0, a disc) as a fraction of the texture's radius.
        private static Texture2D CreateRingTexture(int size, float innerFraction, float outerFraction)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            var centre = (size - 1) * 0.5f;
            var inner = innerFraction * size;
            var outer = outerFraction * size;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
                    var alpha = Mathf.Clamp01(outer - distance + 0.5f);

                    if (inner > 0f)
                    {
                        alpha = Mathf.Min(alpha, Mathf.Clamp01(distance - inner + 0.5f));
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.DontSave;
            return texture;
        }

        private void LateUpdate()
        {
            if (_playerCombatController == null || _root == null)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            var menuOpen = IsPointerMenuOpen();
            var reader = InputReader;
            var usingGamepad = reader != null && reader.IsUsingGamepad;

            // The canvas reticle is the gamepad's; the mouse gets the hardware cursor below.
            _root.gameObject.SetActive(!menuOpen && usingGamepad);

            if (menuOpen)
            {
                ApplyDefaultCursor();
                RestoreCursor();
                return;
            }

            var origin = (Vector2)_playerCombatController.transform.position;
            var screenPoint = ResolveAimScreenPoint(reader, usingGamepad, origin, out var worldPoint);
            var reloading = _playerCombatController.IsPrimaryReloading;
            var onTarget = !reloading && IsAimingAtEnemy(worldPoint);

            if (!usingGamepad)
            {
                RestoreCursor();
                var fraction = reloading ? _playerCombatController.PrimaryReloadProgress : _playerCombatController.PrimaryAmmoNormalized;
                ApplyHardwareCursor(reloading ? 2 : onTarget ? 1 : 0, Mathf.RoundToInt(Mathf.Clamp01(fraction) * MagazineSteps));
                return;
            }

            ApplyDefaultCursor();
            HideCursor();

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, null, out var local))
            {
                _root.anchoredPosition = local;
            }

            var accent = onTarget ? TargetColor : ReadyColor;

            _ammoArc.fillAmount = reloading ? _playerCombatController.PrimaryReloadProgress : _playerCombatController.PrimaryAmmoNormalized;
            _ammoArc.color = reloading ? ReloadColor : accent;
            _dot.color = reloading ? ReloadColor : accent;

            var ringScale = onTarget ? TargetRingScale : 1f;
            _ammoArc.rectTransform.localScale = Vector3.one * ringScale;
            _ringBacking.rectTransform.localScale = Vector3.one * ringScale;
        }

        // state: 0 ready, 1 on target, 2 reloading. step: 0..MagazineSteps of the arc.
        private void ApplyHardwareCursor(int state, int step)
        {
            var key = state * 100 + step;

            if (key == _appliedCursorKey)
            {
                return;
            }

            if (!_cursorTextures.TryGetValue(key, out var texture) || texture == null)
            {
                texture = CreateCursorTexture(state == 2 ? ReloadColor : state == 1 ? TargetColor : ReadyColor, step / (float)MagazineSteps, state == 1);
                _cursorTextures[key] = texture;
            }

            Cursor.SetCursor(texture, new Vector2(CursorSize * 0.5f, CursorSize * 0.5f), CursorMode.Auto);
            _appliedCursorKey = key;
        }

        private void ApplyDefaultCursor()
        {
            if (_appliedCursorKey == -1)
            {
                return;
            }

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            _appliedCursorKey = -1;
        }

        // The same picture as the canvas reticle, drawn into a cursor-sized texture: a dark backing
        // ring, the bright magazine arc from the top counter-clockwise, and the centre dot. On target
        // the ring is drawn tighter, as the canvas version scales it.
        private static Texture2D CreateCursorTexture(Color accent, float fill, bool tight)
        {
            var size = CursorSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            var pixels = new Color32[size * size];
            var centre = (size - 1) * 0.5f;
            var outer = tight ? 12.5f : 14.5f;
            var inner = outer - 3.2f;
            var backingOuter = outer + 1.2f;
            var backingInner = inner - 1.2f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - centre;
                    var dy = y - centre;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var color = new Color(0f, 0f, 0f, 0f);

                    var backing = Mathf.Min(Mathf.Clamp01(backingOuter - distance + 0.5f), Mathf.Clamp01(distance - backingInner + 0.5f));
                    color = Color.Lerp(color, BackingColor, backing * BackingColor.a / Mathf.Max(0.001f, BackingColor.a));
                    color.a = Mathf.Max(color.a, backing * BackingColor.a);

                    // Angle measured from the top, counter-clockwise, 0..1 - the Image fill origin
                    // and direction the canvas reticle uses.
                    var angle = Mathf.Atan2(-dx, dy) / (Mathf.PI * 2f);

                    if (angle < 0f)
                    {
                        angle += 1f;
                    }

                    var ring = Mathf.Min(Mathf.Clamp01(outer - distance + 0.5f), Mathf.Clamp01(distance - inner + 0.5f));

                    if (ring > 0f && angle <= fill)
                    {
                        color = Color.Lerp(color, accent, ring);
                        color.a = Mathf.Max(color.a, ring * accent.a);
                    }

                    var dotBacking = Mathf.Clamp01(3.6f - distance + 0.5f);
                    color = Color.Lerp(color, BackingColor, dotBacking);
                    color.a = Mathf.Max(color.a, dotBacking * BackingColor.a);
                    var dot = Mathf.Clamp01(2.2f - distance + 0.5f);
                    color = Color.Lerp(color, accent, dot);
                    color.a = Mathf.Max(color.a, dot);

                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            texture.hideFlags = HideFlags.DontSave;
            return texture;
        }

        // Menus that are driven by the pointer. Pause, run-end and tutorial beats hold the
        // time-scale guard and the loadout panel has its own flag. The reward screen and the shop
        // are asked directly: their UIManager registrations point at a root that stays at alpha 0
        // while the visible content lives in the additive canvas scenes, so "is a blocking panel
        // visible" answered no with the reward cards on screen (checked 2026-09-29).
        private Corebound.Gameplay.Cards.CardRewardController _rewardController;
        private Corebound.Gameplay.Shop.ShopService _shopService;
        private float _nextServiceLookup;

        private bool IsPointerMenuOpen()
        {
            if (GameplayTimeScaleGuard.IsHeld || WeaponLoadoutPanelPresenter.IsOpen)
            {
                return true;
            }

            if ((_rewardController == null || _shopService == null) && Time.unscaledTime >= _nextServiceLookup)
            {
                _nextServiceLookup = Time.unscaledTime + 1f;
                _rewardController = _rewardController != null ? _rewardController : FindFirstObjectByType<Corebound.Gameplay.Cards.CardRewardController>();
                _shopService = _shopService != null ? _shopService : FindFirstObjectByType<Corebound.Gameplay.Shop.ShopService>();
            }

            if ((_rewardController != null && _rewardController.IsOpen) || (_shopService != null && _shopService.IsOpen))
            {
                return true;
            }

            var manager = UIManager.Instance;
            return manager != null && manager.HasBlockingPanelVisible;
        }

        private Vector2 ResolveAimScreenPoint(IInputReader reader, bool usingGamepad, Vector2 origin, out Vector2 worldPoint)
        {
            worldPoint = origin;

            if (reader == null || _camera == null)
            {
                return _camera != null ? (Vector2)_camera.WorldToScreenPoint(origin) : Vector2.zero;
            }

            // PlayerAimResolver reads Aim as a *screen position* in mouse mode and as a direction on
            // a stick - the same distinction that once made every gamepad shot fly at a screen
            // corner. Keeping both branches explicit here avoids repeating that mistake.
            if (usingGamepad)
            {
                var direction = reader.Aim;
                worldPoint = origin + (direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.right) * OrbitRadius;
                return _camera.WorldToScreenPoint(worldPoint);
            }

            var screen = reader.Aim;
            var world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            worldPoint = new Vector2(world.x, world.y);
            return screen;
        }

        private bool IsAimingAtEnemy(Vector2 aimPoint)
        {
            var count = Physics2D.OverlapCircleNonAlloc(aimPoint, _targetProbeRadius, _probeBuffer);

            for (var index = 0; index < count; index++)
            {
                var collider = _probeBuffer[index];

                if (collider == null)
                {
                    continue;
                }

                var receiver = (collider.attachedRigidbody != null ? collider.attachedRigidbody.GetComponent<CombatHitReceiver>() : null)
                    ?? collider.GetComponent<CombatHitReceiver>();

                if (receiver != null && receiver.IsAlive && receiver.gameObject != _playerCombatController.gameObject)
                {
                    return true;
                }
            }

            return false;
        }

        private void HideCursor()
        {
            if (_cursorHidden)
            {
                return;
            }

            Cursor.visible = false;
            _cursorHidden = true;
        }

        private void RestoreCursor()
        {
            if (!_cursorHidden)
            {
                return;
            }

            // Restored on disable and whenever a menu opens, so the player is never left without
            // a pointer where one is needed.
            Cursor.visible = true;
            _cursorHidden = false;
        }
    }
}
