using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Gameplay.Shared
{
    // Open Topics 4, owner decision (a), 2026-09-29 - the item called it "VFXSheetData"; half of the
    // six sheets are UI icons, so it is named for what it describes. One asset per multi-cell
    // sheet: where the file is, its grid, and what each cell is called. Before this the grid lived
    // in call arguments - thirteen projectile assets and nine weapon presentations each repeated
    // "4 x 4" or "3 x 3", and the two impact sheets and the tag icons kept theirs as constants in
    // code - so a re-exported sheet meant hunting every copy, and a wrong one sliced garbage
    // silently. Now a consumer names a cell and the sheet decides where it is.
    //
    // Cells are sliced at runtime from the raw file (DebugSpriteFactory), the accepted alternative
    // to Unity-sliced sprites that Graphics.md documents, so this works with the StreamingAssets
    // art path unchanged.
    [CreateAssetMenu(menuName = "Corebound/Art/Sprite Sheet", fileName = "SpriteSheet")]
    public sealed class SpriteSheetData : ScriptableObject
    {
        [Serializable]
        public sealed class Cell
        {
            [SerializeField] private string _name;
            [SerializeField] private int _column;
            [SerializeField] private int _row;

            // For a hand-composed sheet that is not a grid (projectile_sheet_weapons_02.png): the
            // cell's pixel rect, bottom-left origin. Used instead of column/row when it has a size.
            [SerializeField] private Rect _pixelRect;

            public string Name => _name;

            public int Column => _column;

            public int Row => _row;

            public Rect PixelRect => _pixelRect;

            public bool HasPixelRect => _pixelRect.width > 0f && _pixelRect.height > 0f;
        }

        [SerializeField] private string _sheetPath = string.Empty;
        [SerializeField] private int _columns = 1;
        [SerializeField] private int _rows = 1;

        // > 0: the row count is read from the image instead of _rows, as height / (cell width x
        // this). The tag icon sheet grows a row when new tags get art (Asset 108), and this lets
        // the new file work the moment it is dropped in.
        [SerializeField] private float _cellAspect;

        [SerializeField] private Cell[] _cells = Array.Empty<Cell>();

        public string SheetPath => _sheetPath;

        public int Columns => Mathf.Max(1, _columns);

        public int Rows
        {
            get
            {
                if (_cellAspect <= 0f)
                {
                    return Mathf.Max(1, _rows);
                }

                var size = DebugSpriteFactory.ResolveProjectAtlasSize(_sheetPath);

                if (size.x <= 0)
                {
                    return Mathf.Max(1, _rows);
                }

                var cellHeight = size.x / (float)Columns * _cellAspect;
                return Mathf.Max(1, Mathf.RoundToInt(size.y / cellHeight));
            }
        }

        public IReadOnlyList<Cell> Cells => _cells;

        public Sprite LoadCell(int column, int row, float pixelsPerUnit = 100f)
        {
            if (column < 0 || row < 0 || column >= Columns || row >= Rows)
            {
                return null;
            }

            return DebugSpriteFactory.LoadProjectAtlasCellSprite(_sheetPath, column, row, Columns, Rows, pixelsPerUnit);
        }

        // Row-major index: 0 is the top-left cell.
        public Sprite LoadCell(int index, float pixelsPerUnit = 100f)
        {
            return index < 0 ? null : LoadCell(index % Columns, index / Columns, pixelsPerUnit);
        }

        public Sprite LoadCell(string cellName, float pixelsPerUnit = 100f)
        {
            var cell = FindCell(cellName);

            if (cell == null)
            {
                return null;
            }

            return cell.HasPixelRect
                ? DebugSpriteFactory.LoadProjectSpriteRect(_sheetPath, cell.PixelRect, pixelsPerUnit)
                : LoadCell(cell.Column, cell.Row, pixelsPerUnit);
        }

        public bool HasCell(int index)
        {
            return index >= 0 && index < Columns * Rows;
        }

        // A whole row as an animation. Null rather than a part-filled array if any cell fails, so
        // the caller falls back to its still instead of flickering through holes.
        public Sprite[] LoadRow(int row)
        {
            var frames = new Sprite[Columns];

            for (var column = 0; column < frames.Length; column++)
            {
                frames[column] = LoadCell(column, row);

                if (frames[column] == null)
                {
                    return null;
                }
            }

            return frames;
        }

        public Cell FindCell(string cellName)
        {
            if (string.IsNullOrEmpty(cellName) || _cells == null)
            {
                return null;
            }

            for (var index = 0; index < _cells.Length; index++)
            {
                if (_cells[index] != null && string.Equals(_cells[index].Name, cellName, StringComparison.OrdinalIgnoreCase))
                {
                    return _cells[index];
                }
            }

            return null;
        }

        // For static callers with no serialized reference of their own (CardTagIconAtlas, the VFX
        // spawner's static entry points). The asset still has to be referenced by something in a
        // scene or it is not in the build: CardPoolData references the tag sheet and
        // CombatVfxSpawner the impact sheets, and both register what they hold here.
        private static readonly Dictionary<string, SpriteSheetData> Registry = new Dictionary<string, SpriteSheetData>();

        public static void Register(SpriteSheetData sheet)
        {
            if (sheet != null)
            {
                Registry[sheet.name] = sheet;
            }
        }

        public static SpriteSheetData Find(string assetName)
        {
            if (Registry.TryGetValue(assetName, out var sheet) && sheet != null)
            {
                return sheet;
            }

            // Loaded but not yet registered (a static call before its owner's OnEnable).
            foreach (var candidate in UnityEngine.Resources.FindObjectsOfTypeAll<SpriteSheetData>())
            {
                if (candidate != null && candidate.name == assetName)
                {
                    Registry[assetName] = candidate;
                    return candidate;
                }
            }

            return null;
        }
    }
}
