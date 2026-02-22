using System.Collections.ObjectModel;

namespace WinHUD.Models.Nodes
{
    public enum LayoutDirection { Vertical, Horizontal }

    public class LayoutNode : OverlayNode
    {
        private LayoutDirection _direction = LayoutDirection.Vertical;

        public LayoutDirection Direction
        {
            get => _direction;
            set { _direction = value; OnPropertyChanged(); }
        }

        public double Spacing { get; set; } = 5;
        public ObservableCollection<OverlayNode> Children { get; set; } = new();
    }
}
