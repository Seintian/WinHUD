using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using WinHUD.Core;
using WinHUD.ViewModels;
using WinFormsScreen = System.Windows.Forms.Screen;

namespace WinHUD.Views
{
    public partial class MainWindow : Window
    {
        private IntPtr _windowHandle;
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // 1. Assign the Engine (ViewModel)
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;

            // 2. Snap to screen when sizes change
            this.SizeChanged += (s, e) => SnapToTargetScreen();
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsOverlayVisible) && _viewModel.IsOverlayVisible)
                {
                    SnapToTargetScreen();
                }
            };
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
            NativeMethods.RegisterHotKey(_windowHandle, 9000, 5, 0x48); // Alt+Shift+H
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
            _viewModel.Dispose();
            NativeMethods.UnregisterHotKey(_windowHandle, 9000);
            base.OnClosed(e);
        }
    }
}
