using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SideNotes
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly string[] noteColors =
        {
            "#B8E6D0",
            "#F7D794",
            "#D8C4F1",
            "#F3B3B3",
            "#A8D8EA"
        };
        public ObservableCollection<Note> Notes { get; }

        public MainViewModel()
        {
            Notes = new ObservableCollection<Note>(NoteStorage.Load());
        }
        
        public Note CreateNote()
        {
            Note note = new Note
            {
                Title = $"Nova nota {Notes.Count + 1}",
                Content = "",
                Color = noteColors[Notes.Count % noteColors.Length]
            };

            Notes.Add(note);

            return note;
        }
        
        private Note? selectedNote;

        public Note? SelectedNote
        {
            get => selectedNote;
            set
            {
                if (selectedNote == value)
                {
                    return;
                }

                selectedNote = value;

                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(SelectedNote)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

