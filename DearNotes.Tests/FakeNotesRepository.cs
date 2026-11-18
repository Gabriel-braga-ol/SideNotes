using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DearNotes.Models;
using DearNotes.Services;

namespace DearNotes.Tests
{
    public sealed class FakeNotesRepository : INotesRepository
    {
        private int nextNoteNumber = 1;

        public ObservableCollection<Note> Notes { get; } = new();

        public string? LoadWarningMessage { get; init; }

        public bool FlushResult { get; set; } = true;

        public int SavePendingChangesCalls { get; private set; }

        public int FlushCalls { get; private set; }

        public int StopAutoSaveCalls { get; private set; }

        public event Action<string>? SaveFailed;

        public event Action<Note, string?>? NoteChanged;

        public Note CreateNote()
        {
            var note = new Note
            {
                Title = $"Nota {nextNoteNumber++}",
                Content = string.Empty
            };

            Notes.Add(note);

            return note;
        }

        public bool RemoveNote(Note note)
        {
            return Notes.Remove(note);
        }

        public void SavePendingChanges()
        {
            SavePendingChangesCalls++;
        }

        public Task<bool> FlushAsync()
        {
            FlushCalls++;

            return Task.FromResult(FlushResult);
        }

        public void StopAutoSave()
        {
            StopAutoSaveCalls++;
        }

        public void ReportSaveFailure(string message)
        {
            SaveFailed?.Invoke(message);
        }

        public void ReportNoteChanged(
            Note note,
            string? propertyName)
        {
            NoteChanged?.Invoke(note, propertyName);
        }
    }
}
