using DearNotes.Models;
using DearNotes.ViewModels;

namespace DearNotes.Tests
{
    public sealed class DeskViewModelTests
    {
        [Fact]
        public void OpenNote_WhenNoteIsPinned_ExpandsAndOpensNote()
        {
            var repository = new FakeNotesRepository();
            Note note = CreateNote("Fixada", isPinned: true);

            repository.Notes.Add(note);

            using var viewModel = new DeskViewModel(repository);

            viewModel.OpenNote(note);

            Assert.Same(note, viewModel.OpenedNote);
            Assert.True(viewModel.IsExpanded);
        }

        [Fact]
        public void OpenNote_WhenNoteIsNotPinned_IgnoresNote()
        {
            var repository = new FakeNotesRepository();
            Note note = CreateNote("Solta", isPinned: false);

            repository.Notes.Add(note);

            using var viewModel = new DeskViewModel(repository);

            viewModel.OpenNote(note);

            Assert.Null(viewModel.OpenedNote);
            Assert.False(viewModel.IsExpanded);
        }

        [Fact]
        public void SetExpanded_WhenDeskIsEmpty_StaysCollapsed()
        {
            var repository = new FakeNotesRepository();

            using var viewModel = new DeskViewModel(repository);

            viewModel.SetExpanded(true);

            Assert.False(viewModel.IsExpanded);
        }

        [Fact]
        public void CloseNote_WhenNoteIsOpen_ClearsOpenedNote()
        {
            var repository = new FakeNotesRepository();
            Note note = CreateNote("Fixada", isPinned: true);

            repository.Notes.Add(note);

            using var viewModel = new DeskViewModel(repository);

            viewModel.OpenNote(note);
            viewModel.CloseNote();

            Assert.Null(viewModel.OpenedNote);
        }

        private static Note CreateNote(string title, bool isPinned)
        {
            return new Note
            {
                Title = title,
                Content = string.Empty,
                IsPinnedToDesk = isPinned
            };
        }
    }
}
