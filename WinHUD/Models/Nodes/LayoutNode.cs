using System.Collections.Generic;

namespace WinHUD.Models.Nodes
{
    public enum LayoutDirection
    {
        Vertical,
        Horizontal
    }

    public class LayoutNode : OverlayNode
    {
        public LayoutDirection Direction { get; set; } = LayoutDirection.Vertical;

        // Space between items in this container
        public double Spacing { get; set; } = 5;

        // The blocks contained inside this layout
        public List<OverlayNode> Children { get; set; } = new();
    }
}
