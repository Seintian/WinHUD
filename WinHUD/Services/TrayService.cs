using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace WinHUD.Services
{
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly Action<Screen> _onMonitorSelected;
        private readonly Action _onExit;

        // Track the currently selected monitor ID (DeviceName)
        private string _selectedDeviceName;

        public TrayService(Action<Screen> onMonitorSelected, Action onExit, string initialDeviceName)
        {
            _onMonitorSelected = onMonitorSelected;
            _onExit = onExit;
            _selectedDeviceName = initialDeviceName;

            _notifyIcon = new NotifyIcon
            {
                Visible = false, // Will set true after setup
                Text = "WinHUD"
            };

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("WinHUD.assets.logo-w-bg.ico");

                _notifyIcon.Icon = stream != null ? new Icon(stream) : SystemIcons.Application;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tray] Error loading icon: {ex.Message}");
                _notifyIcon.Icon = SystemIcons.Application;
            }

            // Build the Right-Click Menu
            RebuildContextMenu();
            _notifyIcon.Visible = true;
        }

        public void UpdateSelectedMonitor(string deviceName)
        {
            _selectedDeviceName = deviceName;
            RebuildContextMenu();
        }

        private void RebuildContextMenu()
        {
            try
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

                    // Logic: Check the item if it matches our config
                    if (screen.DeviceName == _selectedDeviceName)
                    {
                        item.Checked = true;
                    }

                    // Click Action: Trigger callback with this specific screen
                    item.Click += (s, e) =>
                    {
                        try
                        {
                            _onMonitorSelected?.Invoke(screen);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Tray] Error in monitor selection callback: {ex.Message}");
                        }
                    };

                    contextMenu.Items.Add(item);
                }

                // 3. Add Exit
                contextMenu.Items.Add(new ToolStripSeparator());
                var exitItem = new ToolStripMenuItem("Exit");
                exitItem.Click += (s, e) =>
                {
                    try
                    {
                        _onExit?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Tray] Error in Exit callback: {ex.Message}");
                    }
                };
                contextMenu.Items.Add(exitItem);

                _notifyIcon.ContextMenuStrip = contextMenu;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tray] Error building context menu: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tray] Error disposing icon: {ex.Message}");
            }
        }
    }
}
