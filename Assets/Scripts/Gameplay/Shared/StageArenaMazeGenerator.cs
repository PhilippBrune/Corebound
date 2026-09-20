using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Gameplay.Shared
{
    // Extracted from StageArenaArtPresenter (God-object split, 2026-09-08): the open-cell layout
    // algorithm has no dependency on the presenter's rendering/collider/texture state, so it is a
    // plain, Unity-object-free generator that can be unit tested without a MonoBehaviour or scene.
    //
    // 2026-09-20 (Dynamic Arena Slice): generalised from "Stage 1's hardcoded 6x2 carve" to a set
    // of strategies evaluated against whatever grid a StageArenaLayoutData authored. The Maze
    // strategy on a 6-column, 2-row grid reproduces the original output cell-for-cell, including
    // the order of the two System.Random draws, so Stage 1 is unchanged - that is the slice's
    // first Acceptance Criterion.
    internal static class StageArenaMazeGenerator
    {
        public static HashSet<Vector2Int> BuildOpenCells(
            ArenaLayoutStrategy strategy,
            int columnCount,
            int rowCount,
            System.Random random)
        {
            var openCells = new HashSet<Vector2Int>();

            if (columnCount <= 0 || rowCount <= 0)
            {
                return openCells;
            }

            switch (strategy)
            {
                case ArenaLayoutStrategy.OpenField:
                    BuildOpenField(openCells, columnCount, rowCount, random);
                    break;
                case ArenaLayoutStrategy.Corridors:
                    BuildCorridors(openCells, columnCount, rowCount, random);
                    break;
                case ArenaLayoutStrategy.Perimeter:
                    BuildPerimeter(openCells, columnCount, rowCount);
                    break;
                default:
                    BuildMaze(openCells, columnCount, rowCount, random);
                    break;
            }

            return openCells;
        }

        // A guaranteed central corridor plus one randomly chosen passage on each flank. On a 6x2
        // grid the centre columns are 2 and 3, the left flank resolves to {1} or {0,1} on row 0,
        // and the right flank to {4} or {4,5} on row 0 - exactly Stage 1's original literal set.
        private static void BuildMaze(HashSet<Vector2Int> openCells, int columnCount, int rowCount, System.Random random)
        {
            var centreLeft = Mathf.Clamp((columnCount - 1) / 2, 0, columnCount - 1);
            var centreRight = Mathf.Clamp(columnCount / 2, 0, columnCount - 1);

            OpenColumn(openCells, centreLeft, rowCount);

            if (centreRight != centreLeft)
            {
                OpenColumn(openCells, centreRight, rowCount);
            }

            // Draw order matters: two NextDouble() calls, left flank first, same as the original.
            if (random.NextDouble() >= 0.5d)
            {
                OpenColumn(openCells, centreLeft - 1, rowCount);
            }
            else
            {
                OpenCell(openCells, centreLeft - 2, 0, columnCount, rowCount);
                OpenCell(openCells, centreLeft - 1, 0, columnCount, rowCount);
            }

            if (random.NextDouble() >= 0.5d)
            {
                OpenColumn(openCells, centreRight + 1, rowCount);
            }
            else
            {
                OpenCell(openCells, centreRight + 2, 0, columnCount, rowCount);
                OpenCell(openCells, centreRight + 1, 0, columnCount, rowCount);
            }
        }

        // Most of the grid walkable with a sparse scattering of blockers - reads as an open field
        // with cover rather than a corridor system.
        private static void BuildOpenField(HashSet<Vector2Int> openCells, int columnCount, int rowCount, System.Random random)
        {
            for (var column = 0; column < columnCount; column++)
            {
                for (var row = 0; row < rowCount; row++)
                {
                    // Roughly a quarter of cells stay blocked, and never two in a row on the same
                    // line, so the field never closes into an accidental wall.
                    var keepBlocked = random.NextDouble() < 0.25d && !openCells.Contains(new Vector2Int(column - 1, row));

                    if (!keepBlocked)
                    {
                        openCells.Add(new Vector2Int(column, row));
                    }
                }
            }
        }

        // Diagonal lanes: a cell is open when its column and row indices line up on one of several
        // offset diagonals, leaving wedge-shaped blocked pockets between the lanes.
        private static void BuildCorridors(HashSet<Vector2Int> openCells, int columnCount, int rowCount, System.Random random)
        {
            var laneOffset = random.Next(0, Mathf.Max(1, rowCount));

            for (var column = 0; column < columnCount; column++)
            {
                for (var row = 0; row < rowCount; row++)
                {
                    var diagonal = (column + row + laneOffset) % 3;

                    if (diagonal != 2)
                    {
                        openCells.Add(new Vector2Int(column, row));
                    }
                }
            }

            // Guarantee the grid is always crossable even on very small grids, where the modulo
            // pattern could otherwise seal a row.
            for (var column = 0; column < columnCount; column++)
            {
                openCells.Add(new Vector2Int(column, rowCount - 1));
            }
        }

        // Walkable middle, blocked outer ring. On a grid only one or two rows deep every row is an
        // edge row, so the ring collapses to the first and last columns only - which still reads
        // as intended rather than sealing the arena.
        private static void BuildPerimeter(HashSet<Vector2Int> openCells, int columnCount, int rowCount)
        {
            for (var column = 0; column < columnCount; column++)
            {
                for (var row = 0; row < rowCount; row++)
                {
                    var isEdgeColumn = column == 0 || column == columnCount - 1;
                    var isEdgeRow = rowCount > 2 && (row == 0 || row == rowCount - 1);

                    if (!isEdgeColumn && !isEdgeRow)
                    {
                        openCells.Add(new Vector2Int(column, row));
                    }
                }
            }
        }

        private static void OpenColumn(HashSet<Vector2Int> openCells, int column, int rowCount)
        {
            if (column < 0)
            {
                return;
            }

            for (var row = 0; row < rowCount; row++)
            {
                openCells.Add(new Vector2Int(column, row));
            }
        }

        private static void OpenCell(HashSet<Vector2Int> openCells, int column, int row, int columnCount, int rowCount)
        {
            if (column < 0 || column >= columnCount || row < 0 || row >= rowCount)
            {
                return;
            }

            openCells.Add(new Vector2Int(column, row));
        }
    }
}
