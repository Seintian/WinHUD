using System.Windows;
using System.Windows.Input;
using WinHUD.Models.Nodes;
using WinHUD.ViewModels;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using Button = System.Windows.Controls.Button;
using DragDropEffects = System.Windows.DragDropEffects;
using DataFormats = System.Windows.DataFormats;
using Point = System.Windows.Point;

namespace WinHUD.Views
{
    public partial class EditorWindow : Window
    {
        private readonly EditorViewModel _viewModel;

        // Track exactly where the mouse first clicked down
        private Point? _dragStartPoint;

        public EditorWindow()
        {
            InitializeComponent();
            _viewModel = new EditorViewModel();
            this.DataContext = _viewModel;
            Serilog.Log.Information("[EditorWindow] EditorWindow initialized.");
        }

        // Record the starting coordinates
        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        // Enforce minimum drag distance for the Palette buttons
        private void PaletteButton_MouseMove(object sender, MouseEventArgs e)
        {
            // Using "is Point startPoint" securely unwraps the nullable without warnings!
            if (e.LeftButton == MouseButtonState.Pressed && sender is Button btn && btn.Tag is WidgetType type && _dragStartPoint is Point startPoint)
            {
                Point mousePos = e.GetPosition(null);

                // Use the unwrapped startPoint directly
                Vector diff = startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _dragStartPoint = null;
                    DragDrop.DoDragDrop(btn, type.ToString(), DragDropEffects.Copy);
                }
            }
        }

        // Enforce minimum drag distance for the active blocks
        private void ActiveBlock_MouseMove(object sender, MouseEventArgs e)
        {
            // Using "is Point startPoint" securely unwraps the nullable without warnings!
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element && element.DataContext is WidgetNode node && _dragStartPoint is Point startPoint)
            {
                Point mousePos = e.GetPosition(null);

                // Use the unwrapped startPoint directly
                Vector diff = startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _dragStartPoint = null;
                    DragDrop.DoDragDrop(element, node, DragDropEffects.Move);
                }
            }
        }

        private void ActiveBlock_Drop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement targetElement && targetElement.DataContext is WidgetNode targetNode)
            {
                int targetIndex = _viewModel.MainLayout.Children.IndexOf(targetNode);
                Point dropPos = e.GetPosition(targetElement);
                if (dropPos.X > targetElement.ActualWidth / 2)
                    targetIndex++;

                if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string typeStr = (string)e.Data.GetData(DataFormats.StringFormat);
                    if (Enum.TryParse(typeStr, out WidgetType type))
                        _viewModel.AddWidget(type, targetIndex);
                }
                else if (e.Data.GetDataPresent(typeof(WidgetNode)))
                {
                    var sourceNode = (WidgetNode)e.Data.GetData(typeof(WidgetNode));
                    if (sourceNode != targetNode)
                    {
                        var children = _viewModel.MainLayout.Children;
                        int sourceIndex = children.IndexOf(sourceNode);
                        if (sourceIndex < targetIndex) targetIndex--;
                        children.Move(sourceIndex, targetIndex);
                    }
                }
                e.Handled = true;
            }
        }

        private void ActiveLayout_Drop(object sender, DragEventArgs e)
        {
            if (e.Handled) return;

            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string typeStr = (string)e.Data.GetData(DataFormats.StringFormat);
                if (Enum.TryParse(typeStr, out WidgetType type))
                    _viewModel.AddWidget(type);

                e.Handled = true;
            }
        }

        private void RemoveWidget_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OverlayNode node) _viewModel.RemoveWidget(node);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Save();
            this.Close();
        }

        private void ToggleOrientation_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ToggleOrientation();
        }
    }
}
