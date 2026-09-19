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
    public static class StreamingArtMirrorTool
    {
        private const string SourceRoot = "Assets/Art";
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

            var message = mirrorReport + "\n\n" + pathReport;
            Debug.Log("[ArtPipeline] Validation report:\n" + message);

            if (EditorUtility.DisplayDialog("Corebound - Art Pipeline", message, "Sync Mirror Now", "Close"))
            {
                Debug.Log("[ArtPipeline] " + SyncMirror(applyChanges: true));
                AssetDatabase.Refresh();
            }
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
