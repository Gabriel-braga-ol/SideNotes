using DearNotes.Models;

namespace DearNotes.Tests
{
    public sealed class NoteTests
    {
        [Fact]
        public void CreateSnapshot_CopiesValuesAndMetadata()
        {
            var note = new Note
            {
                Title = "Compras",
                Content = "Leite",
                Color = "#112233",
                IsPinnedToDesk = true
            };

            Note snapshot = note.CreateSnapshot();

            Assert.NotSame(note, snapshot);
            Assert.Equal(note.Id, snapshot.Id);
            Assert.Equal(note.CreatedAt, snapshot.CreatedAt);
            Assert.Equal(note.UpdatedAt, snapshot.UpdatedAt);
            Assert.Equal(note.Title, snapshot.Title);
            Assert.Equal(note.Content, snapshot.Content);
            Assert.Equal(note.Color, snapshot.Color);
            Assert.Equal(note.IsPinnedToDesk, snapshot.IsPinnedToDesk);
        }

        [Fact]
        public void Title_WhenChanged_NotifiesUpdatedAt()
        {
            var note = new Note();

            var changedProperties = new List<string?>();

            note.PropertyChanged += (_, e) =>
                changedProperties.Add(e.PropertyName);

            note.Title = "Nova";

            Assert.Contains(nameof(Note.UpdatedAt), changedProperties);
        }

        [Fact]
        public void CreateSnapshot_IsIndependentFromOriginal()
        {
            var note = new Note
            {
                Title = "Nota",
                Content = "Texto"
            };

            Note snapshot = note.CreateSnapshot();

            snapshot.Title = "Outra";
            snapshot.Content = "Outro texto";
            snapshot.IsPinnedToDesk = true;

            Assert.Equal("Nota", note.Title);
            Assert.Equal("Texto", note.Content);
            Assert.False(note.IsPinnedToDesk);
        }
    }
}
