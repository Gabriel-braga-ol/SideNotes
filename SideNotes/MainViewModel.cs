using System.Collections.ObjectModel;

namespace SideNotes
{
    public class MainViewModel
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
    }
}

