using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DearNotes.Models;
using DearNotes.Services;

namespace DearNotes.Tests
{
    public sealed class NoteStorageTests
    {
        [Fact]
        public void Save_ThenLoad_RestoresAllFields()
        {
            using var directory = new TemporaryDirectory();
            var storage = new NoteStorage(directory.Combine("notes.json"));

            var notes = new List<Note>
            {
                new()
                {
                    Title = "Compras",
                    Content = "Leite",
                    Color = "#112233",
                    IsPinnedToDesk = true
                },
                new()
                {
                    Title = string.Empty,
                    Content = string.Empty,
                    Color = "#AABBCC",
                    IsPinnedToDesk = false
                }
            };

            storage.Save(notes);

            IReadOnlyList<Note> loaded = storage.Load(out string? warning);

            Assert.Null(warning);
            Assert.Equal(2, loaded.Count);
            Assert.Equal("Compras", loaded[0].Title);
            Assert.Equal("Leite", loaded[0].Content);
            Assert.Equal("#112233", loaded[0].Color);
            Assert.True(loaded[0].IsPinnedToDesk);
            Assert.Equal(notes[0].Id, loaded[0].Id);
            Assert.Equal(notes[0].CreatedAt, loaded[0].CreatedAt);
            Assert.Equal(notes[0].UpdatedAt, loaded[0].UpdatedAt);
            Assert.Equal("#AABBCC", loaded[1].Color);
            Assert.False(loaded[1].IsPinnedToDesk);
        }

        [Fact]
        public void Save_WritesVersionedDocumentWithMetadata()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");
            var storage = new NoteStorage(filePath);
            var note = new Note
            {
                Title = "Nota",
                Content = "Texto",
                Color = "#112233"
            };

            storage.Save(new List<Note> { note });

            using JsonDocument document = JsonDocument.Parse(
                File.ReadAllText(filePath));

            JsonElement root = document.RootElement;

            Assert.Equal(JsonValueKind.Object, root.ValueKind);
            Assert.Equal(
                NotesDocument.CurrentSchemaVersion,
                root.GetProperty("SchemaVersion").GetInt32());

            JsonElement notes = root.GetProperty("Notes");

            Assert.Equal(JsonValueKind.Array, notes.ValueKind);
            Assert.Equal(1, notes.GetArrayLength());

            JsonElement savedNote = notes[0];

            Assert.Equal(note.Id, savedNote.GetProperty("Id").GetGuid());
            Assert.Equal(
                note.CreatedAt,
                savedNote.GetProperty("CreatedAt").GetDateTimeOffset());
        }

        [Fact]
        public void Load_WhenFileContainsLegacyArray_MigratesNotes()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");
            File.WriteAllText(
                filePath,
                """[{"Title":"Antiga","Content":"Texto","Color":"#112233","IsPinnedToDesk":true}]""");

            var storage = new NoteStorage(filePath);

            IReadOnlyList<Note> loaded = storage.Load(out string? warning);

            Assert.Null(warning);

            Note note = Assert.Single(loaded);

            Assert.Equal("Antiga", note.Title);
            Assert.Equal("Texto", note.Content);
            Assert.Equal("#112233", note.Color);
            Assert.True(note.IsPinnedToDesk);
            Assert.NotEqual(Guid.Empty, note.Id);
            Assert.NotEqual(default, note.CreatedAt);
            Assert.True(note.UpdatedAt >= note.CreatedAt);
        }

        [Fact]
        public void Load_WhenSchemaVersionIsUnsupported_PreservesBackupAndReturnsEmpty()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");
            File.WriteAllText(
                filePath,
                """{"SchemaVersion":2,"Notes":[]}""");

            var storage = new NoteStorage(filePath);

            IReadOnlyList<Note> loaded = storage.Load(out string? warning);

            Assert.Empty(loaded);
            Assert.NotNull(warning);
            Assert.Single(
                Directory.GetFiles(directory.Path, "notes-invalid-*.json"));
        }

        [Fact]
        public void Load_WhenVersionedDocumentHasNoSchemaVersion_PreservesBackup()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");
            File.WriteAllText(filePath, """{"Notes":[]}""");

            var storage = new NoteStorage(filePath);

            IReadOnlyList<Note> loaded = storage.Load(out string? warning);

            Assert.Empty(loaded);
            Assert.NotNull(warning);
            Assert.Single(
                Directory.GetFiles(directory.Path, "notes-invalid-*.json"));
        }

        [Fact]
        public void Save_DoesNotLeaveTemporaryFile()
        {
            using var directory = new TemporaryDirectory();
            var storage = new NoteStorage(directory.Combine("notes.json"));

            storage.Save(new List<Note> { new() { Title = "Nota" } });

            Assert.True(File.Exists(directory.Combine("notes.json")));
            Assert.False(File.Exists(directory.Combine("notes.json.tmp")));
        }

        [Fact]
        public void Save_CreatesMissingDirectory()
        {
            using var directory = new TemporaryDirectory();
            string nested = Path.Combine(directory.Path, "dados");
            var storage = new NoteStorage(Path.Combine(nested, "notes.json"));

            storage.Save(new List<Note> { new() });

            Assert.True(File.Exists(Path.Combine(nested, "notes.json")));
        }

        [Fact]
        public void Load_WhenFileDoesNotExist_ReturnsEmptyWithoutWarning()
        {
            using var directory = new TemporaryDirectory();
            var storage = new NoteStorage(directory.Combine("notes.json"));

            IReadOnlyList<Note> loaded = storage.Load(out string? warning);

            Assert.Empty(loaded);
            Assert.Null(warning);
        }

        [Fact]
        public void Load_WhenJsonIsInvalid_PreservesBackupAndReturnsEmpty()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");
            File.WriteAllText(filePath, "{ isto não é json }");

            var storage = new NoteStorage(filePath);

            IReadOnlyList<Note> loaded = storage.Load(out string? warning);

            Assert.Empty(loaded);
            Assert.NotNull(warning);
            Assert.Contains(directory.Path, warning);
            Assert.Single(
                Directory.GetFiles(directory.Path, "notes-invalid-*.json"));
        }

        [Fact]
        public void Load_WhenColorIsInvalid_PreservesBackupAndReturnsEmpty()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");
            File.WriteAllText(
                filePath,
                """[{"Title":"Nota","Content":"Texto","Color":"vermelho"}]""");

            var storage = new NoteStorage(filePath);

            IReadOnlyList<Note> loaded = storage.Load(out string? warning);

            Assert.Empty(loaded);
            Assert.NotNull(warning);
            Assert.Single(
                Directory.GetFiles(directory.Path, "notes-invalid-*.json"));
        }
    }
}
