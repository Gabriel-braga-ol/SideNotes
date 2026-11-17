using System;

namespace DearNotes.Models
{
    public sealed class NoteDocument
    {
        public Guid Id { get; set; }

        public string? Title { get; set; }

        public string? Content { get; set; }

        public string? Color { get; set; }

        public bool IsPinnedToDesk { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
