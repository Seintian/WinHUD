using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace WinHUD.Services
{
    // Overlay Toggling UX, respecting the game state and user preferences
    public enum OverlayMode
    {
        Auto = 0,       // Follows Game State (Default)
        ForceShow = 1,  // Always Visible
        ForceHide = 2   // Always Hidden
    }

    // The data model for our settings
    public class AppConfig
    {
        public string TargetMonitorDeviceName { get; set; } = string.Empty;
        public OverlayMode Mode { get; set; } = OverlayMode.Auto;
    }

    public static class ConfigPersistence
    {
        private static readonly string ConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinHUD");
        private static readonly string ConfigPath = Path.Combine(ConfigFolder, "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);

                    if (config != null)
                    {
                        Debug.WriteLine($"[Config] Loaded successfully from {ConfigPath}");
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Config] Error loading configuration: {ex.Message}");
            }

            Debug.WriteLine("[Config] No valid config found, using defaults.");
            return new AppConfig();
        }

        public static void Save(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(ConfigFolder))
                {
                    Directory.CreateDirectory(ConfigFolder);
                }

                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                // Handle "Read-Only" or "Admin-Created" file issues
                if (File.Exists(ConfigPath))
                {
                    var attributes = File.GetAttributes(ConfigPath);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(ConfigPath, attributes & ~FileAttributes.ReadOnly);
                    }
                }

                File.WriteAllText(ConfigPath, json);

                Debug.WriteLine($"[Config] Saved successfully to {ConfigPath}");
            }
            catch (UnauthorizedAccessException)
            {
                // This specifically catches the "Admin created it, User can't write it" error
                System.Windows.MessageBox.Show(
                    $"WinHUD cannot save your settings.\n\nPlease go to:\n{ConfigFolder}\n\nAnd delete 'config.json' manually.",
                    "Permission Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Config] Error saving configuration: {ex.Message}");
            }
        }
    }
}
