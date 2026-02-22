using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using LibreHardwareMonitor.Hardware;
using WinHUD.Models;

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
                _gpuHardware = _computer.Hardware
                    .FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia
                                      || h.HardwareType == HardwareType.GpuAmd
                                      || h.HardwareType == HardwareType.GpuIntel);
            }
            catch
            {
                Debug.WriteLine("[Monitor] Failed to open GPU hardware. Admin rights might be needed.");
            }

            var (initialRx, initialTx) = GetNetworkStats();
            _previousNetworkReceived = initialRx;
            _previousNetworkSent = initialTx;
        }

        // --- SINGLE GETTER ---

        public HardwareData GetLatestData()
        {
            var data = new HardwareData();

            // 1. CPU & RAM
            data.CpuUsagePercent = _cpuCounter.NextValue();
            data.RamAvailableMb = _ramCounter.NextValue();

            // 2. GPU
            if (_gpuHardware != null)
            {
                _gpuHardware.Update();
                var loadSensor = _gpuHardware.Sensors
                    .FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "GPU Core");
                data.GpuUsagePercent = loadSensor?.Value ?? 0;
            }

            // 3. Disk Total Speed
            data.DiskReadBytesPerSec = _diskReadCounter.NextValue();
            data.DiskWriteBytesPerSec = _diskWriteCounter.NextValue();

            // 4. Individual Disk Loads
            foreach (var (name, counter) in _diskDrives)
            {
                float usage = counter.NextValue();
                if (usage > 100) usage = 100;
                data.DiskLoads[name] = usage;
            }

            // 5. Network Speed
            var (currentRx, currentTx) = GetNetworkStats();

            long diffRx = currentRx - _previousNetworkReceived;
            long diffTx = currentTx - _previousNetworkSent;

            if (_isFirstNetworkCheck)
            {
                diffRx = 0;
                diffTx = 0;
                _isFirstNetworkCheck = false;
            }

            _previousNetworkReceived = currentRx;
            _previousNetworkSent = currentTx;

            data.NetDownloadBytesPerSec = diffRx;
            data.NetUploadBytesPerSec = diffTx;

            return data;
        }

        // --- HELPERS ---

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
