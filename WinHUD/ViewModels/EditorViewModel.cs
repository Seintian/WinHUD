using WinHUD.Models;
using WinHUD.Models.Nodes;
using WinHUD.Services;

namespace WinHUD.ViewModels
{
    public class EditorViewModel : ObservableObject
    {
        public AppConfig Config { get; private set; }

        public EditorViewModel()
        {
            Config = ConfigPersistence.Load();

            // Bootstrapping: If the user has a blank config, give them a default setup
            if (Config.Layout.Count == 0)
            {
                CreateDefaultLayout();
                Save(); // Save it immediately so MainViewModel sees it
            }
        }

        private void CreateDefaultLayout()
        {
            var stack = new LayoutNode { Direction = LayoutDirection.Vertical, Spacing = 2 };

            stack.Children.Add(new WidgetNode { Type = WidgetType.Cpu, PrefixText = "CPU: " });
            stack.Children.Add(new WidgetNode { Type = WidgetType.Ram, PrefixText = "RAM: " });

            Config.Layout.Add(stack);
        }

        public void Save()
        {
            ConfigPersistence.Save(Config);
        }
    }
}
