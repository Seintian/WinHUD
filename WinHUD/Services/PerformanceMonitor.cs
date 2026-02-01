using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace WinHUD.Services
{
    public class PerformanceMonitor
    {
        // Counters
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ramCounter;
        private readonly PerformanceCounter _diskCounter;
        private readonly List<(string Name, PerformanceCounter Counter)> _diskDrives;

        // Network State
        private long _previousNetworkBytes = 0;
        private bool _isFirstNetworkCheck = true;

        public PerformanceMonitor()
        {
            // Initialize Global Counters
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            _diskCounter = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "_Total");

            // Initialize Individual Drives
            _diskDrives = [];
            var driveCategory = new PerformanceCounterCategory("PhysicalDisk");
            foreach (var instance in driveCategory.GetInstanceNames())
            {
                if (instance == "_Total") continue;
                _diskDrives.Add((instance, new PerformanceCounter("PhysicalDisk", "% Disk Time", instance)));
            }

            // Initialize Network Baseline
            _previousNetworkBytes = GetTotalNetworkBytes();
        }

        // --- ATOMIC GETTERS ---

        public string GetCpuUsage() => $"{_cpuCounter.NextValue():F1}%";

        public string GetRamUsage() => $"{_ramCounter.NextValue():F0} MB";

        public string GetTotalDiskSpeed() => FormatSpeed(_diskCounter.NextValue());

        public string GetNetworkSpeed()
        {
            long currentBytes = GetTotalNetworkBytes();
            long diff = currentBytes - _previousNetworkBytes;

            // Handle first tick spike
            if (_isFirstNetworkCheck)
            {
                diff = 0;
                _isFirstNetworkCheck = false;
            }

            _previousNetworkBytes = currentBytes;
            return FormatSpeed(diff);
        }

        public string GetDiskLoadSummary()
        {
            var sb = new StringBuilder();
            foreach (var (name, counter) in _diskDrives)
            {
                float usage = counter.NextValue();
                if (usage > 100) usage = 100; // Clamp
                sb.AppendLine($"Disk {name} - {usage:F0}%");
            }
            return sb.ToString().TrimEnd();
        }

        // --- HELPERS ---

        private static long GetTotalNetworkBytes()
        {
            long total = 0;
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var ni in interfaces)
            {
                var stats = ni.GetIPStatistics();
                total += (stats.BytesReceived + stats.BytesSent);
            }
            return total;
        }

        private static string FormatSpeed(float bytes)
        {
            if (bytes < 1024) return $"{bytes:F0} B/s";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB/s";
            return $"{bytes / 1024 / 1024:F1} MB/s";
        }
    }
}
