using System;
using System.Windows.Threading;
using WinHUD.Models;
using WinHUD.Services;

namespace WinHUD.ViewModels
{
    public class MainViewModel : ObservableObject, IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly DispatcherTimer _timer;

        private HardwareData _currentData = new();
        private bool _isOverlayVisible;

        public AppConfig Config { get; private set; }

        // The UI will bind directly to this property to get its numbers!
        public HardwareData CurrentData
        {
            get => _currentData;
            set
            {
                _currentData = value;
                OnPropertyChanged(); // Tells the UI to update
            }
        }

        public bool IsOverlayVisible
        {
            get => _isOverlayVisible;
            set
            {
                _isOverlayVisible = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            _monitor = new PerformanceMonitor();
            Config = ConfigPersistence.Load();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            // 1. Fetch raw math
            CurrentData = _monitor.GetLatestData();

            // 2. Handle Tri-State Visibility Logic
            bool isGameActive = GameDetector.IsProcessRunning("gameoverlayui64");
            IsOverlayVisible = Config.Mode switch
            {
                OverlayMode.ForceShow => true,
                OverlayMode.ForceHide => false,
                _ => isGameActive
            };
        }

        // Called by MainWindow when Alt+Shift+H is pressed
        public void ToggleOverlayMode()
        {
            bool isGameActive = GameDetector.IsProcessRunning("gameoverlayui64");
            bool isCurrentlyVisible = Config.Mode == OverlayMode.ForceShow ||
                                      (Config.Mode == OverlayMode.Auto && isGameActive);

            Config.Mode = isCurrentlyVisible
                ? (isGameActive ? OverlayMode.ForceHide : OverlayMode.Auto)
                : (isGameActive ? OverlayMode.Auto : OverlayMode.ForceShow);

            ConfigPersistence.Save(Config);
            OnTick(null, EventArgs.Empty); // Force instant visual update
        }

        // Called after the user saves changes in the Editor
        public void ReloadConfig()
        {
            Config = ConfigPersistence.Load();
            OnPropertyChanged(nameof(Config)); // Forces the Layout to redraw
        }

        public void Dispose()
        {
            _timer.Stop();
            _monitor.Dispose();
        }
    }
}
