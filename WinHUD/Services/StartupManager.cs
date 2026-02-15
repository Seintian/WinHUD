using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace WinHUD.Services
{
    public static class StartupManager
    {
        private const string AppName = "WinHUD";

        public static void EnsureAppRunsAtStartup()
        {
            try
            {
                // 1. Get current executable path
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(exePath)) return;

                // 2. Register in Registry (Run at Startup)
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        var existingVal = key.GetValue(AppName)?.ToString();
                        if (existingVal != exePath)
                        {
                            key.SetValue(AppName, exePath);
                            Debug.WriteLine("[Startup] Registry registered successfully.");
                        }
                    }
                }

                // 3. Create Start Menu Shortcut (if missing)
                EnsureStartMenuShortcut(exePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Startup] Failed: {ex.Message}");
            }
        }

        private static void EnsureStartMenuShortcut(string exePath)
        {
            try
            {
                // Path to: %AppData%\Microsoft\Windows\Start Menu\Programs\WinHUD.lnk
                string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string shortcutPath = Path.Combine(startMenuPath, $"{AppName}.lnk");

                string workingDirectory = Path.GetDirectoryName(exePath) ?? "";

                if (!File.Exists(shortcutPath))
                {
                    // Escape single quotes for PowerShell (replace ' with '')
                    string safeExePath = exePath.Replace("'", "''");
                    string safeWorkingDir = workingDirectory.Replace("'", "''");
                    string safeShortcutPath = shortcutPath.Replace("'", "''");

                    // PowerShell command to create the shortcut using WScript.Shell
                    string psScript =
                        $"$ws = New-Object -ComObject WScript.Shell; " +
                        $"$s = $ws.CreateShortcut('{safeShortcutPath}'); " +
                        $"$s.TargetPath = '{safeExePath}'; " +
                        $"$s.WorkingDirectory = '{safeWorkingDir}'; " +
                        $"$s.Description = 'WinHUD Overlay'; " +
                        $"$s.Save()";

                    // Run PowerShell silently
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });

                    Debug.WriteLine("[Startup] Start Menu shortcut created.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Startup] Shortcut creation failed: {ex.Message}");
            }
        }
    }
}
