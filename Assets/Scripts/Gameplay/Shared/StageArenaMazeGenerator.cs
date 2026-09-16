using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Gameplay.Shared
{
    // Extracted from StageArenaArtPresenter (God-object split, 2026-09-08): the maze open-cell
    // layout algorithm has no dependency on the presenter's rendering/collider/texture state, so
    // it is moved out as a plain, Unity-object-free generator that can be unit tested without a
    // MonoBehaviour or scene.
    internal static class StageArenaMazeGenerator
    {
        public static HashSet<Vector2Int> BuildOpenCells(System.Random random)
        {
            var openCells = new HashSet<Vector2Int>();
            openCells.Add(new Vector2Int(2, 1));
            openCells.Add(new Vector2Int(3, 1));
            openCells.Add(new Vector2Int(2, 0));
            openCells.Add(new Vector2Int(3, 0));

            if (random.NextDouble() >= 0.5d)
            {
                openCells.Add(new Vector2Int(1, 1));
                openCells.Add(new Vector2Int(1, 0));
            }
            else
            {
                openCells.Add(new Vector2Int(0, 0));
                openCells.Add(new Vector2Int(1, 0));
            }

            if (random.NextDouble() >= 0.5d)
            {
                openCells.Add(new Vector2Int(4, 1));
                openCells.Add(new Vector2Int(4, 0));
            }
            else
            {
                openCells.Add(new Vector2Int(5, 0));
                openCells.Add(new Vector2Int(4, 0));
            }

            return openCells;
        }
    }
}
