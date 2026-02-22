using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using LibreHardwareMonitor.Hardware;

namespace WinHUD.Services
{
    public class PerformanceMonitor : IDisposable
    {
        // Native Counters
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ramCounter;

        // Separate Disk Counters
        private readonly PerformanceCounter _diskReadCounter;
        private readonly PerformanceCounter _diskWriteCounter;
        private readonly List<(string Name, PerformanceCounter Counter)> _diskDrives;

        // LibreHardwareMonitor
        private readonly Computer _computer;
        private readonly IHardware? _gpuHardware;

        // Separate Network State
        private long _previousNetworkReceived = 0;
        private long _previousNetworkSent = 0;
        private bool _isFirstNetworkCheck = true;

        public PerformanceMonitor()
        {
            // 1. Initialize Standard Counters
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // Split Disk Counters into Read and Write
            _diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            _diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");

            // Initialize individual disk counters for load summary
            _diskDrives = [];
            try
            {
                var driveCategory = new PerformanceCounterCategory("PhysicalDisk");
                foreach (var instance in driveCategory.GetInstanceNames())
                {
                    if (instance == "_Total") continue;
                    _diskDrives.Add((instance, new PerformanceCounter("PhysicalDisk", "% Disk Time", instance)));
                }
            }
            catch
            {
                Debug.WriteLine("[Monitor] Failed to initialize disk counters. Admin rights might be needed.");
            }

            // 2. Initialize GPU Monitor
            _computer = new Computer
            {
                IsGpuEnabled = true,
                IsCpuEnabled = false,
                IsMemoryEnabled = false,
                IsMotherboardEnabled = false,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsStorageEnabled = false
            };

            try
            {
                _computer.Open();
                // Find the first valid GPU (NVIDIA, AMD, or Intel)
                _gpuHardware = _computer.Hardware
                    .FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia
                                      || h.HardwareType == HardwareType.GpuAmd
                                      || h.HardwareType == HardwareType.GpuIntel);
            }
            catch
            {
                Debug.WriteLine("[Monitor] Failed to open GPU hardware. Admin rights might be needed.");
            }

            // Initialize network baseline
            var (initialRx, initialTx) = GetNetworkStats();
            _previousNetworkReceived = initialRx;
            _previousNetworkSent = initialTx;
        }

        // --- GETTERS ---

        public string GetCpuUsage() => $"{_cpuCounter.NextValue():F1}%";

        public string GetRamUsage() => $"{_ramCounter.NextValue():F0} MB";

        // GPU Getter
        public string GetGpuUsage()
        {
            if (_gpuHardware == null) return "GPU: N/A";

            // We must call Update() to refresh sensors
            _gpuHardware.Update();

            // Find the "Load" sensor (specifically "GPU Core")
            var loadSensor = _gpuHardware.Sensors
                .FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "GPU Core");

            float value = loadSensor?.Value ?? 0;
            return $"GPU: {value:F0}%";
        }

        // Formatted Disk Read/Write
        public string GetTotalDiskSpeed()
        {
            float readSpeed = _diskReadCounter.NextValue();
            float writeSpeed = _diskWriteCounter.NextValue();
            return $"↓ {FormatSpeed(readSpeed)} - ↑ {FormatSpeed(writeSpeed)}";
        }

        // Formatted Network Download/Upload
        public string GetNetworkSpeed()
        {
            var (currentRx, currentTx) = GetNetworkStats();

            long diffRx = currentRx - _previousNetworkReceived;
            long diffTx = currentTx - _previousNetworkSent;

            // Handle first tick strike
            if (_isFirstNetworkCheck)
            {
                diffRx = 0;
                diffTx = 0;
                _isFirstNetworkCheck = false;
            }

            _previousNetworkReceived = currentRx;
            _previousNetworkSent = currentTx;

            return $"↓ {FormatSpeed(diffRx)} - ↑ {FormatSpeed(diffTx)}";
        }

        public string GetDiskLoadSummary()
        {
            var sb = new StringBuilder();
            foreach (var (name, counter) in _diskDrives)
            {
                float usage = counter.NextValue();
                if (usage > 100) usage = 100;
                sb.AppendLine($"Disk {name} - {usage:F0}%");
            }
            return sb.ToString().TrimEnd();
        }

        // --- HELPERS ---

        // Returns a tuple separating Received (Rx) and Sent (Tx) bytes
        private static (long Received, long Sent) GetNetworkStats()
        {
            long rx = 0;
            long tx = 0;

            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var ni in interfaces)
            {
                var stats = ni.GetIPStatistics();
                rx += stats.BytesReceived;
                tx += stats.BytesSent;
            }

            return (rx, tx);
        }

        private static string FormatSpeed(float bytes)
        {
            if (bytes < 1024) return $"{bytes:F0} B/s";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB/s";
            return $"{bytes / 1024 / 1024:F1} MB/s";
        }

        public void Dispose()
        {
            try
            {
                _computer.Close();
                _cpuCounter?.Dispose();
                _ramCounter?.Dispose();
                _diskReadCounter?.Dispose();
                _diskWriteCounter?.Dispose();
                foreach (var drive in _diskDrives)
                {
                    drive.Counter?.Dispose();
                }
            }
            catch { }
        }
    }
}
