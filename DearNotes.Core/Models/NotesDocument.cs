using System.Collections.Generic;

namespace DearNotes.Models
{
    public sealed class NotesDocument
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        public List<NoteDocument> Notes { get; set; } = new();
    }
}
