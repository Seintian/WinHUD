using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace WinHUD.Core
{
    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        
        // Window Positioning API to fight Fullscreen apps
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy,
            uint uFlags
        );

        // --- CONSTANTS ---
        public const int WS_EX_TRANSPARENT  = 0x00000020; // Click-through
        public const int WS_EX_TOPMOST      = 0x00000008; // Always on top
        public const int WS_EX_TOOLWINDOW   = 0x00000080; // Hides from Alt+Tab
        public const int GWL_EXSTYLE        = -20;
        public const int WM_HOTKEY          = 0x0312;
        public const uint MONITOR_DEFAULTTONEAREST = 0x00000002; // To anchor when Window changes
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;

        // --- WINDOW STYLES ---
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        // --- HOTKEYS ---
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // --- MOUSE & MONITOR POSITIONS ---
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        // --- HELPER: Set Window to "Ghost" Mode ---
        public static void SetWindowGhostMode(IntPtr hwnd)
        {
            int styles = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, styles | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW);
        }
    }
}
