using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using SideNotes.Models;
using SideNotes.Services;

namespace SideNotes.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly NotesRepository repository;

        private Note? selectedNote;
        private string searchText = string.Empty;

        private const int MaxDeskPinnedNotes = 5;

        public ObservableCollection<Note> Notes => repository.Notes;

        public string? LoadWarningMessage =>
            repository.LoadWarningMessage;

        public ICollectionView FilteredNotes { get; }

        public ICollectionView DeskNotes { get; }

        private int PinnedNotesCount =>
            Notes.Count(note => note.IsPinnedToDesk);

        public event PropertyChangedEventHandler? PropertyChanged;

        public event Action<string>? SaveFailed
        {
            add => repository.SaveFailed += value;
            remove => repository.SaveFailed -= value;
        }

        public MainViewModel(NotesRepository repository)
        {
            this.repository = repository;

            DeskNotes = new ListCollectionView(Notes)
            {
                Filter = item =>
                    item is Note note && note.IsPinnedToDesk
            };

            FilteredNotes = new ListCollectionView(Notes)
            {
                Filter = MatchesSearch
            };

            SelectedNote = Notes.FirstOrDefault();
        }

        public Note? SelectedNote
        {
            get => selectedNote;
            set => SetSelectedNote(value);
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

        public Note CreateNote()
        {
            SearchText = string.Empty;

            Note note = repository.CreateNote();

            SetSelectedNote(note, savePreviousNote: false);
            repository.SavePendingChanges();

            return note;
        }

        public void DeleteSelectedNote()
        {
            if (SelectedNote is null) return;

            Note noteToDelete = SelectedNote;

            var visibleNotes = FilteredNotes.Cast<Note>().ToList();
            int index = visibleNotes.IndexOf(noteToDelete);

            SetSelectedNote(null, savePreviousNote: false);

            repository.RemoveNote(noteToDelete);

            visibleNotes = FilteredNotes.Cast<Note>().ToList();

            if (visibleNotes.Count > 0)
            {
                int newIndex = Math.Clamp(
                    index, 0, visibleNotes.Count - 1);

                SetSelectedNote(
                    visibleNotes[newIndex],
                    savePreviousNote: false);
            }

            repository.SavePendingChanges();
        }

        private void SetSelectedNote(
            Note? value,
            bool savePreviousNote = true)
        {
            if (selectedNote == value) return;

            if (savePreviousNote &&
                selectedNote is not null &&
                Notes.Contains(selectedNote))
            {
                repository.SavePendingChanges();
            }

            selectedNote = value;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(SelectedNote)));
        }

        private bool MatchesSearch(object item)
        {
            if (item is not Note note) return false;

            string query = searchText.Trim();

            if (query.Length == 0) return true;

            return note.Title.Contains(
                       query, StringComparison.OrdinalIgnoreCase)
                   || note.Content.Contains(
                       query, StringComparison.OrdinalIgnoreCase);
        }

        public bool TryToggleSelectedNoteDeskPin(
            out string? message)
        {
            message = null;

            if (SelectedNote is null)
            {
                message = "Selecione uma nota primeiro.";
                return false;
            }

            if (!SelectedNote.IsPinnedToDesk &&
                PinnedNotesCount >= MaxDeskPinnedNotes)
            {
                message =
                    $"Você pode fixar no máximo {MaxDeskPinnedNotes} notas no Desk.";

                return false;
            }

            SelectedNote.IsPinnedToDesk =
                !SelectedNote.IsPinnedToDesk;

            DeskNotes.Refresh();
            repository.SavePendingChanges();

            return true;
        }

        public void SaveNotes()
        {
            repository.SaveNotes();
        }

        public bool TrySaveNotes(out string? errorMessage)
        {
            return repository.TrySaveNotes(out errorMessage);
        }

        public void SaveCurrentNote()
        {
            repository.SavePendingChanges();
        }

        public void StopAutoSave()
        {
            repository.StopAutoSave();
        }
    }
}