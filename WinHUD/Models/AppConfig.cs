using System.Collections.Generic;
using WinHUD.Models.Nodes;

namespace WinHUD.Models
{
    public enum OverlayMode
    {
        Auto = 0,
        ForceShow = 1,
        ForceHide = 2
    }

    public class AppConfig
    {
        public string TargetMonitorDeviceName { get; set; } = string.Empty;
        public OverlayMode Mode { get; set; } = OverlayMode.Auto;

        // The user's custom layout tree
        public List<OverlayNode> Layout { get; set; } = new();
    }
}
