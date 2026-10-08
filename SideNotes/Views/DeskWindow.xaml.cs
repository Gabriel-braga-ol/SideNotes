using System.Windows;
using System.Windows.Controls;

namespace SideNotes.Views
{
    public partial class DeskWindow : Window
    {
        public DeskWindow()
        {
            InitializeComponent();
        }

        private void TestClick_Click(
            object sender,
            RoutedEventArgs e)
        {
            ((Button)sender).Content = "Clique recebido!";
        }
    }
}