using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using SideNotes.Models;
using SideNotes.Services;

namespace SideNotes.ViewModels
{
    public sealed class DeskViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly NotesRepository repository;
        private readonly HashSet<Note> trackedNotes = new();

        private Note? openedNote;
        private bool isExpanded;
        private bool isOnRight = true;

        public ObservableCollection<Note> Notes => repository.Notes;

        public ICollectionView DeskNotes { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Note? OpenedNote
        {
            get => openedNote;
            set => SetField(ref openedNote, value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set => SetField(ref isExpanded, value);
        }

        public bool IsOnRight
        {
            get => isOnRight;
            set => SetField(ref isOnRight, value);
        }

        public DeskViewModel(NotesRepository repository)
        {
            this.repository = repository;

            DeskNotes = new ListCollectionView(Notes)
            {
                Filter = item =>
                    item is Note note && note.IsPinnedToDesk
            };

            UpdateTrackedNotes();

            Notes.CollectionChanged += OnNotesCollectionChanged;
        }

        private void OnNotesCollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            UpdateTrackedNotes();
        }

        private void UpdateTrackedNotes()
        {
            foreach (Note note in trackedNotes.ToArray())
            {
                if (!Notes.Contains(note))
                {
                    note.PropertyChanged -= OnNotePropertyChanged;
                    trackedNotes.Remove(note);
                }
            }

            foreach (Note note in Notes)
            {
                if (trackedNotes.Add(note))
                {
                    note.PropertyChanged += OnNotePropertyChanged;
                }
            }
        }

        private void OnNotePropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(Note.IsPinnedToDesk))
            {
                DeskNotes.Refresh();
            }
        }

        private void SetField<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;

            field = value;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Notes.CollectionChanged -= OnNotesCollectionChanged;

            foreach (Note note in trackedNotes)
            {
                note.PropertyChanged -= OnNotePropertyChanged;
            }

            trackedNotes.Clear();
        }
    }
}