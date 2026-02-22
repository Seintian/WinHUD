namespace WinHUD.Models.Nodes
{
    public enum WidgetType { Cpu, Gpu, Ram, Disk, DiskList, Network }

    public class WidgetNode : OverlayNode
    {
        private string _prefixText = "";
        private double _fontSize = 12;
        private double? _minWidth;

        public WidgetType Type { get; set; }

        public double FontSize
        {
            get => _fontSize;
            set { _fontSize = value; OnPropertyChanged(); }
        }

        public string PrefixText
        {
            get => _prefixText;
            set { _prefixText = value; OnPropertyChanged(); }
        }

        // Generous minimums so heavy hitters don't bounce, but DiskList can expand!
        public double MinWidth
        {
            get => _minWidth ?? (Type == WidgetType.Network || Type == WidgetType.Disk ? 260 : 80);
            set { _minWidth = value; OnPropertyChanged(); }
        }
    }
}
