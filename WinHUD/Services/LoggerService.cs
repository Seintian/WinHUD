using System;
using System.IO;
using Serilog;

namespace WinHUD.Services
{
    public static class LoggerService
    {
        public static void Initialize()
        {
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinHUD", "logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string logFile = Path.Combine(logDir, "winhud-log-.txt");

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(
                        path: logFile,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                Log.Information("=========================================================");
                Log.Information("WinHUD Logging Initialized");
                Log.Information($"Log Directory: {logDir}");
                Log.Information("=========================================================");
            }
            catch (Exception ex)
            {
                // Fallback if formatting or directory creation fails
                System.Diagnostics.Debug.WriteLine($"[FATAL] Failed to initialize Serilog: {ex.Message}");
            }
        }
    }
}
