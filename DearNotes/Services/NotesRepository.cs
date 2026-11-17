using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using DearNotes.Models;

namespace DearNotes.Services
{
    // Responsável pela coleção compartilhada, pelo atraso do autosave,
    // pelas novas tentativas e pelo envio de snapshots ao NotesAutoSaver.
    public sealed class NotesRepository : IDisposable
    {
        private const string SaveFailureMessage =
            "Não foi possível salvar as notas no arquivo.\n\n" +
            "Suas alterações continuam na memória. " +
            "O aplicativo tentará salvar novamente em instantes; " +
            "pressione Ctrl + S para tentar agora.";

        private static readonly TimeSpan AutoSaveDelay =
            TimeSpan.FromSeconds(1);

        private static readonly TimeSpan MaxRetryDelay =
            TimeSpan.FromSeconds(30);

        private static readonly string[] NoteColors =
        {
            "#B8E6D0",
            "#F7D794",
            "#D8C4F1",
            "#F3B3B3",
            "#A8D8EA"
        };

        private readonly NotesAutoSaver autoSaver;

        private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        private readonly HashSet<Note> trackedNotes = new();

        private readonly DispatcherTimer autoSaveTimer = new()
        {
            Interval = AutoSaveDelay
        };

        private bool hasUnsavedChanges;

        private bool failureReported;

        private bool isFlushing;

        private int consecutiveFailures;

        private TimeSpan retryDelay = AutoSaveDelay;

        public ObservableCollection<Note> Notes { get; }

        public string? LoadWarningMessage { get; }

        public event Action<string>? SaveFailed;

        public event Action<Note, string?>? NoteChanged;

        public NotesRepository(INoteStore noteStore)
        {
            autoSaver = new NotesAutoSaver(noteStore);
            autoSaver.SaveSucceeded += AutoSaver_SaveSucceeded;
            autoSaver.SaveFailed += AutoSaver_SaveFailed;

            Notes = new ObservableCollection<Note>(
                noteStore.Load(out string? warningMessage));

            LoadWarningMessage = warningMessage;

            UpdateTrackedNotes();

            Notes.CollectionChanged += OnCollectionChanged;
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
        }

        public Note CreateNote()
        {
            var note = new Note
            {
                Title = $"Nova nota {Notes.Count + 1}",
                Content = string.Empty,
                Color = NoteColors[Notes.Count % NoteColors.Length]
            };

            Notes.Add(note);

            return note;
        }

        public bool RemoveNote(Note note)
        {
            return Notes.Remove(note);
        }

        private void OnCollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            UpdateTrackedNotes();
            ScheduleAutoSave();
        }

        private void UpdateTrackedNotes()
        {
            foreach (Note note in trackedNotes.ToArray())
            {
                if (Notes.Contains(note)) continue;

                note.PropertyChanged -= OnNotePropertyChanged;
                trackedNotes.Remove(note);
            }

            foreach (Note note in Notes)
            {
                if (trackedNotes.Add(note))
                {
                    note.PropertyChanged += OnNotePropertyChanged;
                }
            }
        }

        private void OnNotePropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (sender is not Note note) return;

            ScheduleAutoSave();

            NoteChanged?.Invoke(note, e.PropertyName);
        }

        private void ScheduleAutoSave()
        {
            hasUnsavedChanges = true;

            autoSaveTimer.Stop();
            autoSaveTimer.Interval = retryDelay;
            autoSaveTimer.Start();
        }

        private void AutoSaveTimer_Tick(
            object? sender,
            EventArgs e)
        {
            autoSaveTimer.Stop();
            QueuePendingChanges();
        }

        public void SavePendingChanges()
        {
            autoSaveTimer.Stop();
            QueuePendingChanges();
        }

        private void QueuePendingChanges()
        {
            if (!hasUnsavedChanges) return;

            hasUnsavedChanges = false;

            autoSaver.Enqueue(CreateSnapshot());
        }

        public bool TrySaveNotes(out string? errorMessage)
        {
            autoSaveTimer.Stop();

            try
            {
                autoSaver.SaveNow(CreateSnapshot());

                hasUnsavedChanges = false;
                ResetRetryState();

                errorMessage = null;
                return true;
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                Debug.WriteLine(
                    $"Falha ao salvar as notas: {ex.Message}");

                errorMessage = SaveFailureMessage;
                return false;
            }
        }

        // Aguarda a fila e grava edições que chegaram durante a espera.
        public async Task<bool> FlushAsync()
        {
            isFlushing = true;

            try
            {
                while (true)
                {
                    autoSaveTimer.Stop();
                    hasUnsavedChanges = false;

                    bool saved = await autoSaver.FlushAsync(
                        CreateSnapshot());

                    if (!saved)
                    {
                        // A janela pode continuar aberta se o usuário recusar o fechamento.
                        hasUnsavedChanges = true;
                        ScheduleRetry();

                        return false;
                    }

                    if (!hasUnsavedChanges)
                    {
                        ResetRetryState();
                        return true;
                    }
                }
            }
            finally
            {
                isFlushing = false;
            }
        }

        private List<Note> CreateSnapshot()
        {
            var snapshot = new List<Note>(Notes.Count);

            foreach (Note note in Notes)
            {
                snapshot.Add(note.CreateSnapshot());
            }

            return snapshot;
        }

        private void AutoSaver_SaveSucceeded()
        {
            PostToUi(HandleSaveSucceeded);
        }

        private void HandleSaveSucceeded()
        {
            if (isFlushing) return;

            ResetRetryState();

            if (!hasUnsavedChanges) return;

            autoSaveTimer.Stop();
            autoSaveTimer.Interval = AutoSaveDelay;
            autoSaveTimer.Start();
        }

        private void AutoSaver_SaveFailed(Exception exception)
        {
            PostToUi(() => HandleSaveFailed(exception));
        }

        private void HandleSaveFailed(Exception exception)
        {
            if (isFlushing) return;

            Debug.WriteLine(
                $"Falha no autosave: {exception.Message}");

            hasUnsavedChanges = true;
            ScheduleRetry();

            if (failureReported) return;

            failureReported = true;
            SaveFailed?.Invoke(SaveFailureMessage);
        }

        private void PostToUi(Action action)
        {
            if (dispatcher.HasShutdownStarted ||
                dispatcher.HasShutdownFinished)
            {
                return;
            }

            dispatcher.BeginInvoke(action);
        }

        private void ScheduleRetry()
        {
            consecutiveFailures++;

            // O atraso cresce até o limite para não repetir avisos e gravações a cada segundo.
            int exponent = Math.Min(consecutiveFailures - 1, 5);

            double delaySeconds = Math.Min(
                AutoSaveDelay.TotalSeconds * Math.Pow(2, exponent),
                MaxRetryDelay.TotalSeconds);

            retryDelay = TimeSpan.FromSeconds(delaySeconds);

            autoSaveTimer.Stop();
            autoSaveTimer.Interval = retryDelay;
            autoSaveTimer.Start();
        }

        private void ResetRetryState()
        {
            consecutiveFailures = 0;
            failureReported = false;
            retryDelay = AutoSaveDelay;

            autoSaveTimer.Interval = AutoSaveDelay;
        }

        public void StopAutoSave()
        {
            autoSaveTimer.Stop();
        }

        public void Dispose()
        {
            autoSaveTimer.Stop();
            autoSaveTimer.Tick -= AutoSaveTimer_Tick;

            autoSaver.SaveSucceeded -= AutoSaver_SaveSucceeded;
            autoSaver.SaveFailed -= AutoSaver_SaveFailed;
            autoSaver.Dispose();

            Notes.CollectionChanged -= OnCollectionChanged;

            foreach (Note note in trackedNotes)
            {
                note.PropertyChanged -= OnNotePropertyChanged;
            }

            trackedNotes.Clear();
        }
    }
}
