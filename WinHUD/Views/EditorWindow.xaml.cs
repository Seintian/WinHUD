using System;
using System.Windows;
using System.Windows.Controls;
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

        public EditorWindow()
        {
            InitializeComponent();
            _viewModel = new EditorViewModel();
            this.DataContext = _viewModel;
        }

        private void ToggleOrientation_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ToggleOrientation();
        }

        private void PaletteButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is Button btn && btn.Tag is WidgetType type)
                DragDrop.DoDragDrop(btn, type.ToString(), DragDropEffects.Copy);
        }

        private void ActiveBlock_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element && element.DataContext is WidgetNode node)
                DragDrop.DoDragDrop(element, node, DragDropEffects.Move);
        }

        // 1. Dropping ON an existing block (Squeezing it in between)
        private void ActiveBlock_Drop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement targetElement && targetElement.DataContext is WidgetNode targetNode)
            {
                int targetIndex = _viewModel.MainLayout.Children.IndexOf(targetNode);

                // Smart insertion: Did they drop on the left half or right half of the block?
                Point dropPos = e.GetPosition(targetElement);
                if (dropPos.X > targetElement.ActualWidth / 2)
                    targetIndex++; // Insert AFTER the block

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

                        // Prevent index shifting when moving left-to-right
                        if (sourceIndex < targetIndex) targetIndex--;

                        children.Move(sourceIndex, targetIndex);
                    }
                }
                e.Handled = true; // Tell WPF we handled it so the canvas drop event doesn't fire too!
            }
        }

        // 2. Dropping into the empty space on the Canvas
        private void ActiveLayout_Drop(object sender, DragEventArgs e)
        {
            if (e.Handled) return; // Prevent double-drops

            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                // Spawning a new block from the Palette
                string typeStr = (string)e.Data.GetData(DataFormats.StringFormat);
                if (Enum.TryParse(typeStr, out WidgetType type))
                    _viewModel.AddWidget(type); // Appends to the end
            }
            else if (e.Data.GetDataPresent(typeof(WidgetNode)))
            {
                // Moving an existing block to the end of the list
                var sourceNode = (WidgetNode)e.Data.GetData(typeof(WidgetNode));
                var children = _viewModel.MainLayout.Children;

                int sourceIndex = children.IndexOf(sourceNode);
                if (sourceIndex >= 0 && sourceIndex != children.Count - 1)
                {
                    children.Move(sourceIndex, children.Count - 1);
                }
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
    }
}
