using Microsoft.Win32;
using System.Diagnostics;

namespace WinHUD.Services
{
    public static class StartupManager
    {
        private const string AppName = "WinHUD";

        public static void EnsureAppRunsAtStartup()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                if (key != null)
                {
                    var existingVal = key.GetValue(AppName)?.ToString();
                    if (existingVal != exePath)
                    {
                        key.SetValue(AppName, exePath);
                        Debug.WriteLine("[Startup] Registered successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Startup] Failed: {ex.Message}");
            }
        }
    }
}
