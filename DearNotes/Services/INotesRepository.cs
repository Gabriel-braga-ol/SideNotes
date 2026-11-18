using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DearNotes.Models;

namespace DearNotes.Services
{
    /// <summary>
    /// Coleção compartilhada de notas e ponto único de autosave usado pelos
    /// ViewModels, sem expor a infraestrutura de gravação.
    /// </summary>
    public interface INotesRepository
    {
        ObservableCollection<Note> Notes { get; }

        string? LoadWarningMessage { get; }

        event Action<string>? SaveFailed;

        event Action<Note, string?>? NoteChanged;

        Note CreateNote();

        bool RemoveNote(Note note);

        void SavePendingChanges();

        Task<bool> FlushAsync();

        void StopAutoSave();
    }
}
