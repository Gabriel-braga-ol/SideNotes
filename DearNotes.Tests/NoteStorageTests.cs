using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using DearNotes.Models;
using DearNotes.Services;

namespace DearNotes.Tests
{
    public sealed class NoteStorageTests
    {
        private static readonly byte[] testKey =
            Enumerable.Repeat((byte)0x42, 32).ToArray();

        [Fact]
        public void Save_ThenLoad_RestoresAllFields()
        {
            using var directory = new TemporaryDirectory();
            INoteEncryption encryption = CreateEncryption();
            var storage = new NoteStorage(
                directory.Combine("notes.json"),
                encryption);

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
        public void Save_WritesEncryptedEnvelopeWithVersionedDocument()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");
            INoteEncryption encryption = CreateEncryption();
            var storage = new NoteStorage(filePath, encryption);
            var note = new Note
            {
                Title = "Nota",
                Content = "Texto",
                Color = "#112233"
            };

            storage.Save(new List<Note> { note });

            string fileContent = File.ReadAllText(filePath);

            using JsonDocument envelope = JsonDocument.Parse(fileContent);
            JsonElement root = envelope.RootElement;

            Assert.Equal(JsonValueKind.Object, root.ValueKind);
            Assert.Equal(
                "dpapi-aes-gcm",
                root.GetProperty("Protection").GetString());
            Assert.Equal(
                1,
                root.GetProperty("FormatVersion").GetInt32());
            Assert.False(root.TryGetProperty("SchemaVersion", out _));
            Assert.DoesNotContain("Nota", fileContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Texto", fileContent, StringComparison.Ordinal);

            NotesDocument document = DecryptDocument(root, encryption);

            Assert.Equal(
                NotesDocument.CurrentSchemaVersion,
                document.SchemaVersion);
            NoteDocument savedNote = Assert.Single(document.Notes);
            Assert.Equal(note.Id, savedNote.Id);
            Assert.Equal(note.CreatedAt, savedNote.CreatedAt);
            Assert.Equal(note.Title, savedNote.Title);
        }

        [Fact]
        public void Load_WhenFileContainsLegacyArray_MigratesNotes()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");
            File.WriteAllText(
                filePath,
                """[{"Title":"Antiga","Content":"Texto","Color":"#112233","IsPinnedToDesk":true}]""");

            var storage = new NoteStorage(filePath, CreateEncryption());

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
        public void Load_WhenFileContainsPlaintextDocument_LoadsNotes()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");
            File.WriteAllText(
                filePath,
                """{"SchemaVersion":1,"Notes":[{"Title":"Atual","Content":"Texto","Color":"#112233","IsPinnedToDesk":false}]}""");

            var storage = new NoteStorage(filePath, CreateEncryption());

            IReadOnlyList<Note> loaded = storage.Load(out string? warning);

            Assert.Null(warning);

            Note note = Assert.Single(loaded);

            Assert.Equal("Atual", note.Title);
            Assert.NotEqual(Guid.Empty, note.Id);

            storage.Save(loaded);

            using JsonDocument envelope = JsonDocument.Parse(
                File.ReadAllText(filePath));

            Assert.True(
                envelope.RootElement.TryGetProperty(
                    "Protection",
                    out _));
        }

        [Fact]
        public void Load_WhenSchemaVersionIsUnsupported_PreservesBackupAndReturnsEmpty()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");
            File.WriteAllText(
                filePath,
                """{"SchemaVersion":2,"Notes":[]}""");

            var storage = new NoteStorage(filePath, CreateEncryption());

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

            var storage = new NoteStorage(filePath, CreateEncryption());

            IReadOnlyList<Note> loaded = storage.Load(out string? warning);

            Assert.Empty(loaded);
            Assert.NotNull(warning);
            Assert.Single(
                Directory.GetFiles(directory.Path, "notes-invalid-*.json"));
        }

        [Fact]
        public void Load_WhenEnvelopeIsTampered_ThrowsCryptographicException()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");
            INoteEncryption encryption = CreateEncryption();
            var storage = new NoteStorage(filePath, encryption);

            storage.Save(new List<Note> { new() { Title = "Nota" } });

            string originalJson = File.ReadAllText(filePath);

            // Evita depender da codificação JSON: altera o primeiro caractere
            // Base64 do texto cifrado diretamente no arquivo.
            const string ciphertextMarker = "\"Ciphertext\": \"";
            int markerIndex = originalJson.IndexOf(
                ciphertextMarker,
                StringComparison.Ordinal);

            Assert.True(markerIndex >= 0);

            int ciphertextIndex = markerIndex + ciphertextMarker.Length;
            char firstCharacter = originalJson[ciphertextIndex];
            char replacement = firstCharacter == 'A' ? 'B' : 'A';

            string tamperedJson =
                originalJson[..ciphertextIndex] +
                replacement +
                originalJson[(ciphertextIndex + 1)..];

            Assert.NotEqual(originalJson, tamperedJson);

            File.WriteAllText(filePath, tamperedJson);

            Assert.ThrowsAny<CryptographicException>(
                () => storage.Load(out _));
        }

        [Fact]
        public void Load_WhenKeyDoesNotMatch_ThrowsCryptographicException()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");

            var writer = new NoteStorage(filePath, CreateEncryption());
            writer.Save(new List<Note> { new() { Title = "Nota" } });

            byte[] otherKey = Enumerable.Repeat((byte)0x17, 32).ToArray();
            var reader = new NoteStorage(
                filePath,
                new AesGcmNoteEncryption(new FixedKeyProvider(otherKey)));

            Assert.ThrowsAny<CryptographicException>(
                () => reader.Load(out _));
        }

        [Fact]
        public void Load_WhenBackupsExceedLimit_KeepsFiveNewest()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("notes.json");

            for (int index = 1; index <= 7; ++index)
            {
                string backupPath = directory.Combine(
                    $"notes-invalid-{index:00}.json");
                File.WriteAllText(backupPath, "{}");
                File.SetLastWriteTimeUtc(
                    backupPath,
                    DateTime.UtcNow.AddDays(-index));
            }

            File.WriteAllText(filePath, "{ isto não é json }");

            var storage = new NoteStorage(filePath, CreateEncryption());

            IReadOnlyList<Note> loaded = storage.Load(out string? warning);

            Assert.Empty(loaded);
            Assert.NotNull(warning);

            string[] backups = Directory.GetFiles(
                directory.Path,
                "notes-invalid-*.json");

            Assert.Equal(
                5,
                backups.Length);
            Assert.DoesNotContain(
                backups,
                path => path.EndsWith(
                    "notes-invalid-05.json",
                    StringComparison.Ordinal));
            Assert.DoesNotContain(
                backups,
                path => path.EndsWith(
                    "notes-invalid-06.json",
                    StringComparison.Ordinal));
            Assert.DoesNotContain(
                backups,
                path => path.EndsWith(
                    "notes-invalid-07.json",
                    StringComparison.Ordinal));

            for (int index = 1; index <= 4; ++index)
            {
                Assert.Contains(
                    backups,
                    path => path.EndsWith(
                        $"notes-invalid-{index:00}.json",
                        StringComparison.Ordinal));
            }
        }

        [Fact]
        public void Save_DoesNotLeaveTemporaryFile()
        {
            using var directory = new TemporaryDirectory();
            var storage = new NoteStorage(
                directory.Combine("notes.json"),
                CreateEncryption());

            storage.Save(new List<Note> { new() { Title = "Nota" } });

            Assert.True(File.Exists(directory.Combine("notes.json")));
            Assert.False(File.Exists(directory.Combine("notes.json.tmp")));
        }

        [Fact]
        public void Save_CreatesMissingDirectory()
        {
            using var directory = new TemporaryDirectory();
            string nested = Path.Combine(directory.Path, "dados");
            var storage = new NoteStorage(
                Path.Combine(nested, "notes.json"),
                CreateEncryption());

            storage.Save(new List<Note> { new() });

            Assert.True(File.Exists(Path.Combine(nested, "notes.json")));
        }

        [Fact]
        public void Load_WhenFileDoesNotExist_ReturnsEmptyWithoutWarning()
        {
            using var directory = new TemporaryDirectory();
            var storage = new NoteStorage(
                directory.Combine("notes.json"),
                CreateEncryption());

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

            var storage = new NoteStorage(filePath, CreateEncryption());

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

            var storage = new NoteStorage(filePath, CreateEncryption());

            IReadOnlyList<Note> loaded = storage.Load(out string? warning);

            Assert.Empty(loaded);
            Assert.NotNull(warning);
            Assert.Single(
                Directory.GetFiles(directory.Path, "notes-invalid-*.json"));
        }

        private static AesGcmNoteEncryption CreateEncryption()
        {
            return new AesGcmNoteEncryption(
                new FixedKeyProvider(testKey));
        }

        private static NotesDocument DecryptDocument(
            JsonElement envelope,
            INoteEncryption encryption)
        {
            var payload = new EncryptedNotePayload(
                envelope.GetProperty("KeyId").GetString()
                    ?? throw new InvalidOperationException(
                        "O envelope não contém o identificador da chave."),
                Convert.FromBase64String(
                    envelope.GetProperty("Nonce").GetString()
                        ?? throw new InvalidOperationException(
                            "O envelope não contém o nonce.")),
                Convert.FromBase64String(
                    envelope.GetProperty("Tag").GetString()
                        ?? throw new InvalidOperationException(
                            "O envelope não contém a tag.")),
                Convert.FromBase64String(
                    envelope.GetProperty("Ciphertext").GetString()
                        ?? throw new InvalidOperationException(
                            "O envelope não contém o texto cifrado.")));

            byte[] plaintext = encryption.Decrypt(payload);

            try
            {
                return JsonSerializer.Deserialize<NotesDocument>(plaintext)
                    ?? throw new InvalidOperationException(
                        "O documento decifrado está vazio.");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }
    }
}
