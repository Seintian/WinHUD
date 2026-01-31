using System;
using System.Diagnostics;               // Needed for PerformanceCounter
using System.Runtime.InteropServices;   // Needed for Windows API calls
using System.Windows;
using System.Windows.Interop;           // Needed to access the window handle (HWND)
using System.Windows.Threading;         // Needed for DispatcherTimer
using System.Net.NetworkInformation;    // Needed for Network API

namespace WinHUD
{
    public partial class MainWindow : Window
    {
        // 0. Settings
        // trigger: The process name for Steam's In-Game Overlay
        private const string TriggerProcessName = "gameoverlayui64";

        // 1. Define Counters
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private PerformanceCounter diskCounter;
        private List<(string Name, PerformanceCounter Counter)> diskPercentageCounters;

        // 2. Network tracking variables
        private long oldNetworkBytes = 0;
        private bool isFirstNetworkCheck = true;

        // 3. Timer for periodic updates
        private readonly DispatcherTimer updateTimer;

        public MainWindow()
        {
            InitializeComponent();

            // 1. Initialize the counters
            // "Processor", "% Processor Time", "_Total" = Total CPU usage across all cores
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            // "Memory", "Available MBytes" = Free RAM
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // "PhysicalDisk", "Disk Bytes/sec", "_Total" covers all drives.
            diskCounter = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "_Total");
            diskPercentageCounters = [];
            
            // Get all physical disk instances (e.g., "0 C:", "1 D:")
            var pdCategory = new PerformanceCounterCategory("PhysicalDisk");
            var instanceNames = pdCategory.GetInstanceNames();

            foreach (var name in instanceNames)
            {
                // Ignore "_Total" because we want individual drives
                if (name == "_Total") continue;

                // Create a counter for "% Disk Time" (Active usage)
                var pCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", name);

                // Add to our list
                diskPercentageCounters.Add((name, pCounter));
            }

            // 3. Setup the timer (Update every 1 second)
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            // 2. Network baseline setup
            oldNetworkBytes = GetTotalNetworkBytes();

            // Event 1: When the window loads
            this.Loaded += (s, e) => PositionWindowBottomLeft();

            // Event 2: When the text changes size (keeps it anchored bottom-left)
            this.SizeChanged += (s, e) => PositionWindowBottomLeft();

            // START INVISIBLE: Wait for Steam Overlay
            this.Opacity = 0;
        }

        // Update the method signature to explicitly allow nullable sender
        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            var processes = Process.GetProcessesByName(TriggerProcessName);
            bool isGameRunning = processes.Length > 0;

            if (!isGameRunning)
            {
                // If game stops, hide the window and stop processing metrics
                if (this.Opacity > 0)
                {
                    this.Opacity = 0;
                }

                return; // Exit here to save CPU
            }

            // --- STEP 2: SHOW & POSITION ---
            // If game just started (window was hidden), show it and reset position
            if (this.Opacity < 1)
            {
                this.Opacity = 1;
                
                // Force a layout update so we get the correct size
                this.UpdateLayout();
                PositionWindowBottomLeft(); // Snap to position immediately
            }

            // --- STEP 4: ENFORCE TOPMOST ---
            // Force the window to stay on top of the game
            if (!this.Topmost)
            {
                this.Topmost = true;
            }

            // --- STEP 5: UPDATE METRICS ---
            UpdateMetrics();
        }

        private void UpdateMetrics()
        {
            // --- STEP 3: UPDATE METRICS ---
            try
            {
                // 4. Read values
                float cpuUsage = cpuCounter.NextValue();
                float availableRam = ramCounter.NextValue();
                float diskBytesPerSec = diskCounter.NextValue();

                // --- NETWORK I/O ---
                long currentNetworkBytes = GetTotalNetworkBytes();
                long bytesDiff = currentNetworkBytes - oldNetworkBytes;

                if (isFirstNetworkCheck)
                {
                    bytesDiff = 0;
                    isFirstNetworkCheck = false;
                }
                oldNetworkBytes = currentNetworkBytes;

                // --- DISK LIST ---
                System.Text.StringBuilder sb = new();
                foreach (var item in diskPercentageCounters)
                {
                    float usage = item.Counter.NextValue();
                    if (usage > 100) usage = 100;
                    sb.AppendLine($"Disk {item.Name} - {usage:F0}%");
                }
                DiskListText.Text = sb.ToString();

                // 5. Update UI
                CpuText.Text = $"CPU: {cpuUsage:F1}%";
                RamText.Text = $"Free RAM: {availableRam} MB";
                DiskText.Text = $"Disk Total: {FormatSpeed(diskBytesPerSec)}";
                NetText.Text = $"Net: {FormatSpeed(bytesDiff)}";
            }
            catch
            {
                // Handle error as appropriate (e.g., log, throw, etc.)
                // For now, just throw an exception
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        // Helper: Get total bytes (Sent + Received) across all active interfaces
        private static long GetTotalNetworkBytes()
        {
            long total = 0;

            // Get all interfaces that are UP and not Loopback (127.0.0.1)
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up
                          && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var ni in interfaces)
            {
                var stats = ni.GetIPStatistics();
                total += (stats.BytesReceived + stats.BytesSent);
            }
            return total;
        }

        // Helper: Format bytes to KB/s or MB/s
        private static string FormatSpeed(float bytesPerSec)
        {
            if (bytesPerSec < 1024) return $"{bytesPerSec:F0} B/s";
            if (bytesPerSec < 1024 * 1024) return $"{bytesPerSec / 1024:F1} KB/s";
            return $"{bytesPerSec / (1024 * 1024):F1} MB/s";
        }

        private void PositionWindowBottomLeft()
        {
            // Get the screen's working area (excludes taskbar)
            var workArea = SystemParameters.WorkArea;

            // Calculate position: Left Edge + 10px margin
            this.Left = workArea.Left + 10;

            // Calculate position: Bottom Edge - Window Height - 10px margin
            this.Top = workArea.Bottom - this.ActualHeight - 10;
        }

        // 1. Define the Windows API constants for window styles
        const int WS_EX_TRANSPARENT = 0x00000020; // Click-through (Ghost)
        const int WS_EX_TOPMOST = 0x00000008;     // Always on top
        const int GWL_EXSTYLE = -20;              // Get/Set Extended Style

        // 2. Import the necessary functions from user32.dll (The core Windows UI library)
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        // 3. Apply the styles when the window loads
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Get the "Handle" (ID) of this window
            IntPtr hwnd = new WindowInteropHelper(this).Handle;

            // Get the current style
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            // Add the "Ghost" and "TopMost" flags
            int result = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOPMOST);

            // Optionally, check for errors (SetWindowLong returns 0 on failure)
            if (result == 0)
            {
                // Handle error as appropriate (e.g., log, throw, etc.)
                // For now, just throw an exception
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }
}
