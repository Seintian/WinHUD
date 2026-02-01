using System;
using System.Diagnostics;
using System.Linq;

namespace WinHUD.Services
{
    public static class GameDetector
    {
        public static bool IsProcessRunning(string processName)
        {
            // Robust, case-insensitive check using LINQ
            return Process.GetProcesses()
                .Any(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
