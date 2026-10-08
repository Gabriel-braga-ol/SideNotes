using System.Windows;
using SideNotes.ViewModels;

namespace SideNotes.Views
{
    public partial class DeskWindow : Window
    {
        public DeskWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}