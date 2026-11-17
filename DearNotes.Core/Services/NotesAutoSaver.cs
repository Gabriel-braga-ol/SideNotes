using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DearNotes.Models;

namespace DearNotes.Services
{
    // Mantém uma única gravação em andamento e enfileira apenas o snapshot mais recente.
    public sealed class NotesAutoSaver
    {
        private readonly INoteStore noteStore;

        private readonly object syncRoot = new();

        private readonly SemaphoreSlim writeGate = new(1, 1);

        private readonly List<SaveWaiter> waiters = new();

        private SaveRequest? pendingRequest;

        private long latestVersion;

        private long lastSavedVersion;

        private bool isPumping;

        private bool isDisposed;

        public event Action? SaveSucceeded;

        public event Action<Exception>? SaveFailed;

        public NotesAutoSaver(INoteStore noteStore)
        {
            this.noteStore = noteStore;
        }

        public void Enqueue(IReadOnlyList<Note> notes)
        {
            lock (syncRoot)
            {
                if (isDisposed) return;

                pendingRequest = new SaveRequest(
                    notes,
                    ++latestVersion,
                    isFlush: false);
            }

            StartPumpIfIdle();
        }

        public Task<bool> FlushAsync(IReadOnlyList<Note> notes)
        {
            var completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            lock (syncRoot)
            {
                if (isDisposed)
                {
                    completion.TrySetResult(false);
                    return completion.Task;
                }

                var request = new SaveRequest(
                    notes,
                    ++latestVersion,
                    isFlush: true);

                pendingRequest = request;

                waiters.Add(new SaveWaiter(
                    request.Version,
                    completion));
            }

            StartPumpIfIdle();

            return completion.Task;
        }

        // Usado apenas no encerramento de emergência, quando não é possível aguardar a fila.
        public void SaveNow(IReadOnlyList<Note> notes)
        {
            long version;

            lock (syncRoot)
            {
                if (isDisposed) return;

                version = ++latestVersion;
            }

            writeGate.Wait();

            try
            {
                lock (syncRoot)
                {
                    if (version <= lastSavedVersion) return;
                }

                noteStore.Save(notes);

                lock (syncRoot)
                {
                    lastSavedVersion = Math.Max(
                        lastSavedVersion,
                        version);
                }
            }
            finally
            {
                writeGate.Release();
            }
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                isDisposed = true;
                pendingRequest = null;

                CompleteAllWaiters(success: false);
            }
        }

        private void StartPumpIfIdle()
        {
            lock (syncRoot)
            {
                if (isPumping || isDisposed) return;

                isPumping = true;
            }

            _ = Task.Run(PumpAsync);
        }

        private async Task PumpAsync()
        {
            while (true)
            {
                SaveRequest? request;

                lock (syncRoot)
                {
                    if (isDisposed)
                    {
                        isPumping = false;
                        return;
                    }

                    request = pendingRequest;
                    pendingRequest = null;
                }

                if (request is null)
                {
                    lock (syncRoot)
                    {
                        if (pendingRequest is null)
                        {
                            isPumping = false;
                            return;
                        }
                    }

                    continue;
                }

                if (TryWrite(request, out Exception? failure))
                {
                    lock (syncRoot)
                    {
                        lastSavedVersion = Math.Max(
                            lastSavedVersion,
                            request.Version);

                        CompleteWaiters(
                            request.Version,
                            success: true);
                    }

                    SaveSucceeded?.Invoke();
                    continue;
                }

                bool retryPendingFlush;

                lock (syncRoot)
                {
                    retryPendingFlush = pendingRequest is { IsFlush: true };

                    if (!retryPendingFlush)
                    {
                        CompleteAllWaiters(success: false);
                        isPumping = false;
                    }
                }

                if (failure is not null)
                {
                    SaveFailed?.Invoke(failure);
                }

                if (!retryPendingFlush) return;
            }
        }

        private bool TryWrite(
            SaveRequest request,
            out Exception? failure)
        {
            failure = null;

            writeGate.Wait();

            try
            {
                lock (syncRoot)
                {
                    if (request.Version <= lastSavedVersion) return true;
                }

                noteStore.Save(request.Notes);

                return true;
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is CryptographicException)
            {
                failure = ex;
                return false;
            }
            finally
            {
                writeGate.Release();
            }
        }

        private void CompleteWaiters(long version, bool success)
        {
            for (int index = waiters.Count - 1; index >= 0; --index)
            {
                SaveWaiter waiter = waiters[index];

                if (waiter.Version > version) continue;

                waiter.Completion.TrySetResult(success);
                waiters.RemoveAt(index);
            }
        }

        private void CompleteAllWaiters(bool success)
        {
            foreach (SaveWaiter waiter in waiters)
            {
                waiter.Completion.TrySetResult(success);
            }

            waiters.Clear();
        }

        private sealed class SaveRequest
        {
            public SaveRequest(
                IReadOnlyList<Note> notes,
                long version,
                bool isFlush)
            {
                Notes = notes;
                Version = version;
                IsFlush = isFlush;
            }

            public IReadOnlyList<Note> Notes { get; }

            public long Version { get; }

            public bool IsFlush { get; }
        }

        private sealed class SaveWaiter
        {
            public SaveWaiter(
                long version,
                TaskCompletionSource<bool> completion)
            {
                Version = version;
                Completion = completion;
            }

            public long Version { get; }

            public TaskCompletionSource<bool> Completion { get; }
        }
    }
}
