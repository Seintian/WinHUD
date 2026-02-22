namespace WinHUD.Models.Nodes
{
    public enum WidgetType
    {
        Cpu,
        Gpu,
        Ram,
        Disk,
        DiskList,
        Network
    }

    public class WidgetNode : OverlayNode
    {
        public WidgetType Type { get; set; }
        public double FontSize { get; set; } = 14;
        public string PrefixText { get; set; } = "";
    }
}
