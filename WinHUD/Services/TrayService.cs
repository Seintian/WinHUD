using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinHUD.Services
{
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly Action<Screen> _onMonitorSelected;
        private readonly Action _onExit;

        public TrayService(Action<Screen> onMonitorSelected, Action onExit)
        {
            _onMonitorSelected = onMonitorSelected;
            _onExit = onExit;

            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "logo-w-bg.ico");

            // Initialize the Tray Icon
            _notifyIcon = new NotifyIcon
            {
                // Load custom icon if available, otherwise fallback to default application icon
                Icon = System.IO.File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application,
                Visible = true,
                Text = "WinHUD"
            };

            // Build the Right-Click Menu
            RebuildContextMenu();
        }

        private void RebuildContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            // 1. Add Header
            var header = new ToolStripMenuItem("Select Monitor") { Enabled = false };
            contextMenu.Items.Add(header);
            contextMenu.Items.Add(new ToolStripSeparator());

            // 2. Add Dynamic List of Screens
            int index = 1;
            foreach (var screen in Screen.AllScreens)
            {
                // Example: "Monitor 1 (1920x1080) - Primary"
                string label = $"Monitor {index++} ({screen.Bounds.Width}x{screen.Bounds.Height})";
                if (screen.Primary) label += " - Primary";

                var item = new ToolStripMenuItem(label);

                // Click Action: Trigger callback with this specific screen
                item.Click += (s, e) => _onMonitorSelected?.Invoke(screen);

                contextMenu.Items.Add(item);
            }

            // 3. Add Exit
            contextMenu.Items.Add(new ToolStripSeparator());
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => _onExit?.Invoke();
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
