using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Corebound.EditorTools
{
    // The Windows player build, from the menu or without a dialog (2026-10-12: the frameless window
    // only exists in a player, and SteamPipe uploads exactly this folder - Docs/Steam/STEAMWORKS_GUIDE.md).
    // Writing "windows" into BuildRequest.txt in the project root builds Builds/Windows/Corebound.exe
    // from the Editor's update loop; the result goes to Editor.log behind [Build].
    //
    // The StreamingAssets art mirror is synced first: the player reads its art from there, so an
    // unmirrored file would look right in the Editor and ship missing (CLAUDE.md).
    [InitializeOnLoad]
    public static class PlayerBuildTool
    {
        public const string OutputFolder = "Builds/Windows";
        public const string ExecutableName = "Corebound.exe";
        private const string RequestFilePath = "BuildRequest.txt";
        private static double _nextPoll;

        static PlayerBuildTool()
        {
            EditorApplication.update -= PollRequestFile;
            EditorApplication.update += PollRequestFile;
        }

        [MenuItem("Corebound/Build/Windows Player")]
        public static void BuildWindowsPlayer()
        {
            Debug.Log("[Build] " + StreamingArtMirrorTool.SyncMirror(applyChanges: true));
            AssetDatabase.Refresh();

            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(OutputFolder, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log("[Build] Windows player " + PlayerSettings.bundleVersion + ": " + scenes.Length + " scenes, active target " + EditorUserBuildSettings.activeBuildTarget
                + ", to " + options.locationPathName);
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log("[Build] " + summary.result + " in " + summary.totalTime.TotalSeconds.ToString("0") + " s, "
                + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, " + summary.totalErrors + " errors, "
                + summary.totalWarnings + " warnings -> " + Path.GetFullPath(options.locationPathName));

            if (summary.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages.Where(message => message.type == LogType.Error || message.type == LogType.Exception))
                    {
                        Debug.LogError("[Build] " + step.name + ": " + message.content);
                    }
                }
            }
        }

        private static void PollRequestFile()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < _nextPoll)
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

                if (command.Length == 0)
                {
                    return;
                }

                File.WriteAllText(RequestFilePath, string.Empty);
            }
            catch (IOException)
            {
                return;
            }

            var parts = command.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 1 && parts[0] == "windows")
            {
                // "windows 0.1.0" sets the version first. Through PlayerSettings and SaveAssets, so
                // the Editor writes ProjectSettings.asset itself - editing that file by hand while
                // the Editor is open gets overwritten by its next save.
                if (parts.Length >= 2 && !SetVersion(parts[1]))
                {
                    return;
                }

                // After this update tick: a build started inside the poll would block it.
                EditorApplication.delayCall += BuildWindowsPlayer;
            }
            else
            {
                Debug.LogWarning("[Build] unknown request '" + command + "' in " + RequestFilePath + " - use 'windows' or 'windows <version>'");
            }
        }

        // The main menu shows Application.version, so this is what players and bug reports see.
        private static bool SetVersion(string version)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+(\.\d+){1,3}$"))
            {
                Debug.LogError("[Build] '" + version + "' is not a version like 0.1.0 - nothing built");
                return false;
            }

            PlayerSettings.bundleVersion = version;
            AssetDatabase.SaveAssets();
            Debug.Log("[Build] version set to " + PlayerSettings.bundleVersion);
            return true;
        }
    }
}
