namespace WinHUD.Models.Nodes
{
    public enum WidgetType
    {
        Cpu,
        Gpu,
        Ram,
        Disk,
        Network
    }

    public class WidgetNode : OverlayNode
    {
        public WidgetType Type { get; set; }

        // Can add customization here
        //public double FontSize { get; set; } = 14;
        public string PrefixText { get; set; } = ""; // e.g., "CPU: "
    }
}
