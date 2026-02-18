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
        private const string TargetProcess = "gameoverlayui64";

        // --- SERVICES ---
        private PerformanceMonitor? _monitor;
        private TrayService? _trayService;
        private BackgroundAnalyzer? _contrastService;
        private readonly DispatcherTimer _timer;

        // --- STATE ---
        private IntPtr _windowHandle;

        // --- CONFIG STATE ---
        private AppConfig _config;
        // Default to Primary Screen initially
        private WinFormsScreen? _targetScreen = WinFormsScreen.PrimaryScreen;

        public MainWindow()
        {
            InitializeComponent();

            // 1. Startup Logic
            try { StartupManager.EnsureAppRunsAtStartup(); }
            catch (Exception ex) { Debug.WriteLine($"[Main] Startup registration failed: {ex.Message}"); }

            // 2. Load Configuration
            _config = ConfigPersistence.Load();

            // 3. Restore Monitor Selection
            try
            {
                // Find screen by saved DeviceName (e.g., \\.\DISPLAY1)
                _targetScreen = WinFormsScreen.AllScreens
                    .FirstOrDefault(s => s.DeviceName == _config.TargetMonitorDeviceName);

                // Fallback if monitor disconnected or config empty
                if (_targetScreen == null)
                {
                    Debug.WriteLine("[Main] Configured monitor not found, defaulting to Primary.");
                    _targetScreen = WinFormsScreen.PrimaryScreen;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main] Error restoring monitor: {ex.Message}");
                _targetScreen = WinFormsScreen.PrimaryScreen;
            }

            // 4. Initialize Services
            InitializePerformanceMonitor();

            try { _contrastService = new BackgroundAnalyzer(); }
            catch (Exception ex) { Debug.WriteLine($"[Main] Contrast service init failed: {ex.Message}"); }

            // 5. Initialize Tray (Pass the current screen name for the checkmark)
            try
            {
                _trayService = new TrayService(
                    onMonitorSelected: HandleMonitorSelection,
                    onExit: () => System.Windows.Application.Current.Shutdown(),
                    initialDeviceName: _targetScreen?.DeviceName ?? ""
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main] Tray service init failed: {ex.Message}");
            }

            // 6. Setup Timer
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnGameLoopTick;

            this.SizeChanged += OnWindowSizeChanged;
            this.Opacity = 0;
            _timer.Start();
        }

        private void HandleMonitorSelection(WinFormsScreen screen)
        {
            try
            {
                Debug.WriteLine($"[Main] Monitor selected: {screen.DeviceName}");
                _targetScreen = screen;

                // SAVE CONFIG
                _config.TargetMonitorDeviceName = screen.DeviceName;
                ConfigPersistence.Save(_config);

                // Update Tray Checkmark
                _trayService?.UpdateSelectedMonitor(screen.DeviceName);

                // Move Window immediately if visible
                if (this.Opacity > 0) SnapToTargetScreen();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main] Error handling monitor selection: {ex.Message}");
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
                // This is critical, so we might want to show a MessageBox or just log heavily
                Debug.WriteLine($"[Main] FATAL: Failed to init counters: {ex}");
                System.Windows.MessageBox.Show($"Performance Counters failed: {ex.Message}");
            }
        }

        // --- THE MAIN LOOP ---

        private void OnGameLoopTick(object? sender, EventArgs e)
        {
            try
            {
                bool isGameActive = GameDetector.IsProcessRunning(TargetProcess);
                bool shouldShow = _config.Mode switch
                {
                    OverlayMode.ForceShow => true,  // Always show
                    OverlayMode.ForceHide => false, // Always hide
                    _ => isGameActive               // Auto (Default)
                };

                if (shouldShow)
                {
                    ShowWindow();
                    UpdateUI();
                    UpdateContrast();
                }
                else
                {
                    HideWindow();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main] Error in game loop: {ex.Message}");
            }
        }

        // --- LOGIC METHODS ---

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
            try
            {
                // 1. Get DPI Scale Factor (Default to 1.0 if not found)
                double dpiX = 1.0;
                double dpiY = 1.0;

                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    dpiX = source.CompositionTarget.TransformToDevice.M11;
                    dpiY = source.CompositionTarget.TransformToDevice.M22;
                }

                // 2. Get Working Area (Physical Pixels)
                var workArea = _targetScreen.WorkingArea;

                // 3. Convert Pixels -> WPF Units (DIPs)
                // Formula: Units = Pixels / Scale
                double leftDips = workArea.Left / dpiX;
                double bottomDips = workArea.Bottom / dpiY;

                // 4. Calculate Position
                // We anchor to the Bottom-Left
                this.Left = leftDips + 10;
                this.Top = bottomDips - this.ActualHeight - 10;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main] Error snapping to screen: {ex.Message}");
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main] Error resizing: {ex.Message}");
            }
        }

        private void UpdateContrast()
        {
            if (_contrastService == null || this.Opacity < 1) return;
            try
            {
                // 1. GET DPI SCALING FACTOR
                // WPF coordinates (Left/Top) are different from Screen Pixels if scaling is > 100%
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget == null) return;

                double dpiX = source.CompositionTarget.TransformToDevice.M11;
                double dpiY = source.CompositionTarget.TransformToDevice.M22;

                // 2. CONVERT TO PHYSICAL PIXELS
                int pixelLeft = (int)(this.Left * dpiX);
                int pixelTop = (int)(this.Top * dpiY);
                int pixelWidth = (int)(this.ActualWidth * dpiX);

                // 3. APPLY "PERISCOPE" OFFSET
                // Look 20 pixels ABOVE the window to avoid capturing the window itself (which looks black)
                // We sample the center-top area
                int sampleX = pixelLeft + (pixelWidth / 2);
                int sampleY = pixelTop - 20;

                // Safety: Ensure we don't capture off-screen (negative Y)
                if (sampleY < 0) sampleY = 0;

                // 4. GET COLOR
                var optimalBrush = _contrastService.GetOptimalTextColor(sampleX, sampleY);

                // 5. APPLY
                System.Windows.Documents.TextElement.SetForeground(Container, optimalBrush);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main] Error updating contrast: {ex.Message}");
            }
        }

        private void UpdateUI()
        {
            if (_monitor == null) return;
            try
            {
                CpuText.Text = $"CPU: {_monitor.GetCpuUsage()}";
                GpuText.Text = _monitor.GetGpuUsage();
                RamText.Text = $"RAM: {_monitor.GetRamUsage()}";
                DiskText.Text = $"Disk: {_monitor.GetTotalDiskSpeed()}";
                NetText.Text = $"Net: {_monitor.GetNetworkSpeed()}";
                DiskListText.Text = _monitor.GetDiskLoadSummary();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main] Error updating UI: {ex.Message}"); 
            }
        }

        // --- WINDOW SETUP (Native Methods) ---

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                _windowHandle = new WindowInteropHelper(this).Handle;

                // 1. Make Transparent & Ghost
                NativeMethods.SetWindowGhostMode(_windowHandle);

                // 2. Register Hotkey (Alt + Shift + H)
                bool success = NativeMethods.RegisterHotKey(_windowHandle, 9000, 5, 0x48);
                if (!success)
                {
                    Debug.WriteLine("[Main] Failed to register hotkey (ALt+Shift+H).");
                }

                // 3. Listen for Hotkey
                HwndSource.FromHwnd(_windowHandle).AddHook(HwndHook);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main] Error in OnSourceInitialized (Native Setup): {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Cleanup Tray Icon
                _trayService?.Dispose();
                // Cleanup Performance Monitor
                _monitor?.Dispose();

                NativeMethods.UnregisterHotKey(_windowHandle, 9000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main] Error during shutdown: {ex.Message}");
            }
            base.OnClosed(e);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == 9000)
            {
                bool isGameActive = GameDetector.IsProcessRunning(TargetProcess);

                // 1. Determine if it is CURRENTLY visible
                bool isCurrentlyVisible = _config.Mode == OverlayMode.ForceShow ||
                                          (_config.Mode == OverlayMode.Auto && isGameActive);

                if (isCurrentlyVisible)
                {
                    // USER INTENT: HIDE
                    // If game is active, we must Force Hide. If game is off, Auto (default hidden) is enough.
                    _config.Mode = isGameActive ? OverlayMode.ForceHide : OverlayMode.Auto;
                }
                else
                {
                    // USER INTENT: SHOW
                    // If game is active, Auto (default shown) is enough. If game is off, we must Force Show.
                    _config.Mode = isGameActive ? OverlayMode.Auto : OverlayMode.ForceShow;
                }

                Debug.WriteLine($"[Main] Toggled. New Mode: {_config.Mode}");

                // Save and Apply Immediately
                ConfigPersistence.Save(_config);
                OnGameLoopTick(null, EventArgs.Empty);

                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
