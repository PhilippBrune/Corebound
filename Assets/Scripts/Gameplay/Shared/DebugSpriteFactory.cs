using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Gameplay.Shared
{
    public static class DebugSpriteFactory
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> AtlasTextureCache = new Dictionary<string, Texture2D>();

        // NOTE on pixelsPerUnit: Texture2D.whiteTexture is 4x4, so CreateSquare(100f) is 0.04
        // world units, not 1. Callers that then apply a localScale meant to read as world size end
        // up with something invisible - which is exactly what happened to the aim reticle on
        // 2026-09-29. Use CreateSquare(UnitPixelsPerUnit) when you want a one-unit quad and to
        // treat localScale as world size.
        public const float UnitPixelsPerUnit = 4f;

        public static Sprite CreateSquare(float pixelsPerUnit)
        {
            var sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            sprite.hideFlags = HideFlags.DontSaveInEditor;
            return sprite;
        }

        private static Sprite _ringSprite;

        // A thin one-unit ring, drawn once and shared: the Target Painter mark on enemies and the
        // Reactive Plating shield around the player (NEW_CARD_MECHANICS, 2026-09-29).
        public static Sprite ResolveRingSprite()
        {
            if (_ringSprite != null)
            {
                return _ringSprite;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var centre = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
                    var alpha = Mathf.Min(Mathf.Clamp01(31f - distance), Mathf.Clamp01(distance - 26f));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.DontSaveInEditor;
            _ringSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _ringSprite.hideFlags = HideFlags.DontSaveInEditor;
            return _ringSprite;
        }

        // mipmaps: for art drawn far below its file size (a 900 px player sprite shown at ~35 px);
        // without a mip chain the downscale aliases into noise.
        public static Texture2D LoadTextureFromProjectFile(string projectRelativePath, bool mipmaps = false)
        {
            if (!TryReadArtBytes(projectRelativePath, out var bytes))
            {
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipmaps);
            texture.filterMode = mipmaps ? FilterMode.Trilinear : texture.filterMode;
            texture.name = Path.GetFileNameWithoutExtension(projectRelativePath);
            texture.hideFlags = HideFlags.DontSaveInEditor;

            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(texture);
                }
                else
                {
                    Object.DestroyImmediate(texture);
                }

                return null;
            }

            return texture;
        }

        // Runtime art (Player/Enemy/Boss/Tower/Projectile/UI sprites, arena atlases) is
        // authored under Assets/Art/... but a packaged Player build does not ship the raw
        // Assets/ folder, so File.ReadAllBytes against Application.dataPath only ever works
        // inside the Editor. Assets/StreamingAssets/Art/ mirrors the same relative layout and
        // *is* copied into every build, and Application.streamingAssetsPath resolves correctly
        // both in the Editor and in a real Standalone build, so it is tried first. The original
        // Assets/-relative read is kept as an Editor-only convenience fallback for art that has
        // not yet been mirrored into StreamingAssets. (Assumes a desktop Standalone target per
        // the project's platform baseline; Android would need UnityWebRequest instead of File IO
        // for StreamingAssets reads.)
        private static bool TryReadArtBytes(string projectRelativePath, out byte[] bytes)
        {
            bytes = null;

            if (string.IsNullOrWhiteSpace(projectRelativePath) || !projectRelativePath.StartsWith("Assets/Art/"))
            {
                return false;
            }

            var relativeFilePath = projectRelativePath.Substring("Assets/Art/".Length).Replace('/', Path.DirectorySeparatorChar);
            var streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "Art", relativeFilePath);

            if (File.Exists(streamingAssetsPath))
            {
                bytes = File.ReadAllBytes(streamingAssetsPath);
                return bytes != null && bytes.Length > 0;
            }

#if UNITY_EDITOR
            var editorAssetPath = Path.Combine(Application.dataPath, projectRelativePath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(editorAssetPath))
            {
                bytes = File.ReadAllBytes(editorAssetPath);
                return bytes != null && bytes.Length > 0;
            }
#endif

            return false;
        }

        public static Sprite CreateSprite(Texture2D texture, float pixelsPerUnit, Vector4 border = default)
        {
            if (texture == null)
            {
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0u,
                SpriteMeshType.FullRect,
                border);
            sprite.hideFlags = HideFlags.DontSaveInEditor;
            return sprite;
        }

        public static Sprite LoadProjectSprite(string projectRelativePath, float pixelsPerUnit = 100f)
        {
            if (string.IsNullOrWhiteSpace(projectRelativePath))
            {
                return null;
            }

            if (SpriteCache.TryGetValue(projectRelativePath, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            var texture = LoadTextureFromProjectFile(projectRelativePath);
            var sprite = CreateSprite(texture, pixelsPerUnit);

            if (sprite == null)
            {
                return null;
            }

            SpriteCache[projectRelativePath] = sprite;
            return sprite;
        }

        // The file cropped to its visible figure (alpha above alphaThreshold), pivot in the figure's
        // centre, and sized so its longer side is longerSideUnits at scale 1. For art that will be
        // redrawn and redelivered with different padding (the player sprites, 2026-10-07): the
        // figure keeps its size and stays centred on the transform however much empty canvas the
        // next file brings.
        public static Sprite LoadProjectSpriteTrimmed(string projectRelativePath, float longerSideUnits, float alphaThreshold = 0.08f)
        {
            if (string.IsNullOrWhiteSpace(projectRelativePath) || longerSideUnits <= 0f)
            {
                return null;
            }

            var cacheKey = projectRelativePath + "#trimmed#" + longerSideUnits.ToString("0.####");

            if (SpriteCache.TryGetValue(cacheKey, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            var texture = LoadTextureFromProjectFile(projectRelativePath, true);

            if (texture == null)
            {
                return null;
            }

            var pixels = texture.GetPixels32();
            var threshold = (byte)Mathf.Clamp(Mathf.RoundToInt(alphaThreshold * 255f), 0, 255);
            int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;

            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    if (pixels[y * texture.width + x].a <= threshold)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            var rect = maxX < 0
                ? new Rect(0f, 0f, texture.width, texture.height)
                : new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            var pixelsPerUnit = Mathf.Max(rect.width, rect.height) / longerSideUnits;
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags = HideFlags.DontSaveInEditor;
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }

        // Loads a sprite from an explicit pixel Rect within the source file instead of the whole
        // canvas - needed when a texture's own visible artwork doesn't fill its canvas (e.g. a
        // texture re-sliced in the Sprite Editor down the line, whose canvas keeps its original
        // full size/padding even though only a sub-rect is meant to be the actual sprite), so
        // FitTransformToSprite doesn't undersize the result against that oversized full-canvas rect.
        public static Sprite LoadProjectSpriteRect(string projectRelativePath, Rect pixelRect, float pixelsPerUnit = 100f, Vector4 border = default)
        {
            if (string.IsNullOrWhiteSpace(projectRelativePath))
            {
                return null;
            }

            var cacheKey = projectRelativePath + "#" + pixelRect + "#" + border;

            if (SpriteCache.TryGetValue(cacheKey, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            var texture = LoadTextureFromProjectFile(projectRelativePath);

            if (texture == null)
            {
                return null;
            }

            var sprite = Sprite.Create(texture, pixelRect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0u, SpriteMeshType.FullRect, border);
            sprite.hideFlags = HideFlags.DontSaveInEditor;
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }

        // Loads a single cell out of a uniform-grid sprite sheet (e.g. card_tag_icon_sheet.png's
        // 4x2 icon grid, projectile_sheet_core.png's 4x3 variant grid) via runtime pixel-Rect
        // slicing - the same "valid, gleichwertige Alternative" to pre-sliced Unity sprites that
        // Docs/Specs/Graphics.md documents for StageArenaArtPresenter's ground/prop atlases.
        // column/row are 0-based from the top-left, matching how the sheets read visually;
        // Unity's pixel-Rect origin is bottom-left, so the row is flipped internally.
        // The pixel size of an atlas sheet, loading it into the same cache the cell loader uses so
        // asking costs nothing extra. Zero when the file cannot be read.
        public static Vector2Int ResolveProjectAtlasSize(string projectRelativePath)
        {
            if (string.IsNullOrWhiteSpace(projectRelativePath))
            {
                return Vector2Int.zero;
            }

            if (!AtlasTextureCache.TryGetValue(projectRelativePath, out var texture) || texture == null)
            {
                texture = LoadTextureFromProjectFile(projectRelativePath);
                AtlasTextureCache[projectRelativePath] = texture;
            }

            return texture != null ? new Vector2Int(texture.width, texture.height) : Vector2Int.zero;
        }

        public static Sprite LoadProjectAtlasCellSprite(string projectRelativePath, int column, int row, int columns, int rows, float pixelsPerUnit = 100f)
        {
            if (string.IsNullOrWhiteSpace(projectRelativePath) || columns <= 0 || rows <= 0)
            {
                return null;
            }

            var cacheKey = projectRelativePath + "#" + column + "," + row + "/" + columns + "x" + rows;

            if (SpriteCache.TryGetValue(cacheKey, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            if (!AtlasTextureCache.TryGetValue(projectRelativePath, out var texture) || texture == null)
            {
                texture = LoadTextureFromProjectFile(projectRelativePath);
                AtlasTextureCache[projectRelativePath] = texture;
            }

            if (texture == null)
            {
                return null;
            }

            var cellWidth = texture.width / (float)columns;
            var cellHeight = texture.height / (float)rows;
            var clampedColumn = Mathf.Clamp(column, 0, columns - 1);
            var clampedRow = Mathf.Clamp(row, 0, rows - 1);
            var pixelRect = new Rect(
                clampedColumn * cellWidth,
                texture.height - (clampedRow + 1) * cellHeight,
                cellWidth,
                cellHeight);

            var sprite = Sprite.Create(texture, pixelRect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0u, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.DontSaveInEditor;
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }

        // Fits by what you can actually see rather than by the file's dimensions.
        //
        // Playtest 2026-09-27: "make the core as big as the towers". The Core was already being
        // fitted into a 0.95-unit box against the tower's 0.9, so on paper it matched - but
        // core_main_sprite.png is 1090x606 for a reactor that occupies about 560px of that width,
        // so fitting the *file* scaled the padding and left the visible Core at roughly half the
        // intended size. Measuring the opaque region first fixes it here and for any other art
        // delivered with generous padding, instead of hiding it in a per-stage scale number that
        // silently becomes wrong the next time a sprite is redrawn.
        private static readonly Dictionary<Texture2D, Vector2> OpaqueSizeCache = new Dictionary<Texture2D, Vector2>();

        public static Vector2 GetOpaqueSizePixels(Texture2D texture)
        {
            if (texture == null)
            {
                return Vector2.zero;
            }

            if (OpaqueSizeCache.TryGetValue(texture, out var cached))
            {
                return cached;
            }

            var result = new Vector2(texture.width, texture.height);

            try
            {
                var pixels = texture.GetPixels32();
                int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;

                for (var y = 0; y < texture.height; y++)
                {
                    var rowStart = y * texture.width;

                    for (var x = 0; x < texture.width; x++)
                    {
                        // 8 rather than 0: the delivered art has faint anti-aliased dust in the
                        // padding that would otherwise count as content.
                        if (pixels[rowStart + x].a <= 8)
                        {
                            continue;
                        }

                        if (x < minX) { minX = x; }
                        if (x > maxX) { maxX = x; }
                        if (y < minY) { minY = y; }
                        if (y > maxY) { maxY = y; }
                    }
                }

                if (maxX >= minX && maxY >= minY)
                {
                    result = new Vector2(maxX - minX + 1, maxY - minY + 1);
                }
            }
            catch (UnityException)
            {
                // Not readable (an imported asset without Read/Write enabled): fall back to the
                // full size, which is the pre-2026-09-27 behaviour rather than a broken one.
            }

            OpaqueSizeCache[texture] = result;
            return result;
        }

        public static void FitTransformToSpriteContent(Transform targetTransform, Sprite sprite, Texture2D texture, Vector2 targetWorldSize)
        {
            if (targetTransform == null || sprite == null)
            {
                return;
            }

            var opaquePixels = GetOpaqueSizePixels(texture);

            if (opaquePixels.x <= Mathf.Epsilon || opaquePixels.y <= Mathf.Epsilon)
            {
                FitTransformToSprite(targetTransform, sprite, targetWorldSize);
                return;
            }

            var pixelsPerUnit = sprite.pixelsPerUnit > Mathf.Epsilon ? sprite.pixelsPerUnit : 100f;
            var contentWorldSize = opaquePixels / pixelsPerUnit;
            var scaleFactor = Mathf.Min(targetWorldSize.x / contentWorldSize.x, targetWorldSize.y / contentWorldSize.y);
            targetTransform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }

        public static void FitTransformToSprite(Transform targetTransform, Sprite sprite, Vector2 targetWorldSize)
        {
            if (targetTransform == null || sprite == null)
            {
                return;
            }

            var bounds = sprite.bounds.size;

            if (bounds.x <= Mathf.Epsilon || bounds.y <= Mathf.Epsilon)
            {
                return;
            }

            var scaleFactor = Mathf.Min(targetWorldSize.x / bounds.x, targetWorldSize.y / bounds.y);
            targetTransform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }
    }
}