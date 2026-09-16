---
name: unity-editor-control
description: Control the locally running Unity Editor for this project (Corebound) from Claude Code via Win32 API automation through PowerShell - screenshots, Editor-UI clicks, forcing script recompilation, and reading Editor.log. Use whenever a task requires observing or driving the actual Unity Editor window (verifying a scene fix, checking Console output, navigating the Hierarchy/Inspector) rather than just editing project files.
---

# Unity Editor control (Windows, PowerShell + Win32 API)

Claude Code has no first-party Unity integration. Everything below is achieved by
calling user32.dll functions from PowerShell (`Add-Type` + `DllImport`). It works,
but has real limitations documented at the bottom - read those before relying on
this for gameplay-input testing.

## Finding the window

```powershell
$proc = Get-Process Unity | Where-Object { $_.MainWindowTitle -like "*SampleScene*" }
# or, once you know the PID from an earlier call:
$proc = Get-Process -Id <pid>
$hwnd = $proc.MainWindowHandle
```

The window can be moved/resized by the user between calls (e.g. dragged to another
monitor). **Always re-query `GetWindowRect` in the same call you compute click
coordinates from** - never reuse a rect captured earlier in the conversation.

## Screenshot (works even if window isn't focused/foreground)

Use `PrintWindow` with flag `2` (`PW_RENDERFULLCONTENT`), not a desktop-region
screen-grab - it captures the window's real content regardless of z-order or
occlusion by other windows:

```powershell
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
public static class UnityCap {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    public static void Capture(IntPtr hWnd, string path) {
        RECT r; GetWindowRect(hWnd, out r);
        int w = r.Right - r.Left, h = r.Bottom - r.Top;
        using (Bitmap bmp = new Bitmap(w, h)) {
            using (Graphics g = Graphics.FromImage(bmp)) {
                IntPtr hdc = g.GetHdc();
                PrintWindow(hWnd, hdc, 2u);
                g.ReleaseHdc(hdc);
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
'@ -ErrorAction Stop
[UnityCap]::Capture($hwnd, "$env:TEMP\claude\shot.png")
```

Then `Read` the PNG directly - the tool renders it as an image.

**Click coordinates are window-relative + the `GetWindowRect` origin.** The
captured bitmap is 1:1 with the window's client+chrome area at `GetWindowRect`
size, so `screenX = r.Left + localX`, `screenY = r.Top + localY`. No DPI/display-
scaling correction needed on top of that.

**Zoom before clicking small targets.** Hierarchy expand-arrows, toolbar icons,
and search-box boundaries are a handful of pixels. Crop the last screenshot with
Python/PIL (`img.crop((x0,y0,x1,y1)).resize(...)`) and read the crop before
computing a click point - guessing from the full screenshot wastes turns.

## Clicking / keyboard input into the Editor

```powershell
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
[DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
[DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
// MOUSEEVENTF_LEFTDOWN=0x0002, MOUSEEVENTF_LEFTUP=0x0004, KEYEVENTF_KEYUP=0x0002
```

Do the focus + action in **one PowerShell invocation**. Claude Code runs as a
VS Code extension, so VS Code reclaims OS foreground the instant a tool call
returns - a separate "focus Unity" call followed by a separate "click" call will
send the click to VS Code instead.

`SetForegroundWindow` can legitimately return `False` when the human is actively
using their own machine (Windows' foreground-lock protects whatever they last
clicked). Check the return value; if `False`, don't fight it - tell the user
you need them to click the Unity window once, and wait.

For text fields (e.g. the Hierarchy search box), raw `keybd_event` per character
is unreliable; `[System.Windows.Forms.SendKeys]::SendWait("text")` after loading
`-AssemblyName System.Windows.Forms` works better once the field has focus (click
it first, confirm the blue focus outline in a screenshot, then send keys).

Hierarchy row Y-positions **shift** whenever a node above the target expands/
collapses or the window is resized/moved - never hardcode a Y from an earlier
screenshot in this session; re-derive it from the most recent capture. Prefer the
Hierarchy search box (click it, type a name) over pixel-hunting a specific row -
it's far more reliable and immune to scroll/expand-state changes.

## Forcing a script recompile

Unity's file-watcher can go stale after many edits/reimports in one session (a
known instability also seen as `Asset File Changes: new=0, changed=0` in
Editor.log even after a genuine on-disk edit). To force detection:

1. Make sure the Editor is **stopped** (not in Play mode) - C# edits don't
   compile while playing, they just queue.
2. Send Ctrl+R (`Assets > Refresh`) via `keybd_event`, in the same call as the
   focus grab.
3. **Poll `Editor.log` for confirmation - don't assume it worked.** The log
   lives at `C:\Users\<user>\AppData\Local\Unity\Editor\Editor.log`. Success
   looks like:
   ```
   *** Tundra build success (N.NN seconds), <K> items updated, ...
   ```
   Failure shows `error CS####` lines instead. A "0.005 seconds" refresh with
   no `Tundra build` line means nothing was picked up - retry Ctrl+R, or as a
   last resort touch the file (add+remove a blank line) to force a fresh write
   event, or ask the user to do **Assets > Reimport All** (reliable but slow,
   ~1-2 min) or a full Editor restart (most reliable, resets the watcher).
4. When polling in a loop, use a **correct PowerShell pipeline inside the
   `-Command` string** - `Select-String ... | Select-Object -Last N` must both
   be inside the quoted PowerShell command, not split across a `|` in the outer
   bash command line (that pipes to a nonexistent bash `Select-Object`).
5. Only re-enter Play after confirming the build succeeded, then re-check
   behavior (screenshot and/or targeted `Debug.Log` diagnostics, see below).

## Debug.Log as a diagnostic tool

When static code reading doesn't explain observed behavior, add a temporary
`Debug.Log($"[SomeDiag] ...")` at the exact branch point in question, go through
the recompile dance above, re-enter Play, then:

```bash
powershell.exe -NoProfile -Command "Get-Content '<EditorLogPath>' -Tail 60" | grep -i "SomeDiag"
```

This has repeatedly been the fastest way to resolve "the code looks right but
the behavior is wrong" - faster than trying to pixel-click through Inspector
Debug mode to read a private field's live value. Remove the diagnostic lines
(and verify brace balance) once resolved - never ship them.

## Hard limitation: gameplay/runtime input does NOT work

Unity's new Input System (`UnityEngine.InputSystem`) reads Raw Input directly
from the OS/driver during Play Mode. Synthetic input via `SendInput`/
`keybd_event`/`mouse_event` does **not** register with `Keyboard.current` /
`Mouse.current` - so WASD movement, firing, clicking a runtime Canvas button
(anything routed through `InputSystemUIInputModule` + `EventSystem`) cannot be
driven this way. This includes MainMenu button clicks once in Play mode, not
just combat.

What **does** work via this same Win32 approach: Editor-level UI - Play/Pause/
Step toolbar, Hierarchy/Project/Inspector panel interaction, menu items, Console -
because those route through Unity's IMGUI/EditorWindow message handling, not the
Input System's device polling.

Net effect: use this skill to verify visual/state outcomes (does the panel show,
what does the Inspector say, did it compile) and to drive Editor-level actions
(select an object, expand a node, Play/Stop). For anything that requires the
*player* to press a key or click an in-game button, either ask the user to do
that one step live, or restructure the test to not require it (e.g. mutate the
save file / call the method via a temporary diagnostic instead of simulating the
UI action that would normally trigger it).
