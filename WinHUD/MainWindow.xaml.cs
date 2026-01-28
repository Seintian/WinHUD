using System;
using System.Diagnostics;               // Needed for PerformanceCounter
using System.Runtime.InteropServices;   // Needed for Windows API calls
using System.Windows;
using System.Windows.Interop;           // Needed to access the window handle (HWND)
using System.Windows.Threading;         // Needed for DispatcherTimer

namespace WinHUD
{
    public partial class MainWindow : Window
    {
        // 1. Define Counters
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private DispatcherTimer updateTimer;

        public MainWindow()
        {
            InitializeComponent();

            // 2. Initialize the counters
            // "Processor", "% Processor Time", "_Total" = Total CPU usage across all cores
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            // "Memory", "Available MBytes" = Free RAM
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // 3. Setup the timer (Update every 1 second)
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        // Update the method signature to explicitly allow nullable sender
        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            // 4. Read values
            // NextValue() gets the current metric
            float cpuUsage = cpuCounter.NextValue();
            float availableRam = ramCounter.NextValue();

            // 5. Update UI
            // Note: We format the CPU to 1 decimal place (F1)
            CpuText.Text = $"CPU: {cpuUsage:F1}%";
            RamText.Text = $"Free RAM: {availableRam} MB";

            // If the window somehow lost its Topmost status (e.g., you Alt-Tabbed), force it back.
            if (!this.Topmost)
            {
                this.Topmost = true;
            }
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
