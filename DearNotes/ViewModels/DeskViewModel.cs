using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using DearNotes.Models;
using DearNotes.Services;

namespace DearNotes.ViewModels
{
    public sealed class DeskViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly INotesRepository repository;

        private Note? openedNote;
        private bool isExpanded;
        private bool isOnRight = true;

        public ObservableCollection<Note> Notes => repository.Notes;

        public ICollectionView DeskNotes { get; }

        public Note? OpenedNote => openedNote;
        public bool IsExpanded => isExpanded;
        public bool IsOnRight => isOnRight;

        public ICommand CloseOpenedNoteCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public DeskViewModel(INotesRepository repository)
        {
            this.repository = repository;

            DeskNotes = new ListCollectionView(Notes)
            {
                Filter = item => item is Note note && note.IsPinnedToDesk
            };

            CloseOpenedNoteCommand = new RelayCommand(CloseNote);

            repository.NoteChanged += OnNoteChanged;
            Notes.CollectionChanged += OnNotesCollectionChanged;
        }

        public void OpenNote(Note note)
        {
            if (!Notes.Contains(note) || !note.IsPinnedToDesk) return;

            UpdateState(note, expanded: true);
        }

        public void SetExpanded(bool expanded)
        {
            UpdateState(openedNote, expanded && !DeskNotes.IsEmpty);
        }

        public void CloseNote()
        {
            UpdateState(null, isExpanded);
        }

        public void SetSide(bool onRight)
        {
            if (isOnRight == onRight) return;

            isOnRight = onRight;
            Notify(nameof(IsOnRight));
        }

        private void UpdateState(Note? note, bool expanded)
        {
            bool noteChanged = !ReferenceEquals(openedNote, note);
            bool expandedChanged = isExpanded != expanded;

            // Atualiza os dois estados de notificar a interface.
            openedNote = note;
            isExpanded = expanded;

            if (noteChanged) Notify(nameof(OpenedNote));

            if (expandedChanged) Notify(nameof(IsExpanded));
        }

        private void OnNoteChanged(
            Note note,
            string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) ||
                propertyName == nameof(Note.IsPinnedToDesk))
            {
                DeskNotes.Refresh();
                ValidateState();
            }
        }

        private void OnNotesCollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            ValidateState();
        }

        private void ValidateState()
        {
            bool openedNoteIsGone =
                openedNote is not null &&
                (!Notes.Contains(openedNote) ||
                 !openedNote.IsPinnedToDesk);

            if (openedNoteIsGone)
            {
                CloseNote();
            }

            if (DeskNotes.IsEmpty)
            {
                SetExpanded(false);
            }
        }

        private void Notify(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            repository.NoteChanged -= OnNoteChanged;
            Notes.CollectionChanged -= OnNotesCollectionChanged;
        }
    }
}
