using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Windows.Threading;
using System.Windows.Data;
using SideNotes.Models;
using SideNotes.Services;

namespace SideNotes.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public string? LoadWarningMessage { get; }
        public ObservableCollection<Note> Notes { get; }
        public ICollectionView FilteredNotes { get; }
        public ICollectionView DeskNotes { get; }
        private Note? selectedNote;
        public event Action<string>? SaveFailed;
        private string searchText = string.Empty;
        private bool hasUnsavedChanges;
        private const int MaxDeskPinnedNotes = 5;
        private int PinnedNotesCount => Notes.Count(note => note.IsPinnedToDesk);
        public event PropertyChangedEventHandler? PropertyChanged;


        private readonly string[] noteColors =
        {
            "#B8E6D0",
            "#F7D794",
            "#D8C4F1",
            "#F3B3B3",
            "#A8D8EA"
        };
        

        public MainViewModel()
        {
            Notes = new ObservableCollection<Note>(NoteStorage.Load(out string? warningMessage));

            DeskNotes = new ListCollectionView(Notes);
            DeskNotes.Filter = item => item is Note note && note.IsPinnedToDesk;

            FilteredNotes = new ListCollectionView(Notes);
            FilteredNotes.Filter = MatchesSearch;

            LoadWarningMessage = warningMessage;
            
            autoSaveTimer.Tick += AutoSaveTimer_Tick;

            foreach (Note note in Notes)
            {
                note.PropertyChanged += OnNotePropertyChanged;
            }
            
            SelectedNote = Notes.FirstOrDefault();
        }
        
        public Note CreateNote()
        {
            SearchText = string.Empty;

            Note note = new Note
            {
                Title = $"Nova nota {Notes.Count + 1}",
                Content = "",
                Color = noteColors[Notes.Count % noteColors.Length]
            };

            note.PropertyChanged += OnNotePropertyChanged;

            Notes.Add(note);
            hasUnsavedChanges = true;
            SetSelectedNote(note, savePreviousNote: false);

            SaveNotesAndNotifyFailure();

            return note;
        }
        
        public Note? SelectedNote
        {
            get => selectedNote;
            set => SetSelectedNote(value);
        }

        private void OnNotePropertyChanged(
            object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Note.Title) ||
                e.PropertyName == nameof(Note.Content))
            {
                autoSaveTimer.Stop();
                hasUnsavedChanges = true;
                autoSaveTimer.Start();
            }
        }
        
        public void SaveNotes()
        {
            autoSaveTimer.Stop();
            NoteStorage.Save(Notes.ToList());
            hasUnsavedChanges = false;
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
                    "Mantenha o aplicativo aberto e pressione Ctrl +S para tentar salvar novamente.";

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
            SaveNotesAndNotifyFailure();
        }
        
        public void StopAutoSave()
        {
            autoSaveTimer.Stop();
        }

        public void DeleteSelectedNote()
        {
            if (SelectedNote is null) return;

            Note noteToDelete = SelectedNote;

            var visibleNotes = FilteredNotes.Cast<Note>().ToList();
            int index = visibleNotes.IndexOf(noteToDelete);

            SetSelectedNote(null, savePreviousNote: false);

            noteToDelete.PropertyChanged -= OnNotePropertyChanged;

            hasUnsavedChanges = true;
            Notes.Remove(noteToDelete);

            visibleNotes = FilteredNotes.Cast<Note>().ToList();

            if (visibleNotes.Count > 0)
            {
                int newIndex = Math.Clamp(index, 0, visibleNotes.Count - 1);
                SetSelectedNote(visibleNotes[newIndex], savePreviousNote: false);
            }

            SaveNotesAndNotifyFailure();
        }

        private void SaveNotesAndNotifyFailure()
        {
            if (!hasUnsavedChanges) return;         
            
            if (!TrySaveNotes(out string? errorMessage))
            {
                SaveFailed?.Invoke(
                    errorMessage ?? "Não foi possível salvar as notas.");
            }
        }

        public void SaveCurrentNote()
        {
            SaveNotesAndNotifyFailure();
        }

        private void SetSelectedNote(Note? value, bool savePreviousNote = true)
        {
            if (selectedNote == value) return;                     

            if (savePreviousNote && selectedNote is not null && Notes.Contains(selectedNote))
            {
                SaveNotesAndNotifyFailure();
            }

            selectedNote = value;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(SelectedNote)));
        }
        
        public string SearchText
        {
            get => searchText;
            set
            {
                if (searchText == value) return;

                searchText = value;
                FilteredNotes.Refresh();

                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(SearchText)));
            }
        }

        private bool MatchesSearch(object item)
        {
            if (item is not Note note)
            {
                return false;
            }

            string query = searchText.Trim();

            if (query.Length == 0)
            {
                return true;
            }

            return note.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                   note.Content.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        public bool TryToggleSelectedNoteDeskPin(out string? message)
        {
            message = null;

            if (SelectedNote is null)
            {
                message = "Selecione uma nota primeiro.";
                return false;
            }

            if (!SelectedNote.IsPinnedToDesk && PinnedNotesCount >= MaxDeskPinnedNotes)
            {
                message = $"Você pode fixar no máximo {MaxDeskPinnedNotes} notas no Desk.";
                return false;
            }

            SelectedNote.IsPinnedToDesk = !SelectedNote.IsPinnedToDesk;

            DeskNotes.Refresh();

            hasUnsavedChanges = true;
            SaveNotesAndNotifyFailure();

            return true;
        }
    }
}

