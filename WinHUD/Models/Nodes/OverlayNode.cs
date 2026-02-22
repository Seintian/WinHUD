using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows;

namespace WinHUD.Models.Nodes
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(LayoutNode), "layout")]
    [JsonDerivedType(typeof(WidgetNode), "widget")]
    public abstract class OverlayNode : INotifyPropertyChanged
    {
        public double MarginLeft { get; set; } = 0;
        public double MarginTop { get; set; } = 0;
        public double MarginRight { get; set; } = 0;
        public double MarginBottom { get; set; } = 0;

        [JsonIgnore]
        public Thickness MarginThickness => new Thickness(MarginLeft, MarginTop, MarginRight, MarginBottom);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
