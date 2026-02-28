using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using WinHUD.Core;
using WinHUD.Services;
using WinHUD.ViewModels;
using Serilog;
using WinFormsScreen = System.Windows.Forms.Screen;
using Application = System.Windows.Application;

namespace WinHUD.Views
{
    public partial class MainWindow : Window
    {
        private IntPtr _windowHandle;
        private readonly MainViewModel _viewModel;
        private readonly TrayService? _trayService;
        private EditorWindow? _editorWindow;

        // Contrast Services
        private readonly BackgroundAnalyzer _contrastService;
        private readonly DispatcherTimer _contrastTimer;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            Log.Information("[MainWindow] MainWindow initialized.");

            // Initialize Dynamic Contrast
            _contrastService = new BackgroundAnalyzer();
            _contrastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _contrastTimer.Tick += UpdateContrast;
            _contrastTimer.Tick += KeepTopMost;
            _contrastTimer.Start();

            _trayService = new TrayService(
                onMonitorSelected: HandleMonitorSelection,
                onExit: () => Application.Current.Shutdown(),
                onOpenEditor: OpenEditorWindow,
                initialDeviceName: _viewModel.Config.TargetMonitorDeviceName
            );

            SizeChanged += (s, e) => SnapToTargetScreen();
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsOverlayVisible) && _viewModel.IsOverlayVisible)
                {
                    SnapToTargetScreen();
                }
            };
        }

        private void KeepTopMost(object? sender, EventArgs e)
        {
            // Don't waste CPU cycles if the overlay is currently hidden
            if (Opacity < 1 || !_viewModel.IsOverlayVisible) return;
            
            // Aggressively re-assert Z-Order every 500ms!
            // SWP_NOACTIVATE ensures it never steals focus from the user's game.
            if (_windowHandle != IntPtr.Zero)
            {
                NativeMethods.SetWindowPos(
                    _windowHandle, 
                    NativeMethods.HWND_TOPMOST, 
                    0, 0, 0, 0, 
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
            }
        }

        private void UpdateContrast(object? sender, EventArgs e)
        {
            if (Opacity < 1 || !_viewModel.IsOverlayVisible) return;
            try
            {
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget == null) return;

                double dpiX = source.CompositionTarget.TransformToDevice.M11;
                double dpiY = source.CompositionTarget.TransformToDevice.M22;

                int pixelLeft = (int)(Left * dpiX);
                int pixelTop = (int)(Top * dpiY);
                int pixelWidth = (int)(ActualWidth * dpiX);
                int pixelHeight = (int)(ActualHeight * dpiY);

                // Sample dead center of the overlay
                int sampleX = pixelLeft + (pixelWidth / 2);
                int sampleY = pixelTop + (pixelHeight / 2);

                var optimalBrush = _contrastService.GetOptimalTextColor(sampleX, sampleY);

                // Apply this color to EVERY text block in the window simultaneously
                System.Windows.Documents.TextElement.SetForeground(this, optimalBrush);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Main] Error updating contrast: {Message}", ex.Message);
            }
        }

        private void HandleMonitorSelection(WinFormsScreen screen)
        {
            _viewModel.Config.TargetMonitorDeviceName = screen.DeviceName;
            ConfigPersistence.Save(_viewModel.Config);
            _trayService?.UpdateSelectedMonitor(screen.DeviceName);
            SnapToTargetScreen();
        }

        private void OpenEditorWindow()
        {
            if (_editorWindow != null && _editorWindow.IsLoaded)
            {
                _editorWindow.Activate();
                return;
            }

            _editorWindow = new EditorWindow();
            _editorWindow.Closed += (s, e) =>
            {
                _viewModel.ReloadConfig();
                _editorWindow = null;
            };
            _editorWindow.Show();
        }

        private void SnapToTargetScreen()
        {
            if (!_viewModel.IsOverlayVisible) return;
            try
            {
                var screen = WinFormsScreen.AllScreens.FirstOrDefault(s => s.DeviceName == _viewModel.Config.TargetMonitorDeviceName) ?? WinFormsScreen.PrimaryScreen;
                if (screen == null) return;

                double dpiX = 1.0, dpiY = 1.0;
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    dpiX = source.CompositionTarget.TransformToDevice.M11;
                    dpiY = source.CompositionTarget.TransformToDevice.M22;
                }

                var workArea = screen.WorkingArea;
                Left = (workArea.Left / dpiX) + 10;
                Top = (workArea.Bottom / dpiY) - ActualHeight - 10;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Main] Error snapping: {Message}", ex.Message);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowHandle = new WindowInteropHelper(this).Handle;
            NativeMethods.SetWindowGhostMode(_windowHandle);
            NativeMethods.RegisterHotKey(_windowHandle, 9000, 5, 0x48);
            HwndSource.FromHwnd(_windowHandle).AddHook(HwndHook);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == 9000)
            {
                Log.Debug("[MainWindow] Hotkey pressed, toggling overlay mode.");
                _viewModel.ToggleOverlayMode();
                handled = true;
            }
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _contrastTimer.Stop();
            _trayService?.Dispose();
            _viewModel.Dispose();
            NativeMethods.UnregisterHotKey(_windowHandle, 9000);
            base.OnClosed(e);
        }
    }
}
