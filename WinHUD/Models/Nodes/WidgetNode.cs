namespace WinHUD.Models.Nodes
{
    public enum WidgetType { Cpu, Gpu, Ram, Disk, DiskList, Network }

    public class WidgetNode : OverlayNode
    {
        private string _prefixText = "";
        private double _fontSize = 12;

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
    }
}
