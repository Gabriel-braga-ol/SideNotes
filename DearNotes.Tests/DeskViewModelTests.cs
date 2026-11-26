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

            using var viewModel = CreateViewModel(repository);

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

            using var viewModel = CreateViewModel(repository);

            viewModel.OpenNote(note);

            Assert.Null(viewModel.OpenedNote);
        }

        [Fact]
        public void NoteChanged_WhenOpenedNoteIsUnpinned_ClosesNoteAndCollapsesDesk()
        {
            var repository = new FakeNotesRepository();
            Note note = CreateNote("Fixada", isPinned: true);

            repository.Notes.Add(note);

            using var viewModel = CreateViewModel(repository);

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

            using var viewModel = CreateViewModel(repository);

            viewModel.SetExpanded(true);

            Assert.False(viewModel.IsExpanded);
        }

        [Fact]
        public void CloseNote_WhenNoteIsOpen_ClearsOpenedNote()
        {
            var repository = new FakeNotesRepository();
            Note note = CreateNote("Fixada", isPinned: true);

            repository.Notes.Add(note);

            using var viewModel = CreateViewModel(repository);

            viewModel.OpenNote(note);
            viewModel.CloseNote();

            Assert.Null(viewModel.OpenedNote);
        }

        [Fact]
        public void CreateNote_WhenBelowLimit_AddsPinnedNoteAndOpensIt()
        {
            var repository = new FakeNotesRepository();
            var dialogs = new RecordingDialogService();

            using var viewModel = CreateViewModel(repository, dialogs);

            viewModel.CreateNoteCommand.Execute(null);

            Note note = Assert.Single(repository.Notes);

            Assert.True(note.IsPinnedToDesk);
            Assert.Same(note, viewModel.OpenedNote);
            Assert.True(viewModel.IsExpanded);
            Assert.Empty(dialogs.Information);
            Assert.Equal(1, repository.SavePendingChangesCalls);
        }

        [Fact]
        public void CreateNote_WhenLimitIsReached_ShowsWarningAndDoesNotAddNote()
        {
            var repository = new FakeNotesRepository();
            var dialogs = new RecordingDialogService();

            for (int i = 0; i < Note.MaxDeskPinnedNotes; i++)
            {
                repository.Notes.Add(
                    CreateNote($"Fixada {i + 1}", isPinned: true));
            }

            using var viewModel = CreateViewModel(repository, dialogs);

            viewModel.CreateNoteCommand.Execute(null);

            Assert.Equal(
                Note.MaxDeskPinnedNotes,
                repository.Notes.Count);

            Assert.Single(dialogs.Information);
            Assert.Null(viewModel.OpenedNote);
            Assert.Equal(0, repository.SavePendingChangesCalls);
        }

        private static DeskViewModel CreateViewModel(
            FakeNotesRepository repository,
            RecordingDialogService? dialogs = null)
        {
            return new DeskViewModel(
                repository,
                dialogs ?? new RecordingDialogService());
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
