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
        private double _marginLeft = 0;
        private double _marginTop = 0;

        // Add default spacing so items aren't glued together
        private double _marginRight = 15;
        private double _marginBottom = 0;

        public double MarginLeft { get => _marginLeft; set { _marginLeft = value; OnPropertyChanged(); OnPropertyChanged(nameof(MarginThickness)); } }
        public double MarginTop { get => _marginTop; set { _marginTop = value; OnPropertyChanged(); OnPropertyChanged(nameof(MarginThickness)); } }
        public double MarginRight { get => _marginRight; set { _marginRight = value; OnPropertyChanged(); OnPropertyChanged(nameof(MarginThickness)); } }
        public double MarginBottom { get => _marginBottom; set { _marginBottom = value; OnPropertyChanged(); OnPropertyChanged(nameof(MarginThickness)); } }

        [JsonIgnore]
        public Thickness MarginThickness => new(MarginLeft, MarginTop, MarginRight, MarginBottom);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
