using System;
using System.Collections;
using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
using System.Text;
#endif

namespace Corebound.Settings
{
    // The game window, by the Settings "Display Mode" index (owner request 2026-10-12: "a frameless
    // window mode and a window mode"). Indices are stored in the settings save, so they never move;
    // Frameless Window was appended as 3.
    //   0 Windowed              - a normal window with title bar, never larger than the screen
    //   1 Borderless Fullscreen - Unity's FullScreenWindow
    //   2 Exclusive Fullscreen  - Unity's ExclusiveFullScreen
    //   3 Frameless Window      - a window at the chosen size with no title bar or border, centred
    //
    // Unity has no frameless *windowed* mode (FullScreenWindow always covers the whole display), so
    // mode 3 is a Windows window style change after Unity has sized the window: the caption and the
    // sizing border are removed and the window is re-positioned in the centre of its monitor. That
    // only exists in a Windows player; in the Editor and on other platforms mode 3 is plain Windowed.
    public static class WindowModeController
    {
        public const int Windowed = 0;
        public const int BorderlessFullscreen = 1;
        public const int ExclusiveFullscreen = 2;
        public const int FramelessWindow = 3;
        public const int ModeCount = 4;

        // Windows' title bar and the taskbar take roughly this much height; a Windowed size that
        // would not fit with them steps down to the next preset.
        private const int WindowedHeightMargin = 96;

        private static Runner _runner;

        // width/height: the chosen resolution preset; presets: every preset, largest last, for the
        // Windowed step-down.
        public static void Apply(int mode, int width, int height, Vector2Int[] presets)
        {
            mode = Mathf.Clamp(mode, 0, ModeCount - 1);

            // Leaving Frameless: give the window its frame back first, so Unity's own windowed
            // bookkeeping starts from the style it created.
            if (mode != FramelessWindow)
            {
                FramelessWindowStyle.Restore();
            }

            switch (mode)
            {
                case BorderlessFullscreen:
                    Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
                    return;
                case ExclusiveFullscreen:
                    Screen.SetResolution(width, height, FullScreenMode.ExclusiveFullScreen);
                    return;
                case FramelessWindow:
                    Screen.SetResolution(width, height, FullScreenMode.Windowed);
                    // Unity resizes the window at the end of the frame; the style change has to come
                    // after that or Unity's resize restores the frame.
                    EnsureRunner().ApplyFramelessLater(width, height);
                    return;
                default:
                    var fitted = FitWindowed(width, height, presets);
                    Screen.SetResolution(fitted.x, fitted.y, FullScreenMode.Windowed);
                    return;
            }
        }

        // The largest preset no bigger than the chosen one that fits the display with a title bar
        // and the taskbar; the smallest preset if none does.
        private static Vector2Int FitWindowed(int width, int height, Vector2Int[] presets)
        {
            var displayWidth = Display.main != null ? Display.main.systemWidth : Screen.currentResolution.width;
            var displayHeight = Display.main != null ? Display.main.systemHeight : Screen.currentResolution.height;

            if (width <= displayWidth && height <= displayHeight - WindowedHeightMargin)
            {
                return new Vector2Int(width, height);
            }

            var best = presets != null && presets.Length > 0 ? presets[0] : new Vector2Int(width, height);

            for (var index = 0; presets != null && index < presets.Length; index++)
            {
                var preset = presets[index];

                if (preset.x <= width && preset.x <= displayWidth && preset.y <= displayHeight - WindowedHeightMargin)
                {
                    best = preset;
                }
            }

            return best;
        }

        private static Runner EnsureRunner()
        {
            if (_runner == null)
            {
                var host = new GameObject("WindowModeRunner") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.Object.DontDestroyOnLoad(host);
                _runner = host.AddComponent<Runner>();
            }

            return _runner;
        }

        private sealed class Runner : MonoBehaviour
        {
            private Coroutine _pending;

            public void ApplyFramelessLater(int width, int height)
            {
                if (_pending != null)
                {
                    StopCoroutine(_pending);
                }

                _pending = StartCoroutine(ApplyFrameless(width, height));
            }

            // Unity applies a resolution change over several frames and resizes the window to "client
            // size plus the frame it created" when it does. A style change made before that is undone
            // in size: the first player test came out 1296 x 759 for 1280 x 720, exactly the old
            // frame. So: wait until Unity reports the new size, strip, then check the real window
            // and correct it a few times if Unity resized it again.
            private IEnumerator ApplyFrameless(int width, int height)
            {
                for (var frame = 0; frame < 60 && (Screen.width != width || Screen.height != height); frame++)
                {
                    yield return null;
                }

                yield return null;
                yield return null;
                FramelessWindowStyle.Strip(width, height);

                for (var attempt = 0; attempt < 3; attempt++)
                {
                    for (var frame = 0; frame < 5; frame++)
                    {
                        yield return null;
                    }

                    if (FramelessWindowStyle.HasSize(width, height))
                    {
                        break;
                    }

                    FramelessWindowStyle.Strip(width, height);
                }

                FramelessWindowStyle.LogState(width, height);
                _pending = null;
            }
        }
    }

    // The Win32 half of Frameless Window. Every method is a no-op outside a Windows player.
    public static class FramelessWindowStyle
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int GwlStyle = -16;
        private const long WsPopup = 0x80000000L;
        private const long WsCaption = 0x00C00000L;
        private const long WsSysMenu = 0x00080000L;
        private const long WsThickFrame = 0x00040000L;
        private const long WsMinimizeBox = 0x00020000L;
        private const long WsMaximizeBox = 0x00010000L;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpFrameChanged = 0x0020;
        private const uint SwpShowWindow = 0x0040;
        private const uint MonitorDefaultToNearest = 0x00000002;

        private static long _originalStyle = -1;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public Rect Monitor;
            public Rect Work;
            public uint Flags;
        }

        private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

        [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc callback, IntPtr parameter);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder name, int capacity);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
        [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        private static IntPtr _window;

        // Unity's player window: the "UnityWndClass" window of the thread Unity's scripts run on.
        private static IntPtr ResolveWindow()
        {
            if (_window != IntPtr.Zero)
            {
                return _window;
            }

            var name = new StringBuilder(64);
            EnumThreadWindows(GetCurrentThreadId(), (window, parameter) =>
            {
                name.Length = 0;
                GetClassName(window, name, name.Capacity);

                if (name.ToString() == "UnityWndClass")
                {
                    _window = window;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            if (_window == IntPtr.Zero)
            {
                _window = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            }

            return _window;
        }

        public static void Strip(int width, int height)
        {
            var window = ResolveWindow();

            if (window == IntPtr.Zero || Screen.fullScreenMode != FullScreenMode.Windowed)
            {
                Debug.LogWarning("[Window] frameless: no player window to change");
                return;
            }

            var style = GetWindowLongPtr(window, GwlStyle).ToInt64();

            if (_originalStyle < 0)
            {
                _originalStyle = style;
            }

            var frameless = (style & ~(WsCaption | WsSysMenu | WsThickFrame | WsMinimizeBox | WsMaximizeBox)) | WsPopup;
            SetWindowLongPtr(window, GwlStyle, new IntPtr(frameless));

            // Centred on the monitor the window is on; with no frame the window rect is the client
            // rect, so this is exactly the chosen size.
            var info = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
            var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
            var x = 0;
            var y = 0;

            if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
            {
                x = info.Monitor.Left + Mathf.Max(0, (info.Monitor.Right - info.Monitor.Left - width) / 2);
                y = info.Monitor.Top + Mathf.Max(0, (info.Monitor.Bottom - info.Monitor.Top - height) / 2);
            }

            SetWindowPos(window, IntPtr.Zero, x, y, width, height, SwpNoZOrder | SwpFrameChanged | SwpShowWindow);
            Debug.Log("[Window] frameless " + width + " x " + height + " at " + x + "," + y);
        }

        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out Rect rect);

        public static bool HasSize(int width, int height)
        {
            var window = ResolveWindow();
            return window != IntPtr.Zero && GetWindowRect(window, out var rect) && rect.Right - rect.Left == width && rect.Bottom - rect.Top == height;
        }

        public static void LogState(int width, int height)
        {
            var window = ResolveWindow();

            if (window != IntPtr.Zero && GetWindowRect(window, out var rect))
            {
                Debug.Log("[Window] frameless check: window " + (rect.Right - rect.Left) + " x " + (rect.Bottom - rect.Top)
                    + " (wanted " + width + " x " + height + "), Unity renders " + Screen.width + " x " + Screen.height);
            }
        }

        public static void Restore()
        {
            if (_originalStyle < 0)
            {
                return;
            }

            var window = ResolveWindow();

            if (window != IntPtr.Zero)
            {
                SetWindowLongPtr(window, GwlStyle, new IntPtr(_originalStyle));
                SetWindowPos(window, IntPtr.Zero, 0, 0, 0, 0, SwpNoZOrder | SwpFrameChanged | SwpShowWindow | 0x0001 | 0x0002);
                Debug.Log("[Window] frame restored");
            }

            _originalStyle = -1;
        }
#else
        public static void Strip(int width, int height)
        {
        }

        public static bool HasSize(int width, int height)
        {
            return true;
        }

        public static void LogState(int width, int height)
        {
        }

        public static void Restore()
        {
        }
#endif
    }
}
