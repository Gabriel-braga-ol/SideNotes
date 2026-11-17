using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using DearNotes.Models;

namespace DearNotes.Services
{
    public sealed class NoteStorage : INoteStore
    {
        private const string ColorPattern = @"\A#[0-9a-fA-F]{6}\z";

        private const int MaxInvalidBackups = 5;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        private readonly string folderPath;

        private readonly string filePath;

        private readonly INoteEncryption encryption;

        public NoteStorage(INoteEncryption encryption)
            : this(AppPaths.NotesFilePath, encryption)
        {
        }

        public NoteStorage(
            string filePath,
            INoteEncryption encryption)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            ArgumentNullException.ThrowIfNull(encryption);

            this.filePath = Path.GetFullPath(filePath);
            this.encryption = encryption;

            folderPath = Path.GetDirectoryName(this.filePath)
                ?? throw new ArgumentException(
                    "O caminho do arquivo precisa conter uma pasta.",
                    nameof(filePath));
        }

        public void Save(IReadOnlyList<Note> notes)
        {
            Directory.CreateDirectory(folderPath);

            var document = new NotesDocument
            {
                SchemaVersion = NotesDocument.CurrentSchemaVersion,
                Notes = notes
                    .Select(ToDocument)
                    .ToList()
            };

            byte[] plaintext = JsonSerializer.SerializeToUtf8Bytes(
                document,
                SerializerOptions);

            EncryptedNotePayload payload;

            try
            {
                payload = encryption.Encrypt(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }

            var protectedFile = new ProtectedNotesFile
            {
                Protection = ProtectedNotesFile.ProtectionName,
                FormatVersion = ProtectedNotesFile.CurrentFormatVersion,
                KeyId = payload.KeyId,
                Nonce = Convert.ToBase64String(payload.Nonce),
                Tag = Convert.ToBase64String(payload.Tag),
                Ciphertext = Convert.ToBase64String(payload.Ciphertext)
            };

            string json = JsonSerializer.Serialize(
                protectedFile,
                SerializerOptions);

            string temporaryPath = filePath + ".tmp";

            File.WriteAllText(temporaryPath, json);

            if (File.Exists(filePath))
            {
                File.Replace(temporaryPath, filePath, null);
            }
            else
            {
                File.Move(temporaryPath, filePath);
            }
        }

        public IReadOnlyList<Note> Load(out string? warningMessage)
        {
            warningMessage = null;

            string json;

            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (FileNotFoundException)
            {
                return new List<Note>();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<Note>();
            }

            try
            {
                NotesDocument document = ReadDocument(json);

                return CreateNotes(document);
            }
            catch (JsonException)
            {
                return RecoverInvalidFile(out warningMessage);
            }
        }

        private NotesDocument ReadDocument(string json)
        {
            using JsonDocument parsed = JsonDocument.Parse(json);

            return parsed.RootElement.ValueKind switch
            {
                JsonValueKind.Array => ReadLegacyDocument(parsed.RootElement),
                JsonValueKind.Object => ReadDocumentObject(parsed.RootElement),
                _ => throw new JsonException(
                    "O arquivo não contém um documento de notas.")
            };
        }

        private NotesDocument ReadDocumentObject(JsonElement root)
        {
            if (root.TryGetProperty("Protection", out _))
            {
                return ReadProtectedDocument(root);
            }

            return ReadCurrentDocument(root);
        }

        private NotesDocument ReadProtectedDocument(JsonElement root)
        {
            ProtectedNotesFile? file =
                root.Deserialize<ProtectedNotesFile>();

            if (file is null ||
                file.Protection != ProtectedNotesFile.ProtectionName ||
                file.FormatVersion != ProtectedNotesFile.CurrentFormatVersion)
            {
                throw new JsonException(
                    "O envelope de criptografia não é suportado.");
            }

            EncryptedNotePayload payload = CreatePayload(file);

            byte[] plaintext = encryption.Decrypt(payload);

            try
            {
                using JsonDocument inner = JsonDocument.Parse(plaintext);

                return ReadCurrentDocument(inner.RootElement);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }

        private static EncryptedNotePayload CreatePayload(
            ProtectedNotesFile file)
        {
            if (string.IsNullOrWhiteSpace(file.KeyId) ||
                file.Nonce is null ||
                file.Tag is null ||
                file.Ciphertext is null)
            {
                throw new JsonException(
                    "O envelope de criptografia está incompleto.");
            }

            try
            {
                return new EncryptedNotePayload(
                    file.KeyId,
                    Convert.FromBase64String(file.Nonce),
                    Convert.FromBase64String(file.Tag),
                    Convert.FromBase64String(file.Ciphertext));
            }
            catch (FormatException)
            {
                throw new JsonException(
                    "O envelope de criptografia está inválido.");
            }
        }

        // Versões anteriores gravavam apenas a lista de notas, sem metadados.
        private static NotesDocument ReadLegacyDocument(JsonElement root)
        {
            List<NoteDocument>? notes =
                root.Deserialize<List<NoteDocument>>();

            if (notes is null)
            {
                throw new JsonException(
                    "O arquivo não contém uma lista de notas.");
            }

            return new NotesDocument
            {
                SchemaVersion = 0,
                Notes = notes
            };
        }

        private static NotesDocument ReadCurrentDocument(JsonElement root)
        {
            if (!root.TryGetProperty("SchemaVersion", out _) ||
                !root.TryGetProperty("Notes", out _))
            {
                throw new JsonException(
                    "O arquivo não contém um documento de notas.");
            }

            NotesDocument? document =
                root.Deserialize<NotesDocument>();

            if (document is null || document.Notes is null)
            {
                throw new JsonException(
                    "O arquivo não contém um documento de notas.");
            }

            if (document.SchemaVersion !=
                NotesDocument.CurrentSchemaVersion)
            {
                throw new JsonException(
                    "A versão do formato de notas não é suportada.");
            }

            return document;
        }

        private static List<Note> CreateNotes(NotesDocument document)
        {
            var notes = new List<Note>(document.Notes.Count);

            foreach (NoteDocument? note in document.Notes)
            {
                if (note is null)
                {
                    throw new JsonException(
                        "O arquivo contém uma nota nula.");
                }

                notes.Add(CreateNote(note));
            }

            return notes;
        }

        private static Note CreateNote(NoteDocument document)
        {
            if (document.Title is null || document.Content is null)
            {
                throw new JsonException(
                    "O arquivo contém uma nota com título ou conteúdo nulo.");
            }

            if (document.Color is null ||
                !Regex.IsMatch(document.Color, ColorPattern))
            {
                throw new JsonException(
                    "Uma nota contém uma cor inválida.");
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;

            DateTimeOffset createdAt = document.CreatedAt == default
                ? now
                : document.CreatedAt;

            DateTimeOffset updatedAt = document.UpdatedAt == default
                ? createdAt
                : document.UpdatedAt;

            if (updatedAt < createdAt)
            {
                updatedAt = createdAt;
            }

            Guid id = document.Id == Guid.Empty
                ? Guid.NewGuid()
                : document.Id;

            return new Note(
                id,
                document.Title,
                document.Content,
                document.Color,
                document.IsPinnedToDesk,
                createdAt,
                updatedAt);
        }

        private static NoteDocument ToDocument(Note note)
        {
            return new NoteDocument
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Content,
                Color = note.Color,
                IsPinnedToDesk = note.IsPinnedToDesk,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            };
        }

        private List<Note> RecoverInvalidFile(out string? warningMessage)
        {
            string backupPath = Path.Combine(
                folderPath,
                $"notes-invalid-{Guid.NewGuid():N}.json");

            File.Copy(filePath, backupPath);

            PruneInvalidBackups();

            warningMessage =
                "Não foi possível carregar as notas porque o arquivo " +
                "contém dados inválidos.\n\n" +
                "Uma cópia do arquivo foi preservada em: \n" +
                backupPath;

            return new List<Note>();
        }

        private void PruneInvalidBackups()
        {
            try
            {
                FileInfo[] expiredBackups = new DirectoryInfo(folderPath)
                    .GetFiles("notes-invalid-*.json")
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .Skip(MaxInvalidBackups)
                    .ToArray();

                foreach (FileInfo backup in expiredBackups)
                {
                    backup.Delete();
                }
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                Debug.WriteLine(
                    $"Não foi possível remover backups antigos: {ex.Message}");
            }
        }
    }
}
