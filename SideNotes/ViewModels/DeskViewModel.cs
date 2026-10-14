using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using SideNotes.Models;
using SideNotes.Services;

namespace SideNotes.ViewModels
{
    public sealed class DeskViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly NotesRepository repository;

        private Note? openedNote;
        private bool isExpanded;
        private bool isClosing;
        private bool isOnRight = true;

        public ObservableCollection<Note> Notes => repository.Notes;

        public ICollectionView DeskNotes { get; }

        // O estado só pode mudar pelos métodos abaixo.
        public Note? OpenedNote => openedNote;
        public bool IsExpanded => isExpanded;
        public bool IsClosing => isClosing;
        public bool IsOnRight => isOnRight;

        public event PropertyChangedEventHandler? PropertyChanged;

        public DeskViewModel(NotesRepository repository)
        {
            this.repository = repository;

            DeskNotes = new ListCollectionView(Notes)
            {
                Filter = item =>
                    item is Note note && note.IsPinnedToDesk
            };

            repository.NoteChanged += OnNoteChanged;
            Notes.CollectionChanged += OnNotesCollectionChanged;
        }

        public void OpenNote(Note note)
        {
            if (!Notes.Contains(note) || !note.IsPinnedToDesk) return;

            UpdateState(note, expanded: true, closing: false);
        }

        public void SetExpanded(bool expanded)
        {
            // O leque continua expandido ao abrir a nota
            if (openedNote is not null) return;

            UpdateState(
                null,
                expanded && !DeskNotes.IsEmpty,
                closing: false);
        }

        public void CloseNote(bool immediate = false)
        {
            if (openedNote is null || immediate)
            {
                UpdateState(null, expanded: false, closing: false);
                return;
            }

            UpdateState(openedNote, expanded: true, closing: true);
        }

        public void CompleteCloseNote()
        {
            if (!isClosing) return;

            UpdateState(null, expanded: false, closing: false);
        }

        public void SetSide(bool onRight)
        {
            if (isOnRight == onRight) return;

            isOnRight = onRight;
            Notify(nameof(IsOnRight));
        }

        private void UpdateState(
            Note? note,
            bool expanded,
            bool closing)
        {
            bool noteChanged = !ReferenceEquals(openedNote, note);
            bool expandedChanged = isExpanded != expanded;
            bool closingChanged = isClosing != closing;

            // Atualiza tudo antes de notificar a interface.
            openedNote = note;
            isExpanded = expanded;
            isClosing = closing;

            if (noteChanged) Notify(nameof(OpenedNote));

            if (closingChanged) Notify(nameof(IsClosing));

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
            if (openedNote is not null &&
                (!Notes.Contains(openedNote) ||
                 !openedNote.IsPinnedToDesk))
            {
                CloseNote();
            }
            else if (DeskNotes.IsEmpty)
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