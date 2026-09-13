using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Windows.Threading;

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
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
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
                
                autoSaveTimer.Stop();

                if (selectedNote is not null && Notes.Contains(selectedNote))
                {
                    if (!TrySaveNotes(out string? errorMessage))
                    {
                        SaveFailed?.Invoke(
                            errorMessage ?? "Não foi possível salvar as notas.");
                    }
                }

                if (selectedNote is not null)
                {
                    selectedNote.PropertyChanged -= OnSelectedNotePropertyChanged;
                }

                selectedNote = value;

                if (selectedNote is not null)
                {
                    selectedNote.PropertyChanged += OnSelectedNotePropertyChanged;
                }

                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(SelectedNote)));
            }
        }

        private void OnSelectedNotePropertyChanged(
            object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Note.Title) ||
                e.PropertyName == nameof(Note.Content))
            {
                autoSaveTimer.Stop();
                autoSaveTimer.Start();
            }
        }
        
        public void SaveNotes()
        {
            autoSaveTimer.Stop();
            NoteStorage.Save(Notes.ToList());
        }

        public bool TrySaveNotes(out string? errorMessage)
        {
            try
            {
                SaveNotes();
                errorMessage = null;
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                errorMessage =
                    "Não foi possível salvar as notas no arquivo.\n\n" +
                    "Suas alterações continuam na memória. " +
                    "Mantenha o aplicativo aberto e tente salvar novamente.";

                return false;
            }
        }

        private readonly DispatcherTimer autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        public event Action<string>? SaveFailed;

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            autoSaveTimer.Stop();

            if (SelectedNote is null)
            {
                return;
            }

            if (!TrySaveNotes(out string? errorMessage))
            {
                SaveFailed?.Invoke(errorMessage ?? "Não foi possível salvar as notas.");
            }
        }
        
        public void StopAutoSave()
        {
            autoSaveTimer.Stop();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

