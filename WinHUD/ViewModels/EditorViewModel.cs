using WinHUD.Models;
using WinHUD.Models.Nodes;
using WinHUD.Services;

namespace WinHUD.ViewModels
{
    public class EditorViewModel : ObservableObject
    {
        public AppConfig Config { get; private set; }
        public IEnumerable<WidgetType> AvailableWidgets => Enum.GetValues<WidgetType>();
        public LayoutNode MainLayout => Config.Layout.OfType<LayoutNode>().FirstOrDefault() ?? CreateDefaultLayout();

        public EditorViewModel()
        {
            Config = ConfigPersistence.Load();
            if (Config.Layout.Count == 0) CreateDefaultLayout();
            Serilog.Log.Information("[EditorViewModel] EditorViewModel initialized.");
        }

        public void ToggleOrientation()
        {
            MainLayout.Direction = MainLayout.Direction == LayoutDirection.Vertical
                                 ? LayoutDirection.Horizontal
                                 : LayoutDirection.Vertical;
            Serilog.Log.Information("[EditorViewModel] Orientation toggled to {Direction}", MainLayout.Direction);
        }

        private LayoutNode CreateDefaultLayout()
        {
            var stack = new LayoutNode();
            Config.Layout.Add(stack);
            return stack;
        }

        // Insert at a specific index to allow squeezing between blocks
        public void AddWidget(WidgetType type, int insertIndex = -1)
        {
            var widget = new WidgetNode
            {
                Type = type,
                // DiskList gets no prefix by default!
                PrefixText = type == WidgetType.DiskList ? "" : $"{type}: "
            };

            if (insertIndex >= 0 && insertIndex <= MainLayout.Children.Count)
                MainLayout.Children.Insert(insertIndex, widget);
            else
                MainLayout.Children.Add(widget); // Fallback: append to end
                
            Serilog.Log.Information("[EditorViewModel] Added Widget {Type} at index {Index}", type, insertIndex);
        }

        public void RemoveWidget(OverlayNode node)
        {
            MainLayout.Children.Remove(node);
            Serilog.Log.Information("[EditorViewModel] Removed Widget from Layout.");
        }
        
        public void Save()
        {
            ConfigPersistence.Save(Config);
            Serilog.Log.Information("[EditorViewModel] Configuration saved.");
        }
    }
}
