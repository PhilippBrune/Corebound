using System;
using UnityEngine;

namespace Corebound.Gameplay.Shared
{
    // How an arena's interior obstacle grid is carved. Answers Open Question 3 of
    // Docs/Slices/15_DYNAMIC_ARENA_SLICE.md the way that question recommends: Stage 1's existing,
    // working algorithm stays as one strategy among several rather than being rewritten, and a
    // stage picks its shape from data. Adding a shape here must never require a stage-specific
    // branch in StageArenaArtPresenter - that is an explicit Acceptance Criterion of the slice.
    public enum ArenaLayoutStrategy
    {
        // Stage 1's original carve: a guaranteed central corridor plus one randomly-chosen side
        // passage on each flank. Everything else becomes a blocker.
        Maze = 0,

        // Mostly walkable with sparse scattered blockers - open, readable, low cover.
        OpenField = 1,

        // Diagonal lanes across the grid, leaving wedge-shaped blocked pockets between them.
        Corridors = 2,

        // Interior walkable, the outermost ring of cells blocked - a bowl with a clear middle.
        Perimeter = 3
    }

    // Serializable twin of the atlas coordinate the arena art presenter uses internally. Lives here
    // rather than nested inside the presenter so prop layouts can be authored per stage in an asset
    // instead of being compiled into static arrays.
    [Serializable]
    public struct ArenaAtlasCell
    {
        [SerializeField] private int _row;
        [SerializeField] private int _column;
        [SerializeField] private float _rotationDegrees;

        public ArenaAtlasCell(int row, int column, float rotationDegrees = 0f)
        {
            _row = Mathf.Max(1, row);
            _column = Mathf.Max(1, column);
            _rotationDegrees = rotationDegrees;
        }

        public int Row => Mathf.Max(1, _row);

        public int Column => Mathf.Max(1, _column);

        public float RotationDegrees => _rotationDegrees;
    }

    [Serializable]
    public struct ArenaPropPlacement
    {
        [SerializeField] private ArenaAtlasCell _atlasCell;
        [SerializeField] private Vector2 _localPosition;
        [SerializeField] private Vector2 _targetSize;
        [SerializeField] private int _sortingOrder;
        [SerializeField] private bool _blocksMovement;

        public ArenaPropPlacement(ArenaAtlasCell atlasCell, Vector2 localPosition, Vector2 targetSize, int sortingOrder, bool blocksMovement)
        {
            _atlasCell = atlasCell;
            _localPosition = localPosition;
            _targetSize = targetSize;
            _sortingOrder = sortingOrder;
            _blocksMovement = blocksMovement;
        }

        public ArenaAtlasCell AtlasCell => _atlasCell;

        public Vector2 LocalPosition => _localPosition;

        public Vector2 TargetSize => _targetSize;

        public int SortingOrder => _sortingOrder;

        public bool BlocksMovement => _blocksMovement;
    }

    // Rect-based rather than polygonal, per the slice's own Open Question 1 ("mit Rects starten,
    // auf Polygone erweitern, sobald ein konkretes Stage-Layout das erzwingt").
    [Serializable]
    public struct ArenaBuildableZone
    {
        [SerializeField] private Vector2 _center;
        [SerializeField] private Vector2 _size;
        [SerializeField] private int _slotCount;

        public ArenaBuildableZone(Vector2 center, Vector2 size, int slotCount)
        {
            _center = center;
            _size = size;
            _slotCount = Mathf.Max(1, slotCount);
        }

        public Vector2 Center => _center;

        public Vector2 Size => _size;

        public int SlotCount => Mathf.Max(1, _slotCount);

        // Distributes this zone's slots evenly along its widest axis, inset by a quarter of the
        // spacing so no slot ends up flush against the zone edge. A single-slot zone sits at the
        // centre. This is the MVP placement rule the slice asks for - deliberately not a
        // constraint solver.
        public Vector2 ResolveSlotPosition(int slotIndex)
        {
            var count = SlotCount;

            if (count <= 1)
            {
                return _center;
            }

            var index = Mathf.Clamp(slotIndex, 0, count - 1);
            var t = (index + 0.5f) / count;
            var isHorizontal = Mathf.Abs(_size.x) >= Mathf.Abs(_size.y);

            if (isHorizontal)
            {
                return new Vector2(_center.x - _size.x * 0.5f + _size.x * t, _center.y);
            }

            return new Vector2(_center.x, _center.y - _size.y * 0.5f + _size.y * t);
        }
    }

    // The per-stage arena layout asset proposed by Docs/Slices/15_DYNAMIC_ARENA_SLICE.md. Before
    // this existed, arena *size* was the only per-stage value; shape, prop placement and tower
    // slots all came from Stage-1-only static arrays in StageArenaArtPresenter, so every stage
    // rendered Stage 1's geometry stretched to a different size ("Arena still is a fix in size").
    //
    // Every field is optional: a stage with no layout asset, or with an unauthored array, falls
    // back to exactly the behaviour that shipped before, so Stage 1 needed no change.
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Stages/Stage Arena Layout Data", fileName = "StageArenaLayoutData")]
    public sealed class StageArenaLayoutData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _layoutId = "layout_stage_01";

        [Header("Arena bounds")]
        [SerializeField] private Vector2 _arenaSize = new Vector2(16f, 10f);
        [SerializeField] private float _boundaryThickness = 0.8f;

        [Header("Interior obstacle grid")]
        [SerializeField] private ArenaLayoutStrategy _layoutStrategy = ArenaLayoutStrategy.Maze;

        // World-space X positions of each obstacle-grid column and Y of each row. Stage 1's
        // original hardcoded grid was columns {-5,-3,-1,1,3,5} and rows {2,0}; keeping those exact
        // numbers in the Stage 1 asset is what makes its layout bit-identical to before.
        [SerializeField] private float[] _obstacleGridColumns = { -5f, -3f, -1f, 1f, 3f, 5f };
        [SerializeField] private float[] _obstacleGridRows = { 2f, 0f };
        [SerializeField] private Vector2 _obstacleCellSize = new Vector2(1.3f, 1.3f);

        [Header("Placements")]
        [SerializeField] private ArenaPropPlacement[] _propPlacements;
        [SerializeField] private ArenaBuildableZone[] _buildableZones;

        public string LayoutId => _layoutId;

        public Vector2 ArenaSize => _arenaSize;

        public float BoundaryThickness => Mathf.Max(0.05f, _boundaryThickness);

        public ArenaLayoutStrategy LayoutStrategy => _layoutStrategy;

        public float[] ObstacleGridColumns => _obstacleGridColumns;

        public float[] ObstacleGridRows => _obstacleGridRows;

        public Vector2 ObstacleCellSize => _obstacleCellSize;

        public ArenaPropPlacement[] PropPlacements => _propPlacements;

        public ArenaBuildableZone[] BuildableZones => _buildableZones;

        public bool HasPropPlacements => _propPlacements != null && _propPlacements.Length > 0;

        public bool HasBuildableZones => _buildableZones != null && _buildableZones.Length > 0;

        public bool HasObstacleGrid =>
            _obstacleGridColumns != null && _obstacleGridColumns.Length > 0 &&
            _obstacleGridRows != null && _obstacleGridRows.Length > 0;

        // Flattens the authored zones into the plain point list TowerService already consumes, so
        // the slot-creation path itself is unchanged - it just gets its points from data now.
        public Vector2[] ResolveTowerSlotPositions()
        {
            if (!HasBuildableZones)
            {
                return null;
            }

            var total = 0;

            for (var zoneIndex = 0; zoneIndex < _buildableZones.Length; zoneIndex++)
            {
                total += _buildableZones[zoneIndex].SlotCount;
            }

            var positions = new Vector2[total];
            var writeIndex = 0;

            for (var zoneIndex = 0; zoneIndex < _buildableZones.Length; zoneIndex++)
            {
                var zone = _buildableZones[zoneIndex];

                for (var slotIndex = 0; slotIndex < zone.SlotCount; slotIndex++)
                {
                    positions[writeIndex++] = zone.ResolveSlotPosition(slotIndex);
                }
            }

            return positions;
        }
    }
}
