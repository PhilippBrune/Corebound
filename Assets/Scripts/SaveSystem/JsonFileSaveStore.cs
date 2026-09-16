using System;
using System.IO;
using UnityEngine;

namespace Corebound.SaveSystem
{
    public sealed class JsonFileSaveStore
    {
        public event Action<string, string> SaveFailed;

        public event Action<string, string> LoadFailed;

        public bool TryLoad<T>(string path, out T data) where T : class
        {
            data = null;

            if (!File.Exists(path))
            {
                return false;
            }

            if (TryReadFile(path, out data))
            {
                return true;
            }

            // Primary file failed to parse (corrupt/truncated write). Fall back to the last
            // known-good backup written by Save<T> rather than losing the player's progress.
            var backupPath = path + ".bak";

            if (File.Exists(backupPath) && TryReadFile(backupPath, out data))
            {
                Debug.LogWarning("Corebound: recovered save data for '" + path + "' from backup file.");
                return true;
            }

            return false;
        }

        public bool Save<T>(string path, T data) where T : class
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(path);

                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var json = JsonUtility.ToJson(data, true);
                var tempPath = path + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(path))
                {
                    // Atomic swap: the previous good file becomes the backup, the new file
                    // becomes current, in one filesystem operation. A crash mid-write leaves
                    // either the old file or the new one intact, never a half-written file.
                    var backupPath = path + ".bak";
                    File.Replace(tempPath, path, backupPath);
                }
                else
                {
                    File.Move(tempPath, path);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("Corebound: failed to save '" + path + "': " + exception.Message);
                SaveFailed?.Invoke(path, exception.Message);
                return false;
            }
        }

        public void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private bool TryReadFile<T>(string path, out T data) where T : class
        {
            try
            {
                var json = File.ReadAllText(path);
                data = JsonUtility.FromJson<T>(json);
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogError("Corebound: failed to load '" + path + "': " + exception.Message);
                LoadFailed?.Invoke(path, exception.Message);
                data = null;
                return false;
            }
        }
    }
}
