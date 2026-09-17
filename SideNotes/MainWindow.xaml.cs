using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;

namespace SideNotes
{
    
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel = new MainViewModel();
        public MainWindow()
        {
            InitializeComponent();

            viewModel.SaveFailed += ViewModel_SaveFailed;
            Closed += MainWindow_Closed;
            
            DataContext = viewModel;

            Left = SystemParameters.WorkArea.Right - Width;
            Top = SystemParameters.WorkArea.Top;
        }
        private void AddNote_Click(object sender, RoutedEventArgs e)
        {
            Note note = viewModel.CreateNote();
            
            NotesList.ScrollIntoView(note);

            TitleTextBox.Focus();
        }
        
        private void SaveNote_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SaveCurrentNote();
        }

        private void  Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && NotesList.IsKeyboardFocusWithin && viewModel.SelectedNote is not null)
            {
                viewModel.DeleteSelectedNote();
            }
        }
        private bool isPanelCollapsed = false;

        private void TogglePanel_Click(object sender, RoutedEventArgs e)
        {
            SetPanelCollapsed(!isPanelCollapsed);
        }

        private void NotesList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject clickedElement)
            {
                var item = ItemsControl.ContainerFromElement(NotesList, clickedElement);

                if (item is ListBoxItem && isPanelCollapsed)
                {
                    SetPanelCollapsed(false);
                }
            }
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (!viewModel.TrySaveNotes(out _)) //out _ descarta a mensagem devolvida pelo método
            {
                MessageBoxResult answer = MessageBox.Show(
                    this,
                    "Não foi possível salvar as notas. \n\n" +
                    "Deseja fechar mesmo assim? " +
                    "As alterações não gravadas serão perdidas.", "Falha ao salvar", 
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                e.Cancel = answer != MessageBoxResult.Yes;
            }
        }

        private void ViewModel_SaveFailed(string message)
        {
            MessageBox.Show(
                this,
                message,
                "Falha ao salvar",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            viewModel.StopAutoSave();
            viewModel.SaveFailed -= ViewModel_SaveFailed;
        }

        private void SetPanelCollapsed(bool collapsed)
        {
            isPanelCollapsed = collapsed;
            
            if (isPanelCollapsed)
            {
                NotePanel.Visibility = Visibility.Collapsed;
                TogglePanelButton.Content = "◀";
                Width = 100;
                Left = SystemParameters.WorkArea.Right - Width;
            }
            else
            {
                NotePanel.Visibility = Visibility.Visible;
                TogglePanelButton.Content = "▶";
                Width = 320;
                Left = SystemParameters.WorkArea.Right - Width;
            }
        }
    }
}