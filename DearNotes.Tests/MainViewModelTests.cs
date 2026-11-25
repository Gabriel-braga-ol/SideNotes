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
            Assert.Equal(2, viewModel.VisibleNoteCount);

            viewModel.SearchText = "pão";

            Assert.Single(VisibleNotes(viewModel));
            Assert.Equal(1, viewModel.VisibleNoteCount);
        }

        [Fact]
        public void CreateNote_WhenFiltersAreActive_ClearsFiltersAndSelectsNewNote()
        {
            var repository = new FakeNotesRepository();
            Note pinned = CreateNote("Fixada", "pão", isPinned: true);

            repository.Notes.Add(pinned);

            using var viewModel = new MainViewModel(
                repository,
                new RecordingDialogService());

            viewModel.SearchText = "pão";
            viewModel.ShowOnlyDesk = true;

            Note? createdNote = null;
            viewModel.NoteCreated += note => createdNote = note;

            viewModel.CreateNoteCommand.Execute(null);

            Assert.NotNull(createdNote);
            Assert.False(viewModel.ShowOnlyDesk);
            Assert.Equal(string.Empty, viewModel.SearchText);
            Assert.Same(createdNote, viewModel.SelectedNote);
            Assert.Contains(createdNote, VisibleNotes(viewModel));
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
            var dialogs = new RecordingDialogService();

            Note first = CreateNote("Primeira", string.Empty);
            Note second = CreateNote("Segunda", string.Empty);

            repository.Notes.Add(first);
            repository.Notes.Add(second);

            using var viewModel = new MainViewModel(repository, dialogs);

            viewModel.SelectedNote = first;

            int savesBeforeDelete = repository.SavePendingChangesCalls;

            viewModel.DeleteSelectedNote();

            Assert.DoesNotContain(first, repository.Notes);
            Assert.Same(second, viewModel.SelectedNote);
            Assert.Single(dialogs.Confirmations);
            Assert.Equal(savesBeforeDelete + 1, repository.SavePendingChangesCalls);
        }

        [Fact]
        public void DeleteSelectedNote_WhenConfirmationIsDeclined_KeepsNote()
        {
            var repository = new FakeNotesRepository();
            Note note = CreateNote("Nota", "conteúdo");

            repository.Notes.Add(note);

            var dialogs = new RecordingDialogService
            {
                ConfirmResult = false
            };

            using var viewModel = new MainViewModel(repository, dialogs);

            int savesBeforeDelete = repository.SavePendingChangesCalls;

            viewModel.DeleteNoteCommand.Execute(null);

            Assert.Contains(note, repository.Notes);
            Assert.Single(dialogs.Confirmations);
            Assert.Equal(savesBeforeDelete, repository.SavePendingChangesCalls);
        }

        [Fact]
        public void ShowOnlyDesk_WhenEnabled_ShowsOnlyPinnedNotesAndSelectsOne()
        {
            var repository = new FakeNotesRepository();

            Note pinned = CreateNote("Fixada", string.Empty, isPinned: true);
            Note loose = CreateNote("Solta", string.Empty);

            repository.Notes.Add(pinned);
            repository.Notes.Add(loose);

            using var viewModel = new MainViewModel(
                repository,
                new RecordingDialogService());

            viewModel.SelectedNote = loose;
            viewModel.ShowOnlyDesk = true;

            Assert.Same(pinned, Assert.Single(VisibleNotes(viewModel)));
            Assert.Same(pinned, viewModel.SelectedNote);
            Assert.Equal(1, viewModel.VisibleNoteCount);

            viewModel.ShowOnlyDesk = false;

            Assert.Equal(2, VisibleNotes(viewModel).Length);
        }

        [Fact]
        public void ToggleDeskPin_WhenDeskFilterIsActive_RemovesUnpinnedNote()
        {
            var repository = new FakeNotesRepository();
            Note pinned = CreateNote("Fixada", string.Empty, isPinned: true);

            repository.Notes.Add(pinned);

            using var viewModel = new MainViewModel(
                repository,
                new RecordingDialogService());

            viewModel.SelectedNote = pinned;
            viewModel.ShowOnlyDesk = true;

            viewModel.ToggleDeskPinCommand.Execute(null);

            Assert.False(pinned.IsPinnedToDesk);
            Assert.Empty(VisibleNotes(viewModel));
            Assert.Null(viewModel.SelectedNote);
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
