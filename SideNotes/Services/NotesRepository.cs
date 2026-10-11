using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using SideNotes.Models;

namespace SideNotes.Services
{
    // responsável pela coleção compartilhada,
    // pelo autosave e pelas solicitações de salvamento ao NoteStorage
    // acompanha alterações na coleção e nos objetos Note
    public sealed class NotesRepository : IDisposable
    {
        public ObservableCollection<Note> Notes { get; }

        public string? LoadWarningMessage { get; }

        public event Action<string>? SaveFailed;

        private bool hasUnsavedChanges;

        private readonly HashSet<Note> trackedNotes = new();

        private readonly DispatcherTimer autoSaveTimer = new()
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        private readonly string[] noteColors =
        {
            "#B8E6D0",
            "#F7D794",
            "#D8C4F1",
            "#F3B3B3",
            "#A8D8EA"
        };

        public NotesRepository()
        {
            Notes = new ObservableCollection<Note>(
                NoteStorage.Load(out string? warningMessage));

            LoadWarningMessage = warningMessage;

            UpdateTrackedNotes();

            Notes.CollectionChanged += OnCollectionChanged;
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
        }

        public Note CreateNote()
        {
            var note = new Note
            {
                Title = $"Nova nota {Notes.Count + 1}",
                Content = string.Empty,
                Color = noteColors[Notes.Count % noteColors.Length]
            };

            Notes.Add(note);

            return note;
        }

        public bool RemoveNote(Note note)
        {
            return Notes.Remove(note);
        }

        private void OnCollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            UpdateTrackedNotes();
            ScheduleAutoSave();
        }

        private void UpdateTrackedNotes()
        {
            // acompanhar notas removidas.
            foreach (Note note in trackedNotes.ToArray())
            {
                if (!Notes.Contains(note))
                {
                    note.PropertyChanged -= OnNotePropertyChanged;
                    trackedNotes.Remove(note);
                }
            }

            // acompanha cada nota uma única vez.
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
            ScheduleAutoSave();
        }

        private void ScheduleAutoSave()
        {
            hasUnsavedChanges = true;

            autoSaveTimer.Stop();
            autoSaveTimer.Start();
        }

        private void AutoSaveTimer_Tick(
            object? sender,
            EventArgs e)
        {
            autoSaveTimer.Stop();
            SavePendingChanges();
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
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                errorMessage =
                    "Não foi possível salvar as notas no arquivo.\n\n" +
                    "Suas alterações continuam na memória. " +
                    "Mantenha o aplicativo aberto e pressione Ctrl + S " +
                    "para tentar salvar novamente.";

                return false;
            }
        }

        public void SavePendingChanges()
        {
            if (!hasUnsavedChanges) return;

            if (!TrySaveNotes(out string? errorMessage))
            {
                SaveFailed?.Invoke(
                    errorMessage ?? "Não foi possível salvar as notas.");
            }
        }

        public void StopAutoSave()
        {
            autoSaveTimer.Stop();
        }

        public void Dispose()
        {
            autoSaveTimer.Stop();
            autoSaveTimer.Tick -= AutoSaveTimer_Tick;

            Notes.CollectionChanged -= OnCollectionChanged;

            foreach (Note note in trackedNotes)
            {
                note.PropertyChanged -= OnNotePropertyChanged;
            }

            trackedNotes.Clear();
        }
    }
}
