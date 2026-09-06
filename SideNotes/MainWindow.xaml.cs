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
            Note note = new Note
            {
                Title = TitleTextBox.Text,
                Content = ContentTextBox.Text,
                Color = noteColors[notes.Count % noteColors.Length]
            };

            notes.Add(note);
            
            NoteStorage.Save(notes);

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
                NoteStorage.Save(notes);

                NotesList.Items.Refresh();
            }
        }

        private void  Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && NotesList.IsKeyboardFocusWithin && NotesList.SelectedItem is Note selectedNote)
            {
                notes.Remove(selectedNote);
                NotesList.Items.Remove(selectedNote);
                NoteStorage.Save(notes);

                TitleTextBox.Text = "";
                ContentTextBox.Text = "";
                NotesCountText.Text = $"Notas salvas: {notes.Count}";
            }
        }
    }
}