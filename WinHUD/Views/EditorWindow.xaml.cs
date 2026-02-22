using System.Windows;
using WinHUD.ViewModels;
using MessageBox = System.Windows.MessageBox;

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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Save();
            MessageBox.Show("Saved config.json! Restart the app or implement a cross-window messenger to see the live update on the overlay.");
        }
    }
}
