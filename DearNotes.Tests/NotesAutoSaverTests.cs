using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DearNotes.Models;
using DearNotes.Services;

namespace DearNotes.Tests
{
    public sealed class NotesAutoSaverTests
    {
        private static readonly TimeSpan testTimeout =
            TimeSpan.FromSeconds(5);

        [Fact]
        public async Task FlushAsync_SavesSnapshotAndReturnsTrue()
        {
            var store = new RecordingNoteStore();
            var saver = new NotesAutoSaver(store);

            bool saved = await saver
                .FlushAsync(CreateNotes("Nota"))
                .WaitAsync(testTimeout);

            Assert.True(saved);
            Assert.Single(store.Saved);
            Assert.Equal("Nota", store.Saved[0][0].Title);
        }

        [Fact]
        public async Task FlushAsync_WhenStoreFails_ReturnsFalseAndReportsFailure()
        {
            var store = new RecordingNoteStore
            {
                Failure = new IOException("disco indisponível")
            };

            var saver = new NotesAutoSaver(store);
            Exception? reportedFailure = null;

            saver.SaveFailed += exception =>
                reportedFailure = exception;

            bool saved = await saver
                .FlushAsync(CreateNotes("Nota"))
                .WaitAsync(testTimeout);

            Assert.False(saved);
            Assert.IsType<IOException>(reportedFailure);
        }

        [Fact]
        public async Task FlushAsync_WhenStoreThrowsUnexpectedException_ReleasesWaitersAndKeepsSaving()
        {
            var store = new RecordingNoteStore
            {
                Failure = new InvalidOperationException("falha inesperada")
            };

            var saver = new NotesAutoSaver(store);
            var failureReported =
                new TaskCompletionSource<Exception>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            saver.SaveFailed += exception =>
                failureReported.TrySetResult(exception);

            bool failedSave = await saver
                .FlushAsync(CreateNotes("Falha"))
                .WaitAsync(testTimeout);

            Exception reportedFailure = await failureReported
                .Task
                .WaitAsync(testTimeout);

            Assert.False(failedSave);
            Assert.IsType<InvalidOperationException>(reportedFailure);

            store.Failure = null;

            bool nextSave = await saver
                .FlushAsync(CreateNotes("Recuperada"))
                .WaitAsync(testTimeout);

            Assert.True(nextSave);
            Assert.Equal("Recuperada", store.Saved[^1][0].Title);
        }

        [Fact]
        public async Task Enqueue_WhileSaving_WritesOnlyLatestPendingSnapshot()
        {
            var store = new RecordingNoteStore
            {
                BlockFirstSave = true
            };

            var saver = new NotesAutoSaver(store);

            saver.Enqueue(CreateNotes("A"));

            await store.FirstSaveStarted.WaitAsync(testTimeout);

            saver.Enqueue(CreateNotes("B"));
            saver.Enqueue(CreateNotes("C"));

            store.ReleaseFirstSave();

            bool saved = await saver
                .FlushAsync(CreateNotes("C"))
                .WaitAsync(testTimeout);

            Assert.True(saved);

            IReadOnlyList<Note>[] writes = store.Saved;

            Assert.Equal("A", writes[0][0].Title);
            Assert.Equal("C", writes[^1][0].Title);
            Assert.DoesNotContain(
                writes,
                notes => notes[0].Title == "B");
        }

        [Fact]
        public async Task SaveNow_WaitsForInFlightWriteAndWritesLatestSnapshot()
        {
            var store = new RecordingNoteStore
            {
                BlockFirstSave = true
            };

            var saver = new NotesAutoSaver(store);

            saver.Enqueue(CreateNotes("Antiga"));

            await store.FirstSaveStarted.WaitAsync(testTimeout);

            Task saveNow = Task.Run(
                () => saver.SaveNow(CreateNotes("Recente")));

            Assert.False(saveNow.IsCompleted);

            store.ReleaseFirstSave();

            await saveNow.WaitAsync(testTimeout);

            Assert.Equal(2, store.Saved.Length);
            Assert.Equal("Antiga", store.Saved[0][0].Title);
            Assert.Equal("Recente", store.Saved[1][0].Title);
        }

        private static List<Note> CreateNotes(string title)
        {
            return new List<Note>
            {
                new() { Title = title, Content = string.Empty }
            };
        }

        private sealed class RecordingNoteStore : INoteStore
        {
            private readonly object syncRoot = new();

            private readonly List<IReadOnlyList<Note>> saved = new();

            private readonly TaskCompletionSource<bool> firstSaveStarted =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            private readonly TaskCompletionSource<bool> releaseFirstSave =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool BlockFirstSave { get; init; }

            public Exception? Failure { get; set; }

            public Task FirstSaveStarted => firstSaveStarted.Task;

            public IReadOnlyList<Note>[] Saved
            {
                get
                {
                    lock (syncRoot)
                    {
                        return saved.ToArray();
                    }
                }
            }

            public void ReleaseFirstSave()
            {
                releaseFirstSave.TrySetResult(true);
            }

            public IReadOnlyList<Note> Load(out string? warningMessage)
            {
                warningMessage = null;
                return Array.Empty<Note>();
            }

            public void Save(IReadOnlyList<Note> notes)
            {
                bool isFirstSave;

                lock (syncRoot)
                {
                    isFirstSave = saved.Count == 0;
                }

                if (BlockFirstSave && isFirstSave)
                {
                    firstSaveStarted.TrySetResult(true);
                    releaseFirstSave.Task.GetAwaiter().GetResult();
                }

                if (Failure is not null) throw Failure;

                lock (syncRoot)
                {
                    saved.Add(notes);
                }
            }
        }
    }
}
