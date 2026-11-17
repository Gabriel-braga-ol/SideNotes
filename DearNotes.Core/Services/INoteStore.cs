using System.Collections.Generic;
using DearNotes.Models;

namespace DearNotes.Services
{
    public interface INoteStore
    {
        IReadOnlyList<Note> Load(out string? warningMessage);

        void Save(IReadOnlyList<Note> notes);
    }
}
