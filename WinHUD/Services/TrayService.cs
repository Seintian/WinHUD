using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using WinFormsScreen = System.Windows.Forms.Screen;

namespace WinHUD.Services
{
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;

        private readonly Action<WinFormsScreen> _onMonitorSelected;
        private readonly Action _onExit;

        // Action to trigger the Editor
        private readonly Action _onOpenEditor;

        public TrayService(Action<WinFormsScreen> onMonitorSelected, Action onExit, Action onOpenEditor, string initialDeviceName)
        {
            _onMonitorSelected = onMonitorSelected;
            _onExit = onExit;
            _onOpenEditor = onOpenEditor;

            _contextMenu = new ContextMenuStrip();
            BuildMenu(initialDeviceName);

            _notifyIcon = new NotifyIcon
            {
                Visible = true,
                Text = "WinHUD Overlay",
                ContextMenuStrip = _contextMenu
            };

            // Load embedded icon
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("WinHUD.assets.logo-w-bg.ico");
                if (stream != null) _notifyIcon.Icon = new Icon(stream);
                else _notifyIcon.Icon = SystemIcons.Application;
            }
            catch { _notifyIcon.Icon = SystemIcons.Application; }
        }

        private void BuildMenu(string currentDeviceName)
        {
            _contextMenu.Items.Clear();

            // 1. Edit Layout Button
            var editItem = new ToolStripMenuItem("Edit Layout...", null, (s, e) => _onOpenEditor());
            editItem.Font = new Font(editItem.Font, FontStyle.Bold); // Make it stand out
            _contextMenu.Items.Add(editItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // 2. Monitor Selection
            var monitorItem = new ToolStripMenuItem("Display on Monitor");
            foreach (var screen in WinFormsScreen.AllScreens)
            {
                string label = screen.Primary ? $"Primary ({screen.Bounds.Width}x{screen.Bounds.Height})"
                                              : $"Display ({screen.Bounds.Width}x{screen.Bounds.Height})";

                var item = new ToolStripMenuItem(label, null, (s, e) => _onMonitorSelected(screen))
                {
                    Tag = screen.DeviceName,
                    Checked = screen.DeviceName == currentDeviceName
                };
                monitorItem.DropDownItems.Add(item);
            }
            _contextMenu.Items.Add(monitorItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // 3. Exit
            _contextMenu.Items.Add(new ToolStripMenuItem("Exit", null, (s, e) => _onExit()));
        }

        public void UpdateSelectedMonitor(string deviceName)
        {
            BuildMenu(deviceName);
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _contextMenu.Dispose();
        }
    }
}
