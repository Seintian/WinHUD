using System.Text.Json.Serialization;
using System.Windows;

namespace WinHUD.Models.Nodes
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(LayoutNode), "layout")]
    [JsonDerivedType(typeof(WidgetNode), "widget")]
    public abstract class OverlayNode
    {
        public double MarginLeft { get; set; } = 0;
        public double MarginTop { get; set; } = 0;
        public double MarginRight { get; set; } = 0;
        public double MarginBottom { get; set; } = 0;

        // This provides a single object for XAML to bind to.
        // [JsonIgnore] ensures we don't save WPF-specific objects to config.json.
        [JsonIgnore]
        public Thickness MarginThickness => new Thickness(MarginLeft, MarginTop, MarginRight, MarginBottom);
    }
}
