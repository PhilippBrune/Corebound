using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Corebound.EditorTools
{
    // Puts the Game view at a fixed resolution for captures (Steam screenshots and trailer frames,
    // 2026-10-07): Steam wants 1920 x 1080, and the docked Game view is whatever size the window is.
    // Unity has no public API for this, so it goes through the Game view's internal size list - the
    // same list the resolution dropdown edits. A failure is logged and leaves the view as it was.
    public static class GameViewSizeUtility
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string RestoreIndexKey = "Corebound.GameViewSize.RestoreIndex";

        private static Type GameViewType => typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");

        public static bool TrySetFixedResolution(int width, int height, string label)
        {
            try
            {
                var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
                var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null, null);
                var groupType = sizesType.GetProperty("currentGroupType").GetValue(sizes, null);
                var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
                var count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
                var index = -1;

                for (var cursor = 0; cursor < count; cursor++)
                {
                    var size = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { cursor });
                    var w = (int)size.GetType().GetProperty("width").GetValue(size, null);
                    var h = (int)size.GetType().GetProperty("height").GetValue(size, null);
                    var kind = size.GetType().GetProperty("sizeType").GetValue(size, null).ToString();

                    if (w == width && h == height && kind == "FixedResolution")
                    {
                        index = cursor;
                        break;
                    }
                }

                if (index < 0)
                {
                    var sizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
                    var kindType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
                    var created = Activator.CreateInstance(sizeType, Enum.Parse(kindType, "FixedResolution"), width, height, label);
                    group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { created });
                    index = count;
                }

                var window = EditorWindow.GetWindow(GameViewType, false, null, false);

                if (SessionState.GetInt(RestoreIndexKey, -1) < 0)
                {
                    SessionState.SetInt(RestoreIndexKey, (int)GameViewType.GetProperty("selectedSizeIndex", AnyInstance).GetValue(window, null));
                }

                GameViewType.GetMethod("SizeSelectionCallback", AnyInstance).Invoke(window, new object[] { index, null });
                Debug.Log("[GameView] fixed at " + width + " x " + height + " for capture");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[GameView] could not set a fixed resolution: " + exception.Message);
                return false;
            }
        }

        // Back to whatever the view was set to before the first capture of this session.
        public static void RestorePrevious()
        {
            var previous = SessionState.GetInt(RestoreIndexKey, -1);

            if (previous < 0)
            {
                return;
            }

            try
            {
                var window = EditorWindow.GetWindow(GameViewType, false, null, false);
                GameViewType.GetMethod("SizeSelectionCallback", AnyInstance).Invoke(window, new object[] { previous, null });
                SessionState.SetInt(RestoreIndexKey, -1);
                Debug.Log("[GameView] size restored");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[GameView] could not restore the size: " + exception.Message);
            }
        }
    }
}
