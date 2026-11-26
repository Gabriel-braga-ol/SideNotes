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

            using var viewModel = new DeskViewModel(
                repository,
                new RecordingDialogService());

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

            using var viewModel = new DeskViewModel(
                repository,
                new RecordingDialogService());

            viewModel.OpenNote(note);

            Assert.Null(viewModel.OpenedNote);
            Assert.False(viewModel.IsExpanded);
        }

        [Fact]
        public void NoteChanged_WhenOpenedNoteIsUnpinned_ClosesNoteAndCollapsesDesk()
        {
            var repository = new FakeNotesRepository();
            Note note = CreateNote("Fixada", isPinned: true);

            repository.Notes.Add(note);

            using var viewModel = new DeskViewModel(
                repository,
                new RecordingDialogService());

            viewModel.OpenNote(note);

            note.IsPinnedToDesk = false;

            repository.ReportNoteChanged(
                note,
                nameof(Note.IsPinnedToDesk));

            Assert.Null(viewModel.OpenedNote);
            Assert.False(viewModel.IsExpanded);
        }

        [Fact]
        public void SetExpanded_WhenDeskIsEmpty_StaysCollapsed()
        {
            var repository = new FakeNotesRepository();

            using var viewModel = new DeskViewModel(
                repository,
                new RecordingDialogService());

            viewModel.SetExpanded(true);

            Assert.False(viewModel.IsExpanded);
        }

        [Fact]
        public void CloseNote_WhenNoteIsOpen_ClearsOpenedNote()
        {
            var repository = new FakeNotesRepository();
            Note note = CreateNote("Fixada", isPinned: true);

            repository.Notes.Add(note);

            using var viewModel = new DeskViewModel(
                repository,
                new RecordingDialogService());

            viewModel.OpenNote(note);
            viewModel.CloseNote();

            Assert.Null(viewModel.OpenedNote);
        }

        [Fact]
        public void CreateNoteCommand_WhenDeskHasRoom_CreatesPinnedNoteAndOpensIt()
        {
            var repository = new FakeNotesRepository();

            using var viewModel = new DeskViewModel(
                repository,
                new RecordingDialogService());

            viewModel.CreateNoteCommand.Execute(null);

            Note note = Assert.Single(repository.Notes);

            Assert.True(note.IsPinnedToDesk);
            Assert.Same(note, viewModel.OpenedNote);
            Assert.True(viewModel.IsExpanded);
            Assert.Equal(1, repository.SavePendingChangesCalls);
        }

        [Fact]
        public void CreateNoteCommand_WhenDeskIsFull_ShowsInformationAndCreatesNothing()
        {
            var repository = new FakeNotesRepository();
            var dialogs = new RecordingDialogService();

            for (int i = 0; i < Note.MaxDeskPinnedNotes; i++)
            {
                repository.Notes.Add(CreateNote($"Fixada {i}", isPinned: true));
            }

            using var viewModel = new DeskViewModel(repository, dialogs);

            viewModel.CreateNoteCommand.Execute(null);

            Assert.Single(dialogs.Information);
            Assert.Equal(Note.MaxDeskPinnedNotes, repository.Notes.Count);
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
