using System.Diagnostics;

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
