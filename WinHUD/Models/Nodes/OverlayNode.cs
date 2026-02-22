using System.Text.Json.Serialization;

namespace WinHUD.Models.Nodes
{
    // This tells the JSON serializer to tag objects with "$type" so it knows 
    // exactly which derived class to recreate when loading config.json
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(LayoutNode), "layout")]
    [JsonDerivedType(typeof(WidgetNode), "widget")]
    public abstract class OverlayNode
    {
        // Common properties every UI block shares
        public double MarginLeft { get; set; } = 0;
        public double MarginTop { get; set; } = 0;
        public double MarginRight { get; set; } = 0;
        public double MarginBottom { get; set; } = 0;
    }
}
