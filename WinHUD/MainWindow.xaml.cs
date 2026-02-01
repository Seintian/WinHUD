using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using WinHUD.Core;
using WinHUD.Services;

namespace WinHUD
{
    public partial class MainWindow : Window
    {
        // --- CONFIG ---
        private const string TargetProcess = "gameoverlayui64";

        // --- SERVICES ---
        private PerformanceMonitor? _monitor;
        private readonly DispatcherTimer _timer;

        // --- STATE ---
        private bool _isManualOverride = false;
        private IntPtr _windowHandle;

        public MainWindow()
        {
            InitializeComponent();
            StartupManager.EnsureAppRunsAtStartup();

            // Initialize Timer
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnGameLoopTick;

            // Whenever the text grows/shrinks, re-calculate position immediately
            this.SizeChanged += OnWindowSizeChanged;

            // Start State
            this.Opacity = 0;
            InitializePerformanceMonitor();
            _timer.Start();
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.Opacity > 0 && _windowHandle != IntPtr.Zero)
            {
                // 1. Find the monitor the window is CURRENTLY on
                IntPtr hMonitor = NativeMethods.MonitorFromWindow(_windowHandle, NativeMethods.MONITOR_DEFAULTTONEAREST);

                // 2. Get that monitor's Work Area
                var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFO)) };
                if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
                {
                    var workArea = mi.rcWork;

                    // 3. Re-position: Keep Left, but adjust Top to grow "Upwards"
                    this.Left = workArea.Left + 10;
                    this.Top = workArea.Bottom - this.ActualHeight - 10;
                }
            }
        }

        private void InitializePerformanceMonitor()
        {
            try
            {
                _monitor = new PerformanceMonitor();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to init counters: {ex.Message}");
            }
        }

        // --- THE MAIN LOOP ---
        private void OnGameLoopTick(object? sender, EventArgs e)
        {
            bool isGameActive = GameDetector.IsProcessRunning(TargetProcess);
            bool shouldShow = isGameActive || _isManualOverride;

            if (shouldShow)
            {
                ShowWindow();
                UpdateUI();
            }
            else
            {
                HideWindow();
            }
        }

        // --- ATOMIC ACTIONS ---

        private void ShowWindow()
        {
            if (this.Opacity < 1)
            {
                this.Opacity = 1;
                this.UpdateLayout();
            }

            // Enforce "Always On Top"
            this.Topmost = false;
            this.Topmost = true;
        }

        private void HideWindow()
        {
            if (this.Opacity > 0) this.Opacity = 0;
        }

        private void UpdateUI()
        {
            if (_monitor == null) return;

            CpuText.Text = $"CPU: {_monitor.GetCpuUsage()}";
            RamText.Text = $"RAM: {_monitor.GetRamUsage()}";
            DiskText.Text = $"Disk: {_monitor.GetTotalDiskSpeed()}";
            NetText.Text = $"Net: {_monitor.GetNetworkSpeed()}";
            DiskListText.Text = _monitor.GetDiskLoadSummary();
        }

        // --- WINDOW POSITIONING (Native Method) ---
        private void SnapToBottomLeftOfActiveScreen()
        {
            // 1. Get Mouse Position
            NativeMethods.GetCursorPos(out var point);

            // 2. Find Monitor where mouse is
            IntPtr hMonitor = NativeMethods.MonitorFromPoint(point, 2 /* MONITOR_DEFAULTTONEAREST */);

            // 3. Get Monitor Work Area
            var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFO)) };
            if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
            {
                var workArea = mi.rcWork;

                // 4. Calculate Position (Left-Bottom with 10px padding)
                this.Left = workArea.Left + 10;
                this.Top = workArea.Bottom - this.ActualHeight - 10;
            }
        }

        // --- WINDOW SETUP (Hotkeys & Transparency) ---

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowHandle = new WindowInteropHelper(this).Handle;

            // 1. Make Transparent & Ghost
            NativeMethods.SetWindowGhostMode(_windowHandle);

            // 2. Register Hotkey (Alt + Shift + H)
            // MOD_ALT(1) | MOD_SHIFT(4) = 5. VK_H = 0x48.
            bool success = NativeMethods.RegisterHotKey(_windowHandle, 9000, 5, 0x48);
            if (!success) Debug.WriteLine("Failed to register hotkey.");

            // 3. Listen for Hotkey
            HwndSource.FromHwnd(_windowHandle).AddHook(HwndHook);
        }

        protected override void OnClosed(EventArgs e)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, 9000);
            base.OnClosed(e);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == 9000)
            {
                _isManualOverride = !_isManualOverride;
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
