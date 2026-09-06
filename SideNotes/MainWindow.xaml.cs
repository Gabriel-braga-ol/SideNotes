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

namespace SideNotes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Note> notes = new List<Note>();
        public MainWindow()
        {
            InitializeComponent();

            Left = SystemParameters.WorkArea.Right - Width;
            Top = SystemParameters.WorkArea.Top;
        }
        private void AddNote_Click(object sender, RoutedEventArgs e)
        {
            Note note = new Note
            {
                Title = TitleTextBox.Text,
                Content = ContentTextBox.Text
            };

            notes.Add(note);

            NotesList.Items.Add(note);

            TitleTextBox.Text = $"Nova nota {notes.Count + 1}";
            ContentTextBox.Text = "";

            NotesCountText.Text = $"Notas salvas: {notes.Count}";
        }
        private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NotesList.SelectedItem is Note selectedNote)
            {
                TitleTextBox.Text = selectedNote.Title;
                ContentTextBox.Text = selectedNote.Content;
            }
        }
        private void SaveNote_Click(object sender, RoutedEventArgs e)
        {
            if (NotesList.SelectedItem is Note selectedNote)
            {
                selectedNote.Title = TitleTextBox.Text;
                selectedNote.Content = ContentTextBox.Text;

                NotesList.Items.Refresh();
            }
        }

        private void  Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && NotesList.IsKeyboardFocusWithin && NotesList.SelectedItem is Note selectedNote)
            {
                notes.Remove(selectedNote);
                NotesList.Items.Remove(selectedNote);

                TitleTextBox.Text = "";
                ContentTextBox.Text = "";
                NotesCountText.Text = $"Notas salvas: {notes.Count}";
            }
        }
    }
}