using System.Windows.Threading;
using Serilog;
using WinHUD.Models;
using WinHUD.Models.Nodes;
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

        public HardwareData CurrentData
        {
            get => _currentData;
            set
            {
                _currentData = value;
                OnPropertyChanged();
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
            Log.Information("[MainViewModel] MainViewModel initialized.");

            // If the layout is empty (e.g., first run or old config.json), 
            // build the default layout right here in the overlay engine!
            if (Config.Layout == null || Config.Layout.Count == 0)
            {
                Config.Layout = new List<OverlayNode>();
                var stack = new LayoutNode { Direction = LayoutDirection.Vertical, Spacing = 2 };
                stack.Children.Add(new WidgetNode { Type = WidgetType.Cpu, PrefixText = "CPU: " });
                stack.Children.Add(new WidgetNode { Type = WidgetType.Ram, PrefixText = "RAM: " });

                Config.Layout.Add(stack);
                ConfigPersistence.Save(Config); // Save it so it's persistent
            }

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            CurrentData = _monitor.GetLatestData();

            bool isGameActive = GameDetector.IsProcessRunning("gameoverlayui64");
            IsOverlayVisible = Config.Mode switch
            {
                OverlayMode.ForceShow => true,
                OverlayMode.ForceHide => false,
                _ => isGameActive
            };
        }

        public void ToggleOverlayMode()
        {
            bool isGameActive = GameDetector.IsProcessRunning("gameoverlayui64");
            bool isCurrentlyVisible = Config.Mode == OverlayMode.ForceShow ||
                                      (Config.Mode == OverlayMode.Auto && isGameActive);

            Config.Mode = isCurrentlyVisible
                ? (isGameActive ? OverlayMode.ForceHide : OverlayMode.Auto)
                : (isGameActive ? OverlayMode.Auto : OverlayMode.ForceShow);

            Log.Information("[MainViewModel] Toggling OverlayMode. New Mode: {Mode}", Config.Mode);

            ConfigPersistence.Save(Config);
            OnTick(null, EventArgs.Empty);
        }

        public void ReloadConfig()
        {
            Config = ConfigPersistence.Load();
            Log.Information("[MainViewModel] Configuration reloaded.");
            OnPropertyChanged(nameof(Config));
        }

        public void Dispose()
        {
            _timer.Stop();
            _monitor.Dispose();
        }
    }
}
