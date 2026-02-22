using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using WinHUD.Core;
using WinHUD.Services;
using WinHUD.ViewModels;
using WinFormsScreen = System.Windows.Forms.Screen;
using Application = System.Windows.Application;

namespace WinHUD.Views
{
    public partial class MainWindow : Window
    {
        private IntPtr _windowHandle;
        private readonly MainViewModel _viewModel;

        // Keep track of our Tray and Editor instances
        private TrayService? _trayService;
        private EditorWindow? _editorWindow;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;

            // Initialize the Tray Service and hook up the callbacks
            _trayService = new TrayService(
                onMonitorSelected: HandleMonitorSelection,
                onExit: () => Application.Current.Shutdown(),
                onOpenEditor: OpenEditorWindow,
                initialDeviceName: _viewModel.Config.TargetMonitorDeviceName
            );

            this.SizeChanged += (s, e) => SnapToTargetScreen();
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsOverlayVisible) && _viewModel.IsOverlayVisible)
                {
                    SnapToTargetScreen();
                }
            };
        }

        private void HandleMonitorSelection(WinFormsScreen screen)
        {
            _viewModel.Config.TargetMonitorDeviceName = screen.DeviceName;
            ConfigPersistence.Save(_viewModel.Config);
            _trayService?.UpdateSelectedMonitor(screen.DeviceName);
            SnapToTargetScreen();
        }

        // Open the Editor and reload layout when it closes
        private void OpenEditorWindow()
        {
            // Don't open a second editor if one is already open
            if (_editorWindow != null && _editorWindow.IsLoaded)
            {
                _editorWindow.Activate();
                return;
            }

            _editorWindow = new EditorWindow();
            _editorWindow.Closed += (s, e) =>
            {
                // When you close the editor, 
                // the overlay immediately updates its UI blocks!
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
                var screen = WinFormsScreen.AllScreens.FirstOrDefault(s => s.DeviceName == _viewModel.Config.TargetMonitorDeviceName)
                             ?? WinFormsScreen.PrimaryScreen;

                if (screen == null) return;

                double dpiX = 1.0, dpiY = 1.0;
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    dpiX = source.CompositionTarget.TransformToDevice.M11;
                    dpiY = source.CompositionTarget.TransformToDevice.M22;
                }

                var workArea = screen.WorkingArea;
                this.Left = (workArea.Left / dpiX) + 10;
                this.Top = (workArea.Bottom / dpiY) - this.ActualHeight - 10;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main] Error snapping: {ex.Message}");
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
                _viewModel.ToggleOverlayMode();
                handled = true;
            }
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _trayService?.Dispose();
            _viewModel.Dispose();
            NativeMethods.UnregisterHotKey(_windowHandle, 9000);
            base.OnClosed(e);
        }
    }
}
