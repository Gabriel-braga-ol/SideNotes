using System.Linq;
using System.Threading.Tasks;
using DearNotes.Models;
using DearNotes.ViewModels;

namespace DearNotes.Tests
{
    public sealed class MainViewModelTests
    {
        [Fact]
        public void SearchText_WhenMatchesTitleOrContent_FiltersVisibleNotes()
        {
            var repository = new FakeNotesRepository();

            repository.Notes.Add(CreateNote("Lista de compras", "comprar leite e ovos"));
            repository.Notes.Add(CreateNote("Ideias", "COMPRAR pão"));

            using var viewModel = new MainViewModel(
                repository,
                new RecordingDialogService());

            viewModel.SearchText = "comprar";

            Assert.Equal(2, VisibleNotes(viewModel).Length);

            viewModel.SearchText = "pão";

            Assert.Single(VisibleNotes(viewModel));
        }

        [Fact]
        public void ToggleDeskPin_WhenLimitReached_ShowsInformationAndKeepsNoteUnpinned()
        {
            var repository = new FakeNotesRepository();
            var dialogs = new RecordingDialogService();

            for (int index = 0; index < 5; index++)
            {
                repository.Notes.Add(
                    CreateNote($"Fixada {index}", string.Empty, isPinned: true));
            }

            Note candidate = CreateNote("Candidata", string.Empty);
            repository.Notes.Add(candidate);

            using var viewModel = new MainViewModel(repository, dialogs);
            viewModel.SelectedNote = candidate;

            int savesBeforeToggle = repository.SavePendingChangesCalls;

            viewModel.ToggleDeskPinCommand.Execute(null);

            Assert.False(candidate.IsPinnedToDesk);
            Assert.Single(dialogs.Information);
            Assert.Contains("5", dialogs.Information[0].Message);
            Assert.Equal(savesBeforeToggle, repository.SavePendingChangesCalls);
        }

        [Fact]
        public void ToggleDeskPin_WhenBelowLimit_PinsNoteAndSaves()
        {
            var repository = new FakeNotesRepository();
            Note note = CreateNote("Nota", string.Empty);

            repository.Notes.Add(note);

            using var viewModel = new MainViewModel(
                repository,
                new RecordingDialogService());

            viewModel.SelectedNote = note;

            int savesBeforeToggle = repository.SavePendingChangesCalls;

            viewModel.ToggleDeskPinCommand.Execute(null);

            Assert.True(note.IsPinnedToDesk);
            Assert.Equal(savesBeforeToggle + 1, repository.SavePendingChangesCalls);
        }

        [Fact]
        public void DeleteSelectedNote_RemovesNoteAndSelectsNextVisibleNote()
        {
            var repository = new FakeNotesRepository();

            Note first = CreateNote("Primeira", string.Empty);
            Note second = CreateNote("Segunda", string.Empty);

            repository.Notes.Add(first);
            repository.Notes.Add(second);

            using var viewModel = new MainViewModel(
                repository,
                new RecordingDialogService());

            viewModel.SelectedNote = first;

            int savesBeforeDelete = repository.SavePendingChangesCalls;

            viewModel.DeleteSelectedNote();

            Assert.DoesNotContain(first, repository.Notes);
            Assert.Same(second, viewModel.SelectedNote);
            Assert.Equal(savesBeforeDelete + 1, repository.SavePendingChangesCalls);
        }

        [Fact]
        public async Task ConfirmCloseAsync_WhenFlushFails_AsksConfirmation()
        {
            var repository = new FakeNotesRepository
            {
                FlushResult = false
            };

            var dialogs = new RecordingDialogService
            {
                ConfirmResult = false
            };

            using var viewModel = new MainViewModel(repository, dialogs);

            bool canClose = await viewModel.ConfirmCloseAsync();

            Assert.False(canClose);
            Assert.Single(dialogs.Confirmations);
        }

        [Fact]
        public async Task ConfirmCloseAsync_WhenFlushSucceeds_ClosesWithoutAsking()
        {
            var repository = new FakeNotesRepository();
            var dialogs = new RecordingDialogService();

            using var viewModel = new MainViewModel(repository, dialogs);

            bool canClose = await viewModel.ConfirmCloseAsync();

            Assert.True(canClose);
            Assert.Empty(dialogs.Confirmations);
        }

        [Fact]
        public void ShowLoadWarningIfAny_WhenWarningExists_ShowsWarning()
        {
            var repository = new FakeNotesRepository
            {
                LoadWarningMessage = "arquivo inválido"
            };

            var dialogs = new RecordingDialogService();

            using var viewModel = new MainViewModel(repository, dialogs);

            viewModel.ShowLoadWarningIfAny();

            Assert.Single(dialogs.Warnings);
            Assert.Equal("arquivo inválido", dialogs.Warnings[0].Message);
        }

        private static Note[] VisibleNotes(MainViewModel viewModel)
        {
            return viewModel.FilteredNotes
                .Cast<Note>()
                .ToArray();
        }

        private static Note CreateNote(
            string title,
            string content,
            bool isPinned = false)
        {
            return new Note
            {
                Title = title,
                Content = content,
                IsPinnedToDesk = isPinned
            };
        }
    }
}
