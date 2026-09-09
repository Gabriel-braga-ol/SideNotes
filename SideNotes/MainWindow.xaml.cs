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

namespace SideNotes
{
    
    public partial class MainWindow : Window
    {
        private readonly string[] noteColors =
        {
            "#B8E6D0",
            "#F7D794",
            "#D8C4F1",
            "#F3B3B3",
            "#A8D8EA"
        };
        
        private List<Note> notes = NoteStorage.Load();
        public MainWindow()
        {
            InitializeComponent();
            foreach (Note note in notes)
            {
                NotesList.Items.Add(note);
            }
            
            NotesCountText.Text =  $"Notas salvas: {notes.Count}";

            Left = SystemParameters.WorkArea.Right - Width;
            Top = SystemParameters.WorkArea.Top;
        }
        private void AddNote_Click(object sender, RoutedEventArgs e)
        {
            SaveNote_Click(sender, e);
            
            Note note = new Note
            {
                Title = $"Nova nota {notes.Count + 1}",
                Content = "",
                Color = noteColors[notes.Count % noteColors.Length]
            };

            notes.Add(note);
            NotesList.Items.Add(note);
            
            NotesList.SelectedItem = note;
            NotesList.ScrollIntoView(note);

            TrySaveNotes();

            NotesCountText.Text = $"Notas salvas: {notes.Count}";
            TitleTextBox.Focus();
        }
        private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is Note previousNote && notes.Contains(previousNote))
            {
                previousNote.Title = TitleTextBox.Text;
                previousNote.Content = ContentTextBox.Text;

                TrySaveNotes();
            }
            

            if (NotesList.SelectedItem is Note selectedNote)
            {
                TitleTextBox.Text = selectedNote.Title;
                ContentTextBox.Text = selectedNote.Content;
                
                NotePanel.Background = (Brush)new BrushConverter().ConvertFromString(selectedNote.Color);
            }
        }
        private void SaveNote_Click(object sender, RoutedEventArgs e)
        {
            if (NotesList.SelectedItem is Note selectedNote)
            {
                selectedNote.Title = TitleTextBox.Text;
                selectedNote.Content = ContentTextBox.Text;

                TrySaveNotes();

                NotesList.Items.Refresh();
            }
        }

        private void  Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && NotesList.IsKeyboardFocusWithin && NotesList.SelectedItem is Note selectedNote)
            {
                notes.Remove(selectedNote);
                NotesList.Items.Remove(selectedNote);
                TrySaveNotes();

                TitleTextBox.Text = "";
                ContentTextBox.Text = "";
                NotesCountText.Text = $"Notas salvas: {notes.Count}";
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
            if (NotesList.SelectedItem is Note selectedNote)
            {
                selectedNote.Title = TitleTextBox.Text;
                selectedNote.Content = ContentTextBox.Text;
            }

            try
            {
                NoteStorage.Save(notes);
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
                NoteStorage.Save(notes);
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
    }
}