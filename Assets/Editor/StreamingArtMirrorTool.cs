using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corebound.Gameplay.Waves;
using UnityEditor;
using UnityEngine;

namespace Corebound.EditorTools
{
    // Runtime art is read as raw bytes by DebugSpriteFactory, which tries
    // Assets/StreamingAssets/Art/ first and only falls back to Assets/Art/ inside the Editor.
    // That fallback means a missing mirror entry is completely invisible while testing in the
    // Editor and only shows up as missing art in a real build - which is exactly how Stage 2-5
    // arena textures, the per-stage enemy reskins, all four extra boss sprites, the Mortar and
    // Sentinel tower sprites and seven VFX sheets ended up unmirrored (39 files, found 2026-09-19).
    // The same fallback also hid a *stale* mirror entry: card_reward_choice_panel.png had been
    // replaced under Assets/Art/ but the mirror still held the old image, so the game kept
    // rendering the previous reward panel art.
    //
    // Keeping the mirror correct by hand has demonstrably not worked, so this does it mechanically.
    [InitializeOnLoad]
    public static class StreamingArtMirrorTool
    {
        private const string SourceRoot = "Assets/Art";

        // Open Topics 4 (2026-09-30): the menu item ends in a modal dialog, which nothing but a
        // person can dismiss. Writing "validate" or "sync" into this file in the project root runs
        // the same checks from the Editor's update loop and writes the report to the log
        // ([ArtPipeline]); "sync" also copies the mirror. The file is emptied once handled.
        private const string RequestFilePath = "ArtPipelineRequest.txt";
        private static double _nextPoll;

        static StreamingArtMirrorTool()
        {
            EditorApplication.update -= PollRequestFile;
            EditorApplication.update += PollRequestFile;
        }

        private static void PollRequestFile()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < _nextPoll)
            {
                return;
            }

            _nextPoll = EditorApplication.timeSinceStartup + 1.0;

            if (!File.Exists(RequestFilePath))
            {
                return;
            }

            string command;

            try
            {
                command = File.ReadAllText(RequestFilePath).Trim().ToLowerInvariant();
                File.WriteAllText(RequestFilePath, string.Empty);
            }
            catch (IOException)
            {
                return;
            }

            if (command != "validate" && command != "sync")
            {
                return;
            }

            if (command == "sync")
            {
                Debug.Log("[ArtPipeline] " + SyncMirror(applyChanges: true));
                AssetDatabase.Refresh();
            }

            Debug.Log("[ArtPipeline] Validation report (request file):\n" + BuildReport());
        }

        private static string BuildReport()
        {
            return SyncMirror(applyChanges: false) + "\n\n" + ValidateStageThemePaths() + "\n\n" + ValidateSpriteSheets() + "\n\n" + ValidateArtContent();
        }
        private const string MirrorRoot = "Assets/StreamingAssets/Art";

        private static readonly string[] MirroredExtensions = { ".png", ".jpg", ".jpeg" };

        // Backups/working files the author keeps beside the real art. They are never loaded at
        // runtime, so mirroring them would just add dead weight to every build.
        private static readonly string[] IgnoredPrefixes = { "be_" };

        [MenuItem("Corebound/Validate Art Pipeline")]
        public static void ValidateArtPipeline()
        {
            var mirrorReport = SyncMirror(applyChanges: false);
            var pathReport = ValidateStageThemePaths();
            var sheetReport = ValidateSpriteSheets();
            var contentReport = ValidateArtContent();

            var message = mirrorReport + "\n\n" + pathReport + "\n\n" + sheetReport + "\n\n" + contentReport;
            Debug.Log("[ArtPipeline] Validation report:\n" + message);

            if (EditorUtility.DisplayDialog("Corebound - Art Pipeline", message, "Sync Mirror Now", "Close"))
            {
                Debug.Log("[ArtPipeline] " + SyncMirror(applyChanges: true));
                AssetDatabase.Refresh();
            }
        }

        // Graphics.md, "Abnahmekriterien für geliefertes Artwork" (2026-09-22). Four assets so far
        // arrived with a checkerboard painted *into* the pixels instead of real transparency, and
        // one was a diagram rather than the frame that was ordered. A path check cannot see any of
        // that, so this looks at the pixels: fully opaque corners on something that should be cut
        // out, and the two-tone checker pattern itself.
        //
        // Naming convention decides what is expected, which keeps the check data-free: a file whose
        // name ends in _frame, _icon, _badge, _pips or _indicator must have transparent corners, and
        // a *_frame must also have a transparent centre.
        private static string ValidateArtContent()
        {
            var suspects = new List<string>();
            var checkedCount = 0;

            foreach (var sourcePath in EnumerateMirroredSourceFiles())
            {
                var name = Path.GetFileNameWithoutExtension(sourcePath).ToLowerInvariant();
                var isFrame = name.EndsWith("_frame");
                var isCutout = isFrame || name.EndsWith("_icon") || name.EndsWith("_badge")
                    || name.EndsWith("_pips") || name.EndsWith("_indicator");

                if (!isCutout)
                {
                    continue;
                }

                // The raw file, the way the game itself reads art (DebugSpriteFactory), so the check
                // works without Read/Write on the import - 16 cut-out assets used to be skipped as
                // "cannot be read".
                var texture = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadTextureFromProjectFile(sourcePath);

                if (texture == null)
                {
                    suspects.Add(sourcePath + " - cannot be decoded");
                    continue;
                }

                checkedCount++;
                var width = texture.width;
                var height = texture.height;
                var cornerAlpha = Mathf.Max(
                    Mathf.Max(texture.GetPixel(1, 1).a, texture.GetPixel(width - 2, 1).a),
                    Mathf.Max(texture.GetPixel(1, height - 2).a, texture.GetPixel(width - 2, height - 2).a));

                if (cornerAlpha > 0.9f)
                {
                    suspects.Add(sourcePath + " - opaque corners: expected a cut-out shape");
                }

                if (isFrame && texture.GetPixel(width / 2, height / 2).a > 0.9f)
                {
                    suspects.Add(sourcePath + " - opaque centre: a frame must be see-through");
                }

                if (LooksLikeCheckerboard(texture))
                {
                    suspects.Add(sourcePath + " - checkerboard baked into the pixels (fake transparency)");
                }

                Object.DestroyImmediate(texture);
            }

            if (suspects.Count == 0)
            {
                return "Art content: " + checkedCount + " cut-out assets checked, none suspicious.";
            }

            return "Art content: " + suspects.Count + " suspicious asset(s) of " + checkedCount + " checked:\n - "
                + string.Join("\n - ", suspects);
        }

        // The classic grey-on-grey transparency checker: two near-grey tones alternating in blocks.
        // Sampled on a coarse grid, so it costs nothing even on a large sheet.
        private static bool LooksLikeCheckerboard(Texture2D texture)
        {
            const int Samples = 24;
            var toneA = 0;
            var toneB = 0;
            var other = 0;

            for (var y = 0; y < Samples; y++)
            {
                for (var x = 0; x < Samples; x++)
                {
                    var pixel = texture.GetPixel(
                        Mathf.Clamp(x * texture.width / Samples, 0, texture.width - 1),
                        Mathf.Clamp(y * texture.height / Samples, 0, texture.height - 1));

                    if (pixel.a < 0.9f)
                    {
                        other++;
                        continue;
                    }

                    var isGrey = Mathf.Abs(pixel.r - pixel.g) < 0.05f && Mathf.Abs(pixel.g - pixel.b) < 0.05f;

                    if (!isGrey)
                    {
                        other++;
                    }
                    else if (pixel.r > 0.92f)
                    {
                        toneA++;
                    }
                    else if (pixel.r > 0.68f && pixel.r < 0.88f)
                    {
                        toneB++;
                    }
                    else
                    {
                        other++;
                    }
                }
            }

            var total = Samples * Samples;

            // Both tones present in quantity, and almost nothing else: that is a checker, not art.
            return toneA > total / 6 && toneB > total / 6 && other < total / 3;
        }

        [MenuItem("Corebound/Sync StreamingAssets Art Mirror")]
        public static void SyncMirrorMenu()
        {
            Debug.Log("[ArtPipeline] " + SyncMirror(applyChanges: true));
            AssetDatabase.Refresh();
        }

        private static string SyncMirror(bool applyChanges)
        {
            if (!Directory.Exists(SourceRoot))
            {
                return "Source folder " + SourceRoot + " does not exist.";
            }

            var missing = new List<string>();
            var stale = new List<string>();

            foreach (var sourcePath in EnumerateMirroredSourceFiles())
            {
                var relativePath = sourcePath.Substring(SourceRoot.Length + 1);
                var mirrorPath = Path.Combine(MirrorRoot, relativePath);

                if (!File.Exists(mirrorPath))
                {
                    missing.Add(relativePath);
                }
                else if (!FilesHaveSameBytes(sourcePath, mirrorPath))
                {
                    stale.Add(relativePath);
                }

                if (!applyChanges)
                {
                    continue;
                }

                var mirrorDirectory = Path.GetDirectoryName(mirrorPath);

                if (!string.IsNullOrEmpty(mirrorDirectory) && !Directory.Exists(mirrorDirectory))
                {
                    Directory.CreateDirectory(mirrorDirectory);
                }

                if (!File.Exists(mirrorPath) || !FilesHaveSameBytes(sourcePath, mirrorPath))
                {
                    File.Copy(sourcePath, mirrorPath, true);
                }
            }

            if (missing.Count == 0 && stale.Count == 0)
            {
                return "StreamingAssets mirror is up to date.";
            }

            var verb = applyChanges ? "Copied" : "Would copy";
            return verb + " " + missing.Count + " missing and " + stale.Count + " stale file(s)."
                + FormatList("Missing", missing)
                + FormatList("Stale", stale);
        }

        private static IEnumerable<string> EnumerateMirroredSourceFiles()
        {
            return Directory
                .EnumerateFiles(SourceRoot, "*.*", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .Where(path => MirroredExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
                .Where(path => !IgnoredPrefixes.Any(prefix => Path.GetFileName(path).StartsWith(prefix)));
        }

        private static bool FilesHaveSameBytes(string leftPath, string rightPath)
        {
            var leftInfo = new FileInfo(leftPath);
            var rightInfo = new FileInfo(rightPath);

            if (leftInfo.Length != rightInfo.Length)
            {
                return false;
            }

            return File.ReadAllBytes(leftPath).SequenceEqual(File.ReadAllBytes(rightPath));
        }

        // StageThemeData stores its textures as plain strings, so a renamed or mistyped file is a
        // silent null at runtime rather than a broken reference the Editor would flag. Stage 2 and
        // Stage 3 both pointed at a ".png.jpg" name that no longer existed after the files were
        // renamed, which is why those arenas rendered with no ground at all.
        private static string ValidateStageThemePaths()
        {
            var brokenEntries = new List<string>();
            var checkedCount = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:StageThemeData"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var theme = AssetDatabase.LoadAssetAtPath<StageThemeData>(assetPath);

                if (theme == null)
                {
                    continue;
                }

                var candidates = new (string Label, string Path)[]
                {
                    ("Ground", theme.GroundTexturePath),
                    ("Boundary", theme.BoundaryTexturePath),
                    ("Prop", theme.PropTexturePath),
                    ("Core", theme.CoreTexturePath),
                    ("SpawnGate", theme.SpawnGateTexturePath)
                };

                foreach (var candidate in candidates)
                {
                    if (string.IsNullOrWhiteSpace(candidate.Path))
                    {
                        continue;
                    }

                    checkedCount++;

                    if (!File.Exists(candidate.Path))
                    {
                        brokenEntries.Add(theme.name + "." + candidate.Label + " -> " + candidate.Path);
                    }
                }
            }

            if (brokenEntries.Count == 0)
            {
                return "StageThemeData texture paths: all " + checkedCount + " resolve.";
            }

            return "StageThemeData texture paths: " + brokenEntries.Count + " of " + checkedCount + " BROKEN."
                + FormatList("Broken", brokenEntries);
        }

        // SpriteSheetData (2026-09-30) is where every multi-cell sheet's grid lives, and its path is
        // a plain string like StageThemeData's. A missing file, an image that does not divide into
        // the grid, or a named pixel rect outside the image would all render as a blank or garbled
        // cell with no error, so each is reported here.
        private static string ValidateSpriteSheets()
        {
            var problems = new List<string>();
            var notes = new List<string>();
            var checkedCount = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:SpriteSheetData"))
            {
                var sheet = AssetDatabase.LoadAssetAtPath<Corebound.Gameplay.Shared.SpriteSheetData>(AssetDatabase.GUIDToAssetPath(guid));

                if (sheet == null)
                {
                    continue;
                }

                checkedCount++;

                if (string.IsNullOrWhiteSpace(sheet.SheetPath) || !File.Exists(sheet.SheetPath))
                {
                    problems.Add(sheet.name + " -> missing file " + sheet.SheetPath);
                    continue;
                }

                var size = Corebound.Gameplay.Shared.DebugSpriteFactory.ResolveProjectAtlasSize(sheet.SheetPath);

                // A note, not a problem: the cell edges land between pixels, which can bleed a
                // one-pixel sliver of the neighbouring cell. Two delivered sheets do this and look fine.
                if (size.x % sheet.Columns != 0 || size.y % sheet.Rows != 0)
                {
                    notes.Add(sheet.name + " -> " + size.x + "x" + size.y + " does not divide evenly into " + sheet.Columns + "x" + sheet.Rows);
                }

                foreach (var cell in sheet.Cells)
                {
                    if (cell == null)
                    {
                        continue;
                    }

                    var outside = cell.HasPixelRect
                        ? cell.PixelRect.xMax > size.x || cell.PixelRect.yMax > size.y || cell.PixelRect.xMin < 0f || cell.PixelRect.yMin < 0f
                        : cell.Column >= sheet.Columns || cell.Row >= sheet.Rows;

                    if (outside)
                    {
                        problems.Add(sheet.name + "." + cell.Name + " -> outside the sheet");
                    }
                }
            }

            if (problems.Count == 0)
            {
                return "Sprite sheets: all " + checkedCount + " resolve and fit their images." + FormatList("Notes", notes);
            }

            return "Sprite sheets: " + problems.Count + " problem(s) in " + checkedCount + " sheets." + FormatList("Problems", problems) + FormatList("Notes", notes);
        }

        private static string FormatList(string header, List<string> entries)
        {
            if (entries.Count == 0)
            {
                return string.Empty;
            }

            var shown = entries.Take(12).Select(entry => "\n  - " + entry);
            var suffix = entries.Count > 12 ? "\n  - ...and " + (entries.Count - 12) + " more" : string.Empty;
            return "\n" + header + ":" + string.Concat(shown) + suffix;
        }
    }
}
