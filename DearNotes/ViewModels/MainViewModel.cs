using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using DearNotes.Models;
using DearNotes.Services;

namespace DearNotes.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private const int MaxDeskPinnedNotes = 5;

        private readonly INotesRepository repository;

        private readonly IDialogService dialogService;

        private readonly RelayCommand toggleDeskPinCommand;

        private readonly RelayCommand deleteNoteCommand;

        private Note? selectedNote;
        private string searchText = string.Empty;
        private bool showOnlyDesk;

        public ObservableCollection<Note> Notes => repository.Notes;

        public string? LoadWarningMessage =>
            repository.LoadWarningMessage;

        public ICollectionView FilteredNotes { get; }

        public int VisibleNoteCount =>
            FilteredNotes.Cast<Note>().Count();

        public ICommand CreateNoteCommand { get; }

        public ICommand ToggleDeskPinCommand => toggleDeskPinCommand;

        public ICommand DeleteNoteCommand => deleteNoteCommand;

        private int PinnedNotesCount =>
            Notes.Count(note => note.IsPinnedToDesk);

        public event PropertyChangedEventHandler? PropertyChanged;

        // A View usa este evento para rolar até a nota e devolver o foco ao título.
        public event Action<Note>? NoteCreated;

        public MainViewModel(
            INotesRepository repository,
            IDialogService dialogService)
        {
            this.repository = repository;
            this.dialogService = dialogService;

            FilteredNotes = new ListCollectionView(Notes)
            {
                Filter = MatchesSearch
            };

            CreateNoteCommand = new RelayCommand(CreateNote);
            toggleDeskPinCommand = new RelayCommand(
                ToggleDeskPin,
                () => SelectedNote is not null);
            deleteNoteCommand = new RelayCommand(
                DeleteSelectedNote,
                () => SelectedNote is not null);

            repository.SaveFailed += OnSaveFailed;
            Notes.CollectionChanged += OnNotesCollectionChanged;

            SelectedNote = Notes.FirstOrDefault();
        }

        public Note? SelectedNote
        {
            get => selectedNote;
            set => SetSelectedNote(value);
        }

        public string SearchText
        {
            get => searchText;
            set
            {
                if (searchText == value) return;

                searchText = value;
                RefreshFilter();

                Notify(nameof(SearchText));
            }
        }

        public bool ShowOnlyDesk
        {
            get => showOnlyDesk;
            set
            {
                if (showOnlyDesk == value) return;

                showOnlyDesk = value;
                Notify(nameof(ShowOnlyDesk));

                RefreshFilter();
                EnsureSelectionIsVisible();
            }
        }

        public void DeleteSelectedNote()
        {
            if (SelectedNote is null) return;

            string target = string.IsNullOrWhiteSpace(SelectedNote.Title)
                ? "esta nota"
                : $"a nota \"{SelectedNote.Title}\"";

            bool confirmed = dialogService.Confirm(
                $"Excluir {target}?\n\n" +
                "Esta ação não pode ser desfeita.",
                "Excluir nota");

            if (!confirmed) return;

            Note noteToDelete = SelectedNote;

            var visibleNotes = FilteredNotes.Cast<Note>().ToList();
            int index = visibleNotes.IndexOf(noteToDelete);

            SetSelectedNote(null, savePreviousNote: false);

            repository.RemoveNote(noteToDelete);

            visibleNotes = FilteredNotes.Cast<Note>().ToList();

            if (visibleNotes.Count > 0)
            {
                int newIndex = Math.Clamp(
                    index, 0, visibleNotes.Count - 1);

                SetSelectedNote(
                    visibleNotes[newIndex],
                    savePreviousNote: false);
            }

            repository.SavePendingChanges();
        }

        public void SaveCurrentNote()
        {
            repository.SavePendingChanges();
        }

        public async Task<bool> ConfirmCloseAsync()
        {
            bool saved = await repository.FlushAsync();

            if (saved) return true;

            return dialogService.Confirm(
                "Não foi possível salvar as notas.\n\n" +
                "Deseja fechar mesmo assim? " +
                "As alterações não gravadas serão perdidas.",
                "Falha ao salvar");
        }

        public void ShowLoadWarningIfAny()
        {
            if (LoadWarningMessage is null) return;

            dialogService.ShowWarning(LoadWarningMessage, "DearNotes");
        }

        public void Dispose()
        {
            repository.SaveFailed -= OnSaveFailed;
            Notes.CollectionChanged -= OnNotesCollectionChanged;
            repository.StopAutoSave();
        }

        private void CreateNote()
        {
            // A nota nova nasce sem fixação; limpar os filtros garante que ela apareça.
            ShowOnlyDesk = false;
            SearchText = string.Empty;

            Note note = repository.CreateNote();

            SetSelectedNote(note, savePreviousNote: false);
            repository.SavePendingChanges();

            NoteCreated?.Invoke(note);
        }

        private void ToggleDeskPin()
        {
            if (SelectedNote is null) return;

            if (!SelectedNote.IsPinnedToDesk &&
                PinnedNotesCount >= MaxDeskPinnedNotes)
            {
                dialogService.ShowInformation(
                    $"Você pode fixar no máximo " +
                    $"{MaxDeskPinnedNotes} notas no Desk.",
                    "DearNotes");

                return;
            }

            SelectedNote.IsPinnedToDesk =
                !SelectedNote.IsPinnedToDesk;

            repository.SavePendingChanges();

            RefreshFilter();
            EnsureSelectionIsVisible();
        }

        private void SetSelectedNote(
            Note? value,
            bool savePreviousNote = true)
        {
            if (selectedNote == value) return;

            if (savePreviousNote &&
                selectedNote is not null &&
                Notes.Contains(selectedNote))
            {
                repository.SavePendingChanges();
            }

            selectedNote = value;

            toggleDeskPinCommand.RaiseCanExecuteChanged();
            deleteNoteCommand.RaiseCanExecuteChanged();

            Notify(nameof(SelectedNote));
        }

        private void RefreshFilter()
        {
            FilteredNotes.Refresh();
            Notify(nameof(VisibleNoteCount));
        }

        private void EnsureSelectionIsVisible()
        {
            if (SelectedNote is not null &&
                FilteredNotes.Contains(SelectedNote))
            {
                return;
            }

            SetSelectedNote(
                FilteredNotes.Cast<Note>().FirstOrDefault());
        }

        private bool MatchesSearch(object item)
        {
            if (item is not Note note) return false;

            if (showOnlyDesk && !note.IsPinnedToDesk) return false;

            string query = searchText.Trim();

            if (query.Length == 0) return true;

            return note.Title.Contains(
                       query, StringComparison.OrdinalIgnoreCase)
                   || note.Content.Contains(
                       query, StringComparison.OrdinalIgnoreCase);
        }

        private void OnNotesCollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            Notify(nameof(VisibleNoteCount));
        }

        private void OnSaveFailed(string message)
        {
            dialogService.ShowWarning(message, "Falha ao salvar");
        }

        private void Notify(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
