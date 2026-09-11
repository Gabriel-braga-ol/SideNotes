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
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
            
            TitleTextBox.TextChanged += NoteTextChanged;
            ContentTextBox.TextChanged += NoteTextChanged;
            
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
        private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            autoSaveTimer.Stop();
            
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is Note previousNote && viewModel.Notes.Contains(previousNote))
            {
                TrySaveNotes();
            }
            
            if (NotesList.SelectedItem is Note selectedNote)
            {
                NotePanel.Background = (Brush)new BrushConverter().ConvertFromString(selectedNote.Color);
            }
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
                NoteStorage.Save(viewModel.Notes.ToList());
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
            try
            {
                NoteStorage.Save(viewModel.Notes.ToList());
                return true;
            }
            catch (Exception ex) when (
                ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(
                    this,
                    "Não foi possível salvar as notas no arquivo.\n\n" +
                    "Sua edição continua na memória. " +
                    "Mantenha o aplicativo aberto e tente salvar novamente.",
                    "Falha ao salvar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }
        }

        private readonly DispatcherTimer autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            autoSaveTimer.Stop();
            SaveCurrentNote();
        }

        private void NoteTextChanged(object sender, TextChangedEventArgs e)
        {
            autoSaveTimer.Stop();

            if (viewModel.SelectedNote is null)
            {
                return;
            }
            
            autoSaveTimer.Start();
        }

        private void SaveCurrentNote()
        {
            autoSaveTimer.Stop();
            
            if (viewModel.SelectedNote is not null)
            {
                TrySaveNotes();
            }
        }
    }
}