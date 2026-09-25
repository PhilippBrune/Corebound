using System.Collections.Generic;
using System;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Waves;
using UnityEngine;

// These were private nested structs here until 2026-09-20. They moved to StageArenaLayoutData.cs
// so prop layouts can be authored per stage in an asset instead of being compiled into static
// arrays; aliasing them keeps every existing usage in this file unchanged.
using AtlasCell = Corebound.Gameplay.Shared.ArenaAtlasCell;
using PropPlacementData = Corebound.Gameplay.Shared.ArenaPropPlacement;

namespace Corebound.Gameplay.Shared
{
    // Known debt: this class owns procedural maze-cell generation, atlas-cell lookup/rotation,
    // ground/boundary/prop tiling, collider creation, and runtime texture/sprite lifecycle in one
    // MonoBehaviour. 2026-09-08: the maze open-cell layout algorithm was extracted into
    // StageArenaMazeGenerator (a plain, Unity-object-free class, now unit-testable apart from
    // Unity) since it took only a System.Random and had no dependency on this presenter's
    // rendering/collider/texture state. Atlas-cell lookup/rotation, tiling and collider creation
    // remain here - they're coupled to this class's static atlas-cell constants and rendering
    // fields, and this is the class that draws the entire playable arena, so splitting those
    // further needs Editor access to see the result before risking it.
    public sealed class StageArenaArtPresenter : MonoBehaviour
    {
        private const int GroundAtlasColumns = 8;
        private const int GroundAtlasRows = 4;
        private const int PropAtlasColumns = 7;
        private const int PropAtlasRows = 3;
        private const float DefaultGroundTileWorldSize = 2f;
        private const string GroundTexturePath = "Assets/Art/Environment/Arena/stage1_ground_tileset.png";
        private const string BoundaryTexturePath = "Assets/Art/Environment/Arena/stage1_boundary_set.png";
        private const string PropTexturePath = "Assets/Art/Environment/Arena/stage1_prop_set.png";
        private const string CoreTexturePath = "Assets/Art/Environment/Arena/core_main_sprite.png";
        private const string SpawnGateTexturePath = "Assets/Art/Environment/Arena/stage1_spawn_gate.png";

        // stage1_boundary_set.png is a hand-composed sheet, not a uniform grid (its 18 pieces
        // have varying widths/heights), so its pieces are addressed by their exact pixel rect
        // (already sliced into Unity sprite metadata) rather than the row/column AtlasCell
        // division used for the ground/prop atlases. Wall segment (index 2, the widest plain
        // barricade run) tiles along each edge and is also reused, rotated, as the corner patch -
        // the sheet's own dedicated corner piece is drawn at a 3/4 perspective that doesn't match
        // this flat wall segment or the flat top-down ground tileset (see CreateTiledBoundaryBand).
        private static readonly Rect BoundaryWallPixelRect = new Rect(401f, 464f, 320f, 109f);

        private static readonly AtlasCell TopLeftCornerCell = new AtlasCell(1, 1);
        private static readonly AtlasCell TopRightCornerCell = new AtlasCell(1, 8);
        private static readonly AtlasCell BottomLeftCornerCell = new AtlasCell(4, 1);
        private static readonly AtlasCell BottomRightCornerCell = new AtlasCell(1, 1, 180f);
        private static readonly AtlasCell[] TopEdgeCells =
        {
            new AtlasCell(1, 2),
            new AtlasCell(1, 3),
            new AtlasCell(1, 4),
            new AtlasCell(1, 6),
            new AtlasCell(1, 7)
        };

        private static readonly AtlasCell[] BottomEdgeCells =
        {
            new AtlasCell(2, 6),
            new AtlasCell(4, 2),
            new AtlasCell(4, 3),
            new AtlasCell(4, 4),
            new AtlasCell(4, 5)
        };

        private static readonly AtlasCell[] LeftEdgeCells =
        {
            new AtlasCell(2, 1),
            new AtlasCell(3, 1)
        };

        private static readonly AtlasCell[] RightEdgeCells =
        {
            new AtlasCell(2, 8),
            new AtlasCell(3, 8)
        };

        private static readonly AtlasCell[] InteriorCells =
        {
            new AtlasCell(1, 5),
            new AtlasCell(2, 2),
            new AtlasCell(2, 3),
            new AtlasCell(2, 4),
            new AtlasCell(2, 5),
            new AtlasCell(2, 7),
            new AtlasCell(3, 2),
            new AtlasCell(3, 3),
            new AtlasCell(3, 4),
            new AtlasCell(3, 5)
        };

        private static readonly AtlasCell[] InteriorDetailCells =
        {
            new AtlasCell(3, 6),
            new AtlasCell(3, 7),
            new AtlasCell(4, 6),
            new AtlasCell(4, 7),
            new AtlasCell(4, 8)
        };

        // Target sizes scaled to ~0.6x (playtest feedback 2026-09-15: "resize stage1_prop_set.png
        // and make them smaller, around the size of tower_slot_marker.png" - the slot marker
        // renders at 1.8x1.8, see TowerService.SlotMarkerTargetWorldSize) - was up to 2x1.95,
        // now tops out at 1.2x1.17, reading as similar-or-smaller than a tower slot instead of
        // dominating the arena. Local positions are left as-is (still spread across the same
        // layout), only the rendered footprint shrinks.
        private static readonly PropPlacementData[] Stage1PropPlacements =
        {
            new PropPlacementData(new AtlasCell(1, 2), new Vector2(-6.1f, 3.45f), new Vector2(0.81f, 1.17f), -8, true),
            new PropPlacementData(new AtlasCell(1, 5), new Vector2(0f, 3.65f), new Vector2(0.87f, 1.11f), -8, true),
            new PropPlacementData(new AtlasCell(1, 4), new Vector2(6.05f, 3.4f), new Vector2(0.87f, 1.17f), -8, true),
            new PropPlacementData(new AtlasCell(2, 2), new Vector2(-6.3f, 0.85f), new Vector2(1.08f, 1.11f), -8, true),
            new PropPlacementData(new AtlasCell(2, 4), new Vector2(6.15f, 0.7f), new Vector2(0.99f, 0.99f), -8, true),
            new PropPlacementData(new AtlasCell(2, 6), new Vector2(-0.25f, 4.05f), new Vector2(0.78f, 0.99f), -8, true),
            new PropPlacementData(new AtlasCell(3, 1), new Vector2(-5.75f, -3.55f), new Vector2(0.87f, 0.75f), -8, true),
            new PropPlacementData(new AtlasCell(3, 5), new Vector2(5.75f, -3.55f), new Vector2(1.2f, 0.63f), -8, true),
            new PropPlacementData(new AtlasCell(3, 6), new Vector2(0.2f, -4.15f), new Vector2(0.99f, 0.49f), -8, true)
        };

        // Stage 2's prop layout used to live here as a second static array selected by a
        // ThemeId == "theme_stage_02" check. It now lives in ArenaLayout_Stage2.asset, along with
        // every other stage's, because that branch was exactly what the Dynamic Arena Slice's
        // Acceptance Criteria forbid. Stage1PropPlacements below stays only as the fallback for a
        // stage that authors no layout at all - it is measured against stage1_prop_set.png, so any
        // stage on a different prop sheet must author its own placements (Corebound > Validate Art
        // Pipeline reports a stage whose layout is missing).
        private static readonly AtlasCell[] MazeBlockerCells =
        {
            new AtlasCell(1, 1),
            new AtlasCell(1, 3),
            new AtlasCell(1, 7),
            new AtlasCell(2, 5),
            new AtlasCell(2, 7),
            new AtlasCell(3, 2),
            new AtlasCell(3, 4),
            new AtlasCell(3, 5)
        };

        [SerializeField] private StageData _stageData;
        [SerializeField] private Transform _coreTarget;
        [SerializeField] private Texture2D _groundTexture;
        [SerializeField] private Texture2D _boundaryTexture;
        [SerializeField] private Texture2D _propTexture;
        [SerializeField] private Texture2D _coreTexture;
        [SerializeField] private Vector2 _arenaSize = new Vector2(16f, 10f);
        [SerializeField] private float _boundaryThickness = 0.8f;
        [SerializeField] private float _coreScale = 1.4f;

        private readonly List<Sprite> _runtimeSprites = new List<Sprite>();
        private readonly List<Vector2> _walkableInteriorPositions = new List<Vector2>();
        private Texture2D _runtimeGroundTexture;
        private Texture2D _runtimeBoundaryTexture;
        private Texture2D _runtimePropTexture;
        private Texture2D _runtimeCoreTexture;
        private Texture2D _runtimeSpawnGateTexture;

        private const float SpawnGatePulseBeatsPerSecond = 1.5f;
        private const float SpawnGatePulseScale = 0.12f;
        private const float SpawnGateIdleBrightness = 0.72f;
        private static readonly Color SpawnGatePulseColor = new Color(1f, 0.55f, 0.25f, 1f);
        private readonly List<SpawnGateVisual> _spawnGates = new List<SpawnGateVisual>();
        private Sprite _spawnGateGlowSprite;

        // Set by BuildArenaVisuals() from whichever source (StageThemeData override or the
        // _arenaSize field) actually decided the built arena's extents, so CameraFollowController
        // can clamp to the real playable bounds instead of duplicating this resolution logic (and
        // instead of hardcoding a size that would drift out of sync with a StageThemeData change).
        public Vector2 ArenaSize { get; private set; }

        private void Awake()
        {
            BuildArenaVisuals();
        }

        // Called by WaveManager after a stage transition (2026-09-09). BuildArenaVisuals() was
        // only ever designed to run once from Awake() - none of its Create* helpers clear existing
        // children first, so calling it again without first destroying the previous visuals would
        // leave the old stage's tiles/props/boundary underneath the new ones instead of replacing
        // them. ResolveStageThemeData() already falls back to a live WaveManager lookup when this
        // presenter's own _stageData field is unset, so no change was needed there - it picks up
        // the new stage's theme automatically once WaveManager's active StageData changes.
        public void RebuildForNewStage()
        {
            var existingRoot = GameObject.Find("ArenaVisualsRoot");

            if (existingRoot != null)
            {
                if (Application.isPlaying)
                {
                    // Destroy() is deferred to the end of the frame, so this object stays alive
                    // and findable by name for the rest of this frame. BuildArenaVisuals() below
                    // resolves its parent with EnsureWorldRoot("ArenaVisualsRoot"), which is a
                    // GameObject.Find() - so it would find this doomed root, build the entire new
                    // arena underneath it, and then Unity would destroy the lot at end of frame,
                    // leaving a mid-run stage transition with no arena at all (playtest 2026-09-19:
                    // "reaching Stage 3 from Stage 2 does not load the tiles", while starting a new
                    // run directly on Stage 3 worked fine). Renaming and deactivating first makes
                    // the old root unfindable immediately. Edit Mode never hit this because
                    // DestroyImmediate() removes the object before the Find happens - which is why
                    // the Corebound > Preview Stage Arena menu looked correct the whole time.
                    existingRoot.name = "ArenaVisualsRoot_Discarded";
                    existingRoot.SetActive(false);
                    Destroy(existingRoot);
                }
                else
                {
                    DestroyImmediate(existingRoot);
                }
            }

            CleanupSprites();
            CleanupTexture(_runtimeGroundTexture);
            CleanupTexture(_runtimeBoundaryTexture);
            CleanupTexture(_runtimePropTexture);
            CleanupTexture(_runtimeCoreTexture);
            CleanupTexture(_runtimeSpawnGateTexture);
            _runtimeGroundTexture = null;
            _runtimeBoundaryTexture = null;
            _runtimePropTexture = null;
            _runtimeCoreTexture = null;
            _runtimeSpawnGateTexture = null;

            BuildArenaVisuals();
        }

        private void OnDestroy()
        {
            CleanupSprites();
            CleanupTexture(_runtimeGroundTexture);
            CleanupTexture(_runtimeBoundaryTexture);
            CleanupTexture(_runtimePropTexture);
            CleanupTexture(_runtimeCoreTexture);
            CleanupTexture(_runtimeSpawnGateTexture);
        }

        private void BuildArenaVisuals()
        {
            var stageThemeData = ResolveStageThemeData();

            // Per-stage arena geometry (Dynamic Arena Slice). When a stage authors a layout asset
            // it decides size, boundary thickness, obstacle-grid shape, prop placement and tower
            // slots; otherwise every one of those falls back to the values that shipped before,
            // which is what keeps Stage 1 bit-identical.
            var layoutData = stageThemeData != null ? stageThemeData.LayoutData : null;

            var arenaSize = layoutData != null
                ? layoutData.ArenaSize
                : (stageThemeData != null ? stageThemeData.ArenaSize : _arenaSize);
            ArenaSize = arenaSize;

            var boundaryThickness = layoutData != null
                ? layoutData.BoundaryThickness
                : (stageThemeData != null ? stageThemeData.BoundaryThickness : _boundaryThickness);

            var layoutRandom = new System.Random(Environment.TickCount ^ arenaSize.GetHashCode());
            var openCells = BuildOpenMazeCells(layoutData, layoutRandom);
            var visualsRoot = EnsureWorldRoot("ArenaVisualsRoot");
            visualsRoot.position = Vector3.zero;
            var groundTexture = ResolveTexture(_groundTexture, stageThemeData != null ? stageThemeData.GroundTexture : null, ref _runtimeGroundTexture, stageThemeData != null ? stageThemeData.GroundTexturePath : null, GroundTexturePath);
            var groundTint = stageThemeData != null ? stageThemeData.GroundTint : Color.white;
            CreateBackdropTiles(visualsRoot, groundTexture, arenaSize, groundTint);
            CreateGroundTiles(visualsRoot, groundTexture, arenaSize, groundTint);
            CreateBoundaryVisuals(
                visualsRoot,
                ResolveTexture(_boundaryTexture, stageThemeData != null ? stageThemeData.BoundaryTexture : null, ref _runtimeBoundaryTexture, stageThemeData != null ? stageThemeData.BoundaryTexturePath : null, BoundaryTexturePath),
                arenaSize,
                boundaryThickness,
                stageThemeData != null ? stageThemeData.BoundaryTint : new Color(0.12f, 0.16f, 0.22f, 0.95f));
            EnsureBoundaryColliders(visualsRoot, arenaSize, boundaryThickness);
            CreatePropClusters(visualsRoot, stageThemeData, layoutData, arenaSize);
            CreateMazeBlockers(visualsRoot, propTexture: ResolveTexture(_propTexture, stageThemeData != null ? stageThemeData.PropTexture : null, ref _runtimePropTexture, stageThemeData != null ? stageThemeData.PropTexturePath : null, PropTexturePath), stageThemeData != null ? stageThemeData.PropTint : Color.white, arenaSize, openCells, layoutRandom, layoutData);
            CreateSpawnGateVisual(visualsRoot, stageThemeData);
            ApplyCoreVisual(
                ResolveTexture(_coreTexture, stageThemeData != null ? stageThemeData.CoreTexture : null, ref _runtimeCoreTexture, stageThemeData != null ? stageThemeData.CoreTexturePath : null, CoreTexturePath),
                arenaSize,
                stageThemeData != null ? stageThemeData.CoreScale : _coreScale,
                layoutRandom);
        }

        private void CreateLayeredSprite(Transform parent, string childName, Texture2D texture, Vector2 targetSize, int sortingOrder, Color color)
        {
            if (texture == null)
            {
                return;
            }

            var sprite = TrackSprite(DebugSpriteFactory.CreateSprite(texture, 100f));
            CreateSpriteVisual(parent, childName, sprite, Vector2.zero, targetSize, sortingOrder, color);
        }

        private void CreateGroundTiles(Transform parent, Texture2D groundTexture, Vector2 arenaSize, Color tint)
        {
            CreateGroundTiles(parent, groundTexture, arenaSize, tint, "GroundTilesRoot", -20, DefaultGroundTileWorldSize);
        }

        // Out-of-bounds floor under the arena (2026-09-22). Cover framing keeps the view inside the
        // walls, but at the minimum zoom on a very wide screen, or while the zoom is still easing in,
        // a sliver past the walls can show - it now shows darkened floor, not the camera colour.
        // Larger tiles than the arena's own, since nobody plays on it and it is several arenas wide.
        private void CreateBackdropTiles(Transform parent, Texture2D groundTexture, Vector2 arenaSize, Color tint)
        {
            var backdropTint = new Color(tint.r * 0.35f, tint.g * 0.35f, tint.b * 0.4f, 1f);
            CreateGroundTiles(parent, groundTexture, arenaSize * 3f, backdropTint, "BackdropTilesRoot", -30, 4f);
        }

        private void CreateGroundTiles(Transform parent, Texture2D groundTexture, Vector2 arenaSize, Color tint, string rootName, int sortingOrder, float tileWorldSize)
        {
            if (groundTexture == null)
            {
                return;
            }

            var groundRoot = EnsureChild(rootName, parent);
            var layoutColumns = Mathf.Max(4, Mathf.RoundToInt(arenaSize.x / tileWorldSize));
            var layoutRows = Mathf.Max(3, Mathf.RoundToInt(arenaSize.y / tileWorldSize));
            var tileSize = new Vector2(arenaSize.x / layoutColumns, arenaSize.y / layoutRows);

            for (var row = 0; row < layoutRows; row++)
            {
                for (var column = 0; column < layoutColumns; column++)
                {
                    var atlasCell = ResolveGroundCell(row, column, layoutRows, layoutColumns);
                    var sprite = CreateAtlasSprite(groundTexture, atlasCell, GroundAtlasRows, GroundAtlasColumns);
                    var localPosition = new Vector2(
                        -arenaSize.x * 0.5f + tileSize.x * 0.5f + column * tileSize.x,
                        arenaSize.y * 0.5f - tileSize.y * 0.5f - row * tileSize.y);

                    CreateSpriteVisual(
                        groundRoot,
                        "GroundTile_" + row + "_" + column,
                        sprite,
                        localPosition,
                        tileSize,
                        sortingOrder,
                        tint,
                        atlasCell.RotationDegrees,
                        false);
                }
            }
        }

        private void EnsureBoundaryColliders(Transform parent, Vector2 arenaSize, float boundaryThickness)
        {
            CreateBoundaryCollider("BoundaryTop", parent, new Vector2(0f, arenaSize.y * 0.5f + boundaryThickness * 0.5f), new Vector2(arenaSize.x, boundaryThickness));
            CreateBoundaryCollider("BoundaryBottom", parent, new Vector2(0f, -arenaSize.y * 0.5f - boundaryThickness * 0.5f), new Vector2(arenaSize.x, boundaryThickness));
            CreateBoundaryCollider("BoundaryLeft", parent, new Vector2(-arenaSize.x * 0.5f - boundaryThickness * 0.5f, 0f), new Vector2(boundaryThickness, arenaSize.y));
            CreateBoundaryCollider("BoundaryRight", parent, new Vector2(arenaSize.x * 0.5f + boundaryThickness * 0.5f, 0f), new Vector2(boundaryThickness, arenaSize.y));
        }

        private void CreateBoundaryVisuals(Transform parent, Texture2D boundaryTexture, Vector2 arenaSize, float boundaryThickness, Color boundaryTint)
        {
            if (boundaryTexture == null)
            {
                CreateBoundaryVisual("BoundaryVisualTop", parent, new Vector2(0f, arenaSize.y * 0.5f), new Vector2(arenaSize.x, boundaryThickness * 0.9f), boundaryTint);
                CreateBoundaryVisual("BoundaryVisualBottom", parent, new Vector2(0f, -arenaSize.y * 0.5f), new Vector2(arenaSize.x, boundaryThickness * 0.9f), boundaryTint);
                CreateBoundaryVisual("BoundaryVisualLeft", parent, new Vector2(-arenaSize.x * 0.5f, 0f), new Vector2(boundaryThickness * 0.9f, arenaSize.y), boundaryTint);
                CreateBoundaryVisual("BoundaryVisualRight", parent, new Vector2(arenaSize.x * 0.5f, 0f), new Vector2(boundaryThickness * 0.9f, arenaSize.y), boundaryTint);
                return;
            }

            var boundaryRoot = EnsureChild("BoundaryVisualsRoot", parent);
            var wallHeight = boundaryThickness * 0.9f;

            // Rebuilt 2026-09-20 after the third "outer edge still looks incorrect" report.
            //
            // The old construction had three separate faults that compounded into the ragged,
            // gear-toothed edge in the playtest screenshot:
            //
            // 1. Every segment was drawn with preserveAspect:false into a box of (edgeLength /
            //    tileCount) x wallHeight. The source crop is 320x109 (aspect 2.94) and the box was
            //    typically aspect 3.6, so each repeat was squashed by a different amount depending
            //    on the arena's size - and the Dynamic Arena Slice made arena sizes differ per
            //    stage, so each stage got its own distinct distortion.
            // 2. Each repeat was its own quad butted against the next, so every seam showed.
            // 3. The corner patches were drawn at 1.35x the wall height, so all four corners bulged
            //    proudly out of the band they were supposed to finish.
            //
            // The replacement is one tiled SpriteRenderer per edge. Unity's Tiled draw mode repeats
            // the sprite at its native size along the band instead of stretching it to fit, so the
            // art keeps its own aspect at any arena size and there are no per-quad seams. The
            // horizontal bands are extended by one wall height at each end so they cap the vertical
            // ones, which removes the corner pieces entirely rather than trying to make a mismatched
            // corner crop sit flush.
            var horizontalLength = arenaSize.x + wallHeight * 2f;

            CreateTiledBoundaryBand(boundaryRoot, boundaryTexture, boundaryTint, "BoundaryTop", new Vector2(0f, arenaSize.y * 0.5f), horizontalLength, wallHeight, 0f);
            CreateTiledBoundaryBand(boundaryRoot, boundaryTexture, boundaryTint, "BoundaryBottom", new Vector2(0f, -arenaSize.y * 0.5f), horizontalLength, wallHeight, 180f);
            CreateTiledBoundaryBand(boundaryRoot, boundaryTexture, boundaryTint, "BoundaryLeft", new Vector2(-arenaSize.x * 0.5f, 0f), arenaSize.y, wallHeight, 90f);
            CreateTiledBoundaryBand(boundaryRoot, boundaryTexture, boundaryTint, "BoundaryRight", new Vector2(arenaSize.x * 0.5f, 0f), arenaSize.y, wallHeight, 270f);
        }

        // One renderer per edge, tiled along its local X. The sprite is built at a pixels-per-unit
        // chosen so the crop's 109px height maps exactly to wallHeight, which is what keeps the
        // repeat unit at the art's own aspect instead of whatever the arena's dimensions imply.
        //
        // Tiled draw mode requires SpriteMeshType.FullRect; a Tight-meshed sprite silently falls
        // back to stretching, which is the bug this replaces. Scale stays at 1 because in Tiled
        // mode the renderer's size property does the work and any transform scale would multiply
        // the repeats back out of shape.
        private void CreateTiledBoundaryBand(
            Transform parent,
            Texture2D boundaryTexture,
            Color tint,
            string childName,
            Vector2 localPosition,
            float lengthAlongEdge,
            float wallHeight,
            float rotationDegrees)
        {
            if (boundaryTexture == null || lengthAlongEdge <= Mathf.Epsilon || wallHeight <= Mathf.Epsilon)
            {
                return;
            }

            var pixelsPerUnit = BoundaryWallPixelRect.height / wallHeight;
            var sprite = TrackSprite(Sprite.Create(
                boundaryTexture,
                BoundaryWallPixelRect,
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0u,
                SpriteMeshType.FullRect));

            var child = EnsureChild(childName, parent);
            child.localPosition = localPosition;
            child.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            child.localScale = Vector3.one;

            var spriteRenderer = child.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            spriteRenderer.tileMode = SpriteTileMode.Continuous;
            spriteRenderer.size = new Vector2(lengthAlongEdge, wallHeight);
            spriteRenderer.sortingOrder = -9;
            spriteRenderer.color = tint;
        }

        private void CreatePropClusters(Transform parent, StageThemeData stageThemeData, StageArenaLayoutData layoutData, Vector2 arenaSize)
        {
            var propTexture = ResolveTexture(
                _propTexture,
                stageThemeData != null ? stageThemeData.PropTexture : null,
                ref _runtimePropTexture,
                stageThemeData != null ? stageThemeData.PropTexturePath : null,
                PropTexturePath);

            if (propTexture == null)
            {
                return;
            }

            var propTint = stageThemeData != null ? stageThemeData.PropTint : Color.white;
            var propsRoot = EnsureChild("PropVisualsRoot", parent);

            // Was `stageThemeData.ThemeId == "theme_stage_02" ? Stage2PropPlacements : Stage1...`,
            // a stage-specific branch the Dynamic Arena Slice explicitly forbids ("Kein neuer
            // Code-Pfad ... ist stage-spezifisch verzweigt"). Prop layout now comes from the
            // stage's own layout asset; the Stage 1 static array is only the no-layout fallback.
            var placements = layoutData != null && layoutData.HasPropPlacements
                ? layoutData.PropPlacements
                : Stage1PropPlacements;

            for (var index = 0; index < placements.Length; index++)
            {
                var placement = placements[index];
                var sprite = CreatePropSprite(propTexture, placement.AtlasCell);
                var localPosition = ConstrainInsideArena(placement.LocalPosition, placement.TargetSize, arenaSize);

                CreateSpriteVisual(
                    propsRoot,
                    "StageProp_" + index,
                    sprite,
                    localPosition,
                    placement.TargetSize,
                    placement.SortingOrder,
                    propTint,
                    placement.AtlasCell.RotationDegrees);

                if (placement.BlocksMovement)
                {
                    EnsureObstacleCollider(propsRoot.Find("StageProp_" + index), placement.TargetSize * 0.82f);
                }
            }
        }

        private void CreateMazeBlockers(Transform parent, Texture2D propTexture, Color propTint, Vector2 arenaSize, HashSet<Vector2Int> openCells, System.Random random, StageArenaLayoutData layoutData)
        {
            if (propTexture == null)
            {
                return;
            }

            // Blocker sprites must be crops of whichever prop sheet this stage actually uses -
            // Stage 1's MazeBlockerCells coordinates are measured against stage1_prop_set.png and
            // produce garbled or empty crops on any differently-composed sheet. Reusing the
            // stage's own authored prop cells as the random pool keeps that correct for every
            // stage without naming any of them (the previous `useStage2PropAtlas` flag was exactly
            // the stage-specific branch the slice forbids).
            var blockerCellPool = layoutData != null && layoutData.HasPropPlacements
                ? System.Array.ConvertAll(layoutData.PropPlacements, placement => placement.AtlasCell)
                : MazeBlockerCells;

            var mazeRoot = EnsureChild("MazeBlockersRoot", parent);

            // Grid geometry is per-stage data now. The fallback numbers are Stage 1's original
            // hardcoded grid, so a stage without a layout asset builds exactly what it built before.
            var cellColumns = layoutData != null && layoutData.HasObstacleGrid
                ? layoutData.ObstacleGridColumns
                : new[] { -5f, -3f, -1f, 1f, 3f, 5f };
            var cellRows = layoutData != null && layoutData.HasObstacleGrid
                ? layoutData.ObstacleGridRows
                : new[] { 2f, 0f };
            var cellSize = layoutData != null ? layoutData.ObstacleCellSize : new Vector2(1.3f, 1.3f);

            var blockerIndex = 0;
            _walkableInteriorPositions.Clear();

            for (var row = 0; row < cellRows.Length; row++)
            {
                for (var column = 0; column < cellColumns.Length; column++)
                {
                    var cellPosition = new Vector2(cellColumns[column], cellRows[row]);

                    if (openCells.Contains(new Vector2Int(column, row)))
                    {
                        _walkableInteriorPositions.Add(ConstrainInsideArena(cellPosition, cellSize * 1.23f, arenaSize));
                        continue;
                    }

                    var atlasCell = blockerCellPool[random.Next(blockerCellPool.Length)];
                    var sprite = CreatePropSprite(propTexture, atlasCell);
                    var childName = "MazeBlocker_" + blockerIndex;
                    var localPosition = ConstrainInsideArena(cellPosition, cellSize, arenaSize);
                    CreateSpriteVisual(mazeRoot, childName, sprite, localPosition, cellSize, -7, propTint, atlasCell.RotationDegrees);
                    EnsureObstacleCollider(mazeRoot.Find(childName), cellSize);
                    blockerIndex += 1;
                }
            }
        }

        // UI proposal 3 (2026-09-21): "stage1_spawn_gate.png is placed but not in use." It was drawn
        // once, at StageThemeData.SpawnGateLocalPosition, while enemies actually came from
        // StageData.SpawnPoints - so the art said "enemies come from here" and they did not. One
        // gate is now drawn per spawn point (or at EnemySpawnOrigin for a stage with none), which
        // makes the gate true, and PulseSpawnGates makes it early.
        private void CreateSpawnGateVisual(Transform parent, StageThemeData stageThemeData)
        {
            _spawnGates.Clear();

            var spawnGateTexture = ResolveTexture(
                null,
                stageThemeData != null ? stageThemeData.SpawnGateTexture : null,
                ref _runtimeSpawnGateTexture,
                stageThemeData != null ? stageThemeData.SpawnGateTexturePath : null,
                SpawnGateTexturePath);

            if (spawnGateTexture == null)
            {
                return;
            }

            var worldSize = stageThemeData != null ? stageThemeData.SpawnGateWorldSize : new Vector2(3.2f, 1.8f);
            var tint = stageThemeData != null ? stageThemeData.SpawnGateTint : Color.white;
            var stageData = ResolveStageData();
            var spawnPoints = stageData != null ? stageData.SpawnPoints : null;
            var positions = new List<Vector2>();

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                for (var index = 0; index < spawnPoints.Length; index++)
                {
                    positions.Add(spawnPoints[index] != null ? spawnPoints[index].Position : Vector2.zero);
                }
            }
            else
            {
                positions.Add(stageData != null
                    ? stageData.EnemySpawnOrigin
                    : (stageThemeData != null ? stageThemeData.SpawnGateLocalPosition : new Vector2(0f, -5f)));
            }

            var sprite = TrackSprite(DebugSpriteFactory.CreateSprite(spawnGateTexture, 100f));

            for (var index = 0; index < positions.Count; index++)
            {
                var position = positions[index];

                // The gate art is drawn as a bottom-edge gate opening upward. Turning each gate so
                // it opens toward the arena centre keeps a side or top spawn point reading as a
                // door into the arena rather than a door lying on its side.
                var inward = position.sqrMagnitude > 0.0001f ? -position.normalized : Vector2.up;
                var rotation = Mathf.Atan2(inward.y, inward.x) * Mathf.Rad2Deg - 90f;
                var childName = "StageSpawnGate_" + index;
                CreateSpriteVisual(parent, childName, sprite, position, worldSize, -9, tint, rotation);
                var child = parent.Find(childName);

                if (child != null)
                {
                    var glow = CreateSpawnGateGlow(parent, "StageSpawnGateGlow_" + index, position, worldSize);
                    _spawnGates.Add(new SpawnGateVisual(child, child.GetComponent<SpriteRenderer>(), tint, child.localScale, glow));
                }
            }

            ApplySpawnGateIdleState();
        }

        public int SpawnGateCount => _spawnGates.Count;

        // The open maze cells, i.e. every interior position that is guaranteed not to be inside a
        // prop or blocker collider. Already used to place the Core; ArenaObjectiveService uses it
        // so an arena objective never lands inside a wall where it can be seen but not reached.
        public IReadOnlyList<Vector2> WalkableInteriorPositions => _walkableInteriorPositions;

        // Playtest check 2026-09-22: tinting the gate itself toward orange barely shows, because the
        // gate art is dark and a multiply tint on dark pixels stays dark. A soft light pool behind
        // the gate is what actually reads as "this door is about to open".
        private SpriteRenderer CreateSpawnGateGlow(Transform parent, string childName, Vector2 position, Vector2 gateSize)
        {
            if (_spawnGateGlowSprite == null)
            {
                _spawnGateGlowSprite = TrackSprite(CreateRadialGlowSprite(64));
            }

            var child = EnsureChild(childName, parent);
            child.localPosition = position;
            child.localRotation = Quaternion.identity;
            var renderer = child.GetComponent<SpriteRenderer>();

            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = _spawnGateGlowSprite;
            renderer.sortingOrder = -8;
            renderer.color = new Color(SpawnGatePulseColor.r, SpawnGatePulseColor.g, SpawnGatePulseColor.b, 0f);
            var diameter = Mathf.Max(gateSize.x, gateSize.y) * 1.5f;
            var bounds = _spawnGateGlowSprite.bounds.size;
            child.localScale = new Vector3(diameter / Mathf.Max(0.01f, bounds.x), diameter / Mathf.Max(0.01f, bounds.y), 1f);
            return renderer;
        }

        private static Sprite CreateRadialGlowSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var center = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                    var alpha = Mathf.Clamp01(1f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            texture.Apply();
            texture.hideFlags = HideFlags.DontSave;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Lights the gates the coming wave will use. WaveManager calls this at wave start with the
        // spawn-point indices WaveSpawnPlacement will actually round-robin through, for the length
        // of the wave-start spawn delay - so the tell always comes before the enemies, never with
        // them.
        public void PulseSpawnGates(IReadOnlyList<int> gateIndices, float durationSeconds)
        {
            for (var index = 0; index < _spawnGates.Count; index++)
            {
                _spawnGates[index].PulseRemaining = 0f;
                _spawnGates[index].Reset();
            }

            if (gateIndices == null)
            {
                return;
            }

            var duration = Mathf.Max(0.1f, durationSeconds);

            for (var index = 0; index < gateIndices.Count; index++)
            {
                var gateIndex = gateIndices[index];

                if (gateIndex >= 0 && gateIndex < _spawnGates.Count)
                {
                    _spawnGates[gateIndex].PulseRemaining = duration;
                    _spawnGates[gateIndex].PulseDuration = duration;
                }
            }
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            for (var index = 0; index < _spawnGates.Count; index++)
            {
                var gate = _spawnGates[index];

                if (gate.Transform == null || gate.Renderer == null || gate.PulseRemaining <= 0f)
                {
                    continue;
                }

                gate.PulseRemaining = Mathf.Max(0f, gate.PulseRemaining - deltaTime);

                if (gate.PulseRemaining <= 0f)
                {
                    gate.Reset();
                    continue;
                }

                // A few beats over the delay, strongest at the start and fading into the idle
                // state by the time the first enemy appears.
                var elapsed = gate.PulseDuration - gate.PulseRemaining;
                var envelope = gate.PulseRemaining / gate.PulseDuration;
                var beat = 0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI * 2f * SpawnGatePulseBeatsPerSecond);
                var strength = Mathf.Lerp(0.35f, 1f, beat) * envelope;
                gate.Renderer.color = Color.Lerp(gate.IdleColor, Color.white, strength * 0.6f);
                gate.Transform.localScale = gate.BaseScale * (1f + SpawnGatePulseScale * strength);

                if (gate.Glow != null)
                {
                    gate.Glow.color = new Color(SpawnGatePulseColor.r, SpawnGatePulseColor.g, SpawnGatePulseColor.b, 0.9f * strength);
                }
            }
        }

        // Idle gates are slightly dimmed so a lit gate reads as lit, not merely as the same gate.
        private void ApplySpawnGateIdleState()
        {
            for (var index = 0; index < _spawnGates.Count; index++)
            {
                var gate = _spawnGates[index];
                var idle = gate.BaseColor;
                idle.r *= SpawnGateIdleBrightness;
                idle.g *= SpawnGateIdleBrightness;
                idle.b *= SpawnGateIdleBrightness;
                gate.IdleColor = idle;
                gate.Reset();
            }
        }

        private StageData ResolveStageData()
        {
            var waveManager = FindFirstObjectByType<WaveManager>();

            if (waveManager != null && waveManager.StageDataAsset != null)
            {
                return waveManager.StageDataAsset;
            }

            return _stageData;
        }

        private sealed class SpawnGateVisual
        {
            public SpawnGateVisual(Transform transform, SpriteRenderer renderer, Color baseColor, Vector3 baseScale, SpriteRenderer glow)
            {
                Transform = transform;
                Renderer = renderer;
                Glow = glow;
                BaseColor = baseColor;
                IdleColor = baseColor;
                BaseScale = baseScale;
            }

            public Transform Transform { get; }

            public SpriteRenderer Renderer { get; }

            public SpriteRenderer Glow { get; }

            public Color BaseColor { get; }

            public Color IdleColor { get; set; }

            public Vector3 BaseScale { get; }

            public float PulseRemaining { get; set; }

            public float PulseDuration { get; set; } = 1f;

            public void Reset()
            {
                if (Renderer != null)
                {
                    Renderer.color = IdleColor;
                }

                if (Transform != null)
                {
                    Transform.localScale = BaseScale;
                }

                if (Glow != null)
                {
                    var color = Glow.color;
                    color.a = 0f;
                    Glow.color = color;
                }
            }
        }

        private void CreateAnchoredSprite(Transform parent, string childName, Texture2D texture, Vector2 localPosition, Vector2 targetSize, int sortingOrder, Color color)
        {
            CreateLayeredSprite(parent, childName, texture, targetSize, sortingOrder, color);
            var child = parent.Find(childName);

            if (child != null)
            {
                child.localPosition = localPosition;
            }
        }

        private void CreateSpriteVisual(Transform parent, string childName, Sprite sprite, Vector2 localPosition, Vector2 targetSize, int sortingOrder, Color color, float rotationDegrees = 0f, bool preserveAspect = true)
        {
            if (sprite == null)
            {
                return;
            }

            var child = EnsureChild(childName, parent);
            child.localPosition = localPosition;
            child.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            var spriteRenderer = child.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.color = color;

            if (preserveAspect)
            {
                DebugSpriteFactory.FitTransformToSprite(child, sprite, targetSize);
            }
            else
            {
                var bounds = sprite.bounds.size;

                if (bounds.x > Mathf.Epsilon && bounds.y > Mathf.Epsilon)
                {
                    child.localScale = new Vector3(targetSize.x / bounds.x, targetSize.y / bounds.y, 1f);
                }
            }
        }

        private static void CreateBoundaryVisual(string childName, Transform parent, Vector2 localPosition, Vector2 size, Color color)
        {
            var child = EnsureChild(childName, parent);
            child.localPosition = localPosition;
            var spriteRenderer = child.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = DebugSpriteFactory.CreateSquare(32f);
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = -10;
            child.localScale = new Vector3(size.x, size.y, 1f);
        }

        private void CreateBoundaryCollider(string childName, Transform parent, Vector2 localPosition, Vector2 size)
        {
            var child = EnsureChild(childName, parent);
            child.localPosition = localPosition;
            var collider = child.GetComponent<BoxCollider2D>();

            if (collider == null)
            {
                collider = child.gameObject.AddComponent<BoxCollider2D>();
            }

            collider.size = size;
            collider.isTrigger = false;
        }

        private void ApplyCoreVisual(Texture2D texture, Vector2 arenaSize, float coreScale, System.Random random)
        {
            if (texture == null)
            {
                return;
            }

            if (_coreTarget == null)
            {
                var coreObject = GameObject.Find(CombatHitReceiver.CoreAnchorObjectName);
                _coreTarget = coreObject != null ? coreObject.transform : null;
            }

            if (_coreTarget == null)
            {
                return;
            }

            var spriteRenderer = _coreTarget.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer = _coreTarget.gameObject.AddComponent<SpriteRenderer>();
            }

            var sprite = TrackSprite(DebugSpriteFactory.CreateSprite(texture, 100f));
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = -2;

            if (sprite != null)
            {
                var tileSize = Mathf.Min(arenaSize.x / 8f, arenaSize.y / 5f);
                DebugSpriteFactory.FitTransformToSprite(_coreTarget, sprite, Vector2.one * (tileSize * 0.34f * Mathf.Max(0.1f, coreScale)));
            }

            if (_walkableInteriorPositions.Count > 0)
            {
                _coreTarget.position = _walkableInteriorPositions[random.Next(_walkableInteriorPositions.Count)];
            }
        }

        private Sprite CreateAtlasSprite(Texture2D texture, AtlasCell atlasCell, int atlasRows, int atlasColumns)
        {
            if (texture == null)
            {
                return null;
            }

            var cellWidth = texture.width / (float)atlasColumns;
            var cellHeight = texture.height / (float)atlasRows;
            var x = (atlasCell.Column - 1) * cellWidth;
            var y = texture.height - atlasCell.Row * cellHeight;
            return TrackSprite(Sprite.Create(texture, new Rect(x, y, cellWidth, cellHeight), new Vector2(0.5f, 0.5f), 100f));
        }

        // For sheets that aren't a uniform grid (e.g. stage1_boundary_set.png, whose pieces have
        // varying pixel dimensions) - crops directly by an explicit pixel Rect instead of a
        // row/column division. The Rect already uses the same bottom-left-origin convention
        // Unity's own sprite-slice metadata uses, so values read from a texture's .meta apply
        // here unchanged.
        private Sprite CreateExplicitRectSprite(Texture2D texture, Rect pixelRect)
        {
            if (texture == null)
            {
                return null;
            }

            return TrackSprite(Sprite.Create(texture, pixelRect, new Vector2(0.5f, 0.5f), 100f));
        }

        // stage1_prop_set.png looks like a uniform PropAtlasRows x PropAtlasColumns grid at a
        // glance, but it's hand-composed like the boundary sheet: items vary in size and aren't
        // centered in equal-sized cells, so several items are wider/taller than their nominal cell
        // and bleed into the next one, while some cells sit empty. Dividing the texture rigidly
        // (CreateAtlasSprite) crops straight through those items or merges a neighbor's bleed into
        // the crop - confirmed directly by connected-component analysis of the texture's alpha
        // channel, which found 19 cleanly separated items whose actual pixel bounds don't line up
        // with the assumed 159x202 grid cells at all. This table maps each AtlasCell (Row, Column)
        // that Stage1PropPlacements/MazeBlockerCells actually reference to that item's real,
        // precisely measured pixel rect instead, the same explicit-rect approach already used for
        // the equally non-uniform boundary sheet (see CreateExplicitRectSprite/BoundaryWallPixelRect).
        private static readonly Dictionary<(int Row, int Column), Rect> PropPixelRects = new Dictionary<(int, int), Rect>
        {
            { (1, 1), new Rect(52f, 422f, 59f, 151f) },
            { (1, 2), new Rect(174f, 424f, 69f, 152f) },
            { (1, 3), new Rect(300f, 425f, 76f, 148f) },
            { (1, 4), new Rect(432f, 415f, 112f, 158f) },
            { (1, 5), new Rect(608f, 400f, 145f, 135f) },
            { (1, 6), new Rect(800f, 407f, 98f, 176f) },
            { (1, 7), new Rect(945f, 418f, 116f, 159f) },
            { (2, 2), new Rect(37f, 237f, 170f, 140f) },
            { (2, 4), new Rect(244f, 234f, 150f, 140f) },
            { (2, 5), new Rect(427f, 229f, 132f, 155f) },
            { (2, 6), new Rect(615f, 195f, 311f, 174f) },
            { (2, 7), new Rect(931f, 145f, 137f, 231f) },
            { (3, 1), new Rect(34f, 28f, 189f, 177f) },
            { (3, 2), new Rect(248f, 28f, 151f, 132f) },
            { (3, 4), new Rect(634f, 31f, 131f, 53f) },
            { (3, 5), new Rect(629f, 135f, 290f, 32f) },
            { (3, 6), new Rect(797f, 28f, 150f, 80f) },

            // stage2_prop_set.png (Stage2PropPlacements above) - 9 items, measured the same way:
            // connected-component analysis of the sheet's alpha channel, converted from image-space
            // (Y down from top) to Unity texture pixel-space (Y up from bottom) via
            // unity_y = image_height - (image_y + height). Row 20 is an otherwise-unused AtlasCell
            // row, chosen only to keep these keys distinct from Stage 1's (rows 1-3) in this shared
            // dictionary - it has no meaning beyond that.
            { (20, 1), new Rect(51f, 360f, 415f, 347f) },
            { (20, 2), new Rect(201f, 367f, 274f, 160f) },
            { (20, 3), new Rect(531f, 424f, 215f, 273f) },
            { (20, 4), new Rect(756f, 504f, 566f, 202f) },
            { (20, 5), new Rect(507f, 212f, 272f, 160f) },
            { (20, 6), new Rect(520f, 48f, 279f, 169f) },
            { (20, 7), new Rect(283f, 39f, 194f, 294f) },
            { (20, 8), new Rect(817f, 42f, 222f, 458f) },
            { (20, 9), new Rect(1067f, 41f, 220f, 456f) },

            // stage3/4/5_prop_set.png, measured 2026-09-20 by the same alpha connected-component
            // pass. Each sheet is another hand-composed collage with its own item count (6, 8 and
            // 7 usable pieces respectively), so like Stage 2 they cannot share Stage 1's grid
            // coordinates. Rows 30/40/50 keep the keys distinct in this shared dictionary and
            // carry no meaning beyond that. Without these, Stages 3-5 fell back to Stage 1's
            // coordinates and cropped unrelated or empty regions of their own sheets.
            { (30, 1), new Rect(34f, 35f, 547f, 287f) },
            { (30, 2), new Rect(55f, 352f, 433f, 348f) },
            { (30, 3), new Rect(447f, 380f, 236f, 286f) },
            { (30, 4), new Rect(683f, 33f, 233f, 209f) },
            { (30, 5), new Rect(709f, 359f, 592f, 337f) },
            { (30, 6), new Rect(961f, 31f, 359f, 314f) },

            { (40, 1), new Rect(44f, 524f, 378f, 184f) },
            { (40, 2), new Rect(50f, 62f, 390f, 236f) },
            { (40, 3), new Rect(55f, 339f, 306f, 170f) },
            { (40, 4), new Rect(463f, 396f, 198f, 312f) },
            { (40, 5), new Rect(496f, 49f, 172f, 231f) },
            { (40, 6), new Rect(732f, 506f, 380f, 196f) },
            { (40, 7), new Rect(761f, 44f, 174f, 216f) },
            { (40, 8), new Rect(1161f, 379f, 152f, 314f) },

            { (50, 1), new Rect(22f, 45f, 365f, 662f) },
            { (50, 2), new Rect(419f, 377f, 214f, 324f) },
            { (50, 3), new Rect(433f, 43f, 189f, 313f) },
            { (50, 4), new Rect(679f, 394f, 302f, 281f) },
            { (50, 5), new Rect(680f, 52f, 300f, 281f) },
            { (50, 6), new Rect(1034f, 400f, 282f, 286f) },
            { (50, 7), new Rect(1039f, 47f, 267f, 298f) },
        };

        private Sprite CreatePropSprite(Texture2D texture, AtlasCell atlasCell)
        {
            if (texture == null)
            {
                return null;
            }

            if (PropPixelRects.TryGetValue((atlasCell.Row, atlasCell.Column), out var pixelRect))
            {
                return CreateExplicitRectSprite(texture, pixelRect);
            }

            // Fallback for any (Row, Column) not in the measured table above (e.g. a stage theme
            // override using a differently laid-out prop texture) - the old rigid-grid crop is
            // wrong for THIS sheet but is still a reasonable guess for an arbitrary uniform one.
            return CreateAtlasSprite(texture, atlasCell, PropAtlasRows, PropAtlasColumns);
        }

        private static AtlasCell ResolveGroundCell(int row, int column, int layoutRows, int layoutColumns)
        {
            if (row == 0)
            {
                if (column == 0)
                {
                    return TopLeftCornerCell;
                }

                if (column == layoutColumns - 1)
                {
                    return TopRightCornerCell;
                }

                return TopEdgeCells[(column - 1) % TopEdgeCells.Length];
            }

            if (row == layoutRows - 1)
            {
                if (column == 0)
                {
                    return BottomLeftCornerCell;
                }

                if (column == layoutColumns - 1)
                {
                    return BottomRightCornerCell;
                }

                return BottomEdgeCells[(column - 1) % BottomEdgeCells.Length];
            }

            if (column == 0)
            {
                return LeftEdgeCells[(row - 1) % LeftEdgeCells.Length];
            }

            if (column == layoutColumns - 1)
            {
                return RightEdgeCells[(row - 1) % RightEdgeCells.Length];
            }

            var interiorIndex = Mathf.Abs(row * 11 + column * 7);

            if (row >= layoutRows - 2 && (column == 2 || column == layoutColumns - 3))
            {
                return InteriorDetailCells[interiorIndex % InteriorDetailCells.Length];
            }

            return InteriorCells[interiorIndex % InteriorCells.Length];
        }

        private static Vector2 ConstrainInsideArena(Vector2 localPosition, Vector2 targetSize, Vector2 arenaSize)
        {
            var halfArena = arenaSize * 0.5f;
            var halfProp = targetSize * 0.5f;
            return new Vector2(
                Mathf.Clamp(localPosition.x, -halfArena.x + halfProp.x, halfArena.x - halfProp.x),
                Mathf.Clamp(localPosition.y, -halfArena.y + halfProp.y, halfArena.y - halfProp.y));
        }

        private Sprite TrackSprite(Sprite sprite)
        {
            if (sprite != null)
            {
                sprite.hideFlags = HideFlags.DontSaveInEditor;
                _runtimeSprites.Add(sprite);
            }

            return sprite;
        }

        private void CleanupSprites()
        {
            for (var index = 0; index < _runtimeSprites.Count; index++)
            {
                var sprite = _runtimeSprites[index];

                if (sprite == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(sprite);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(sprite);
                }
            }

            _runtimeSprites.Clear();
        }

        private static Texture2D ResolveTexture(Texture2D assignedTexture, Texture2D themeTexture, ref Texture2D runtimeTexture, string projectRelativePath, string fallbackProjectRelativePath)
        {
            if (assignedTexture != null)
            {
                return assignedTexture;
            }

            if (themeTexture != null)
            {
                return themeTexture;
            }

            if (runtimeTexture != null)
            {
                return runtimeTexture;
            }

            runtimeTexture = DebugSpriteFactory.LoadTextureFromProjectFile(!string.IsNullOrWhiteSpace(projectRelativePath) ? projectRelativePath : fallbackProjectRelativePath);
            return runtimeTexture;
        }

        private StageThemeData ResolveStageThemeData()
        {
            // WaveManager._stageData is reassigned on stage transition (AdvanceToNextStage), so
            // WaveManager.StageDataAsset is the live, authoritative "what stage is this run on
            // right now" answer. This presenter's own _stageData field is a scene-authored
            // reference that's set once (to Stage 1) and never updated - checking it first meant
            // RebuildForNewStage() always rebuilt with Stage 1's ground/boundary/prop textures no
            // matter which stage WaveManager had actually advanced to ("stage two is not loaded as
            // background"). WaveManager is checked first now; _stageData is only the fallback for
            // when no WaveManager exists at all (e.g. an isolated arena-preview scene).
            var waveManager = FindFirstObjectByType<WaveManager>();

            if (waveManager != null && waveManager.StageDataAsset != null && waveManager.StageDataAsset.ThemeData != null)
            {
                return waveManager.StageDataAsset.ThemeData;
            }

            return _stageData != null ? _stageData.ThemeData : null;
        }

        private static Transform EnsureChild(string childName, Transform parent)
        {
            var child = parent.Find(childName);

            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(childName);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static Transform EnsureWorldRoot(string childName)
        {
            var rootObject = GameObject.Find(childName);

            if (rootObject != null)
            {
                return rootObject.transform;
            }

            return new GameObject(childName).transform;
        }

        private static void CleanupTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static HashSet<Vector2Int> BuildOpenMazeCells(StageArenaLayoutData layoutData, System.Random random)
        {
            // Stage 1's original grid dimensions and Maze strategy are the fallback, so a stage
            // with no layout asset carves exactly the same cells it always did.
            var strategy = layoutData != null ? layoutData.LayoutStrategy : ArenaLayoutStrategy.Maze;
            var columnCount = layoutData != null && layoutData.HasObstacleGrid ? layoutData.ObstacleGridColumns.Length : 6;
            var rowCount = layoutData != null && layoutData.HasObstacleGrid ? layoutData.ObstacleGridRows.Length : 2;

            return StageArenaMazeGenerator.BuildOpenCells(strategy, columnCount, rowCount, random);
        }

        private static void EnsureObstacleCollider(Transform targetTransform, Vector2 colliderSize)
        {
            if (targetTransform == null)
            {
                return;
            }

            var collider = targetTransform.GetComponent<BoxCollider2D>();

            if (collider == null)
            {
                collider = targetTransform.gameObject.AddComponent<BoxCollider2D>();
            }

            collider.size = colliderSize;
            collider.isTrigger = false;
        }
    }
}