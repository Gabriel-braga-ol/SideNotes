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
using System.Windows.Threading;
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
            SaveCurrentNote();

            Note note = viewModel.CreateNote();

            viewModel.SelectedNote = note;
            NotesList.ScrollIntoView(note);

            TrySaveNotes();

            TitleTextBox.Focus();
        }
        
        private void SaveNote_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentNote();
        }

        private void  Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && NotesList.IsKeyboardFocusWithin && viewModel.SelectedNote is Note selectedNote)
            {
                viewModel.Notes.Remove(selectedNote);
                TrySaveNotes();
            }
        }
        private bool isPanelCollapsed = false;

        private void TogglePanel_Click(object sender, RoutedEventArgs e)
        {
            isPanelCollapsed = !isPanelCollapsed;

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

        private void NotesList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject clickedElement)
            {
                var item = ItemsControl.ContainerFromElement(NotesList, clickedElement);

                if (item is ListBoxItem && isPanelCollapsed)
                {
                    isPanelCollapsed = false;
                    NotePanel.Visibility = Visibility.Visible;
                    TogglePanelButton.Content = "▶";

                    Width = 320;
                    Left = SystemParameters.WorkArea.Right - Width;
                }
            }
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                viewModel.SaveNotes();
            }
            catch (Exception ex) when (
                ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBoxResult answer = MessageBox.Show(
                    this,
                    "Não foi possível salvar as notas. \n\n" +
                    "Deseja fechar mesmo assim? " +
                    "As alterações não rgavadas serão perdidas.", "Falha ao salvar", 
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                e.Cancel = answer != MessageBoxResult.Yes;
            }
        }
        private bool TrySaveNotes()
        {
            bool saved = viewModel.TrySaveNotes(out string? errorMessage);

            if (!saved)
            {
                MessageBox.Show(
                    this,
                    errorMessage ?? "Não foi possível salvar as notas.",
                    "Falha ao salvar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            return saved;
        }

        private readonly DispatcherTimer autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        private void SaveCurrentNote()
        {
            if (viewModel.SelectedNote is not null)
            {
                TrySaveNotes();
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
    }
}