using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using WinHUD.Core;
using WinHUD.Services;

// ALIAS: Distinct between WPF and WinForms Screen classes
using WinFormsScreen = System.Windows.Forms.Screen;

namespace WinHUD
{
    public partial class MainWindow : Window
    {
        // --- CONFIG ---
        private const string TargetProcess = "gameoverlayui64"; // or "Notepad" for testing

        // --- SERVICES ---
        private PerformanceMonitor? _monitor;
        private TrayService? _trayService;
        private readonly DispatcherTimer _timer;

        // --- STATE ---
        private bool _isManualOverride = false;
        private IntPtr _windowHandle;

        // Default to Primary Screen initially
        private WinFormsScreen? _targetScreen = WinFormsScreen.PrimaryScreen;

        public MainWindow()
        {
            InitializeComponent();
            StartupManager.EnsureAppRunsAtStartup();

            // 1. Initialize Services
            InitializePerformanceMonitor();

            // Initialize Tray Icon with callbacks: (OnMonitorSelected, OnExit)
            _trayService = new TrayService(
                onMonitorSelected: (screen) =>
                {
                    _targetScreen = screen;
                    // If window is visible, move it immediately to the new screen
                    if (this.Opacity > 0) SnapToTargetScreen();
                },
                onExit: () =>
                {
                    System.Windows.Application.Current.Shutdown();
                }
            );

            // 2. Setup Timer
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnGameLoopTick;

            // 3. Setup Anchor Logic (Keep window at bottom when text grows)
            this.SizeChanged += OnWindowSizeChanged;

            // Start State
            this.Opacity = 0;
            _timer.Start();
        }

        private void InitializePerformanceMonitor()
        {
            try
            {
                _monitor = new PerformanceMonitor();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to init counters: {ex.Message}");
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

        // --- VISIBILITY & POSITIONING ---

        private void ShowWindow()
        {
            if (this.Opacity < 1)
            {
                // Snap to the selected screen BEFORE showing
                SnapToTargetScreen();

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

        private void SnapToTargetScreen()
        {
            if (_targetScreen == null) return;

            // Get Working Area of the user-selected screen
            var workArea = _targetScreen.WorkingArea;

            // Calculate Position (Bottom-Left)
            // Note: We use WPF coordinates, but WinForms Screen returns pixels. 
            // In 99% of cases (100% DPI), they match. Handling DPI mixing is complex, 
            // but this works for standard setups.
            this.Left = workArea.Left + 10;
            this.Top = workArea.Bottom - this.ActualHeight - 10;
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // When text size changes, re-anchor to the bottom of the CURRENT target screen
            // NOTE: `screen` is a safe copy of the nullable _targetScreen
            if (this.Opacity > 0 && _targetScreen is { } screen)
            {
                var workArea = screen.WorkingArea;
                this.Left = workArea.Left + 10;
                this.Top = workArea.Bottom - this.ActualHeight - 10;
            }
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

        // --- WINDOW SETUP (Native Methods) ---

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowHandle = new WindowInteropHelper(this).Handle;

            // 1. Make Transparent & Ghost
            NativeMethods.SetWindowGhostMode(_windowHandle);

            // 2. Register Hotkey (Alt + Shift + H)
            NativeMethods.RegisterHotKey(_windowHandle, 9000, 5, 0x48);

            // 3. Listen for Hotkey
            HwndSource.FromHwnd(_windowHandle).AddHook(HwndHook);
        }

        protected override void OnClosed(EventArgs e)
        {
            // Cleanup Tray Icon
            _trayService?.Dispose();

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
