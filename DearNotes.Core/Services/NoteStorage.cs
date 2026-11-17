using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using DearNotes.Models;

namespace DearNotes.Services
{
    public sealed class NoteStorage : INoteStore
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        private readonly string folderPath;

        private readonly string filePath;

        public NoteStorage()
            : this(AppPaths.NotesFilePath)
        {
        }

        public NoteStorage(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            this.filePath = Path.GetFullPath(filePath);

            folderPath = Path.GetDirectoryName(this.filePath)
                ?? throw new ArgumentException(
                    "O caminho do arquivo precisa conter uma pasta.",
                    nameof(filePath));
        }

        public void Save(IReadOnlyList<Note> notes)
        {
            Directory.CreateDirectory(folderPath);

            string json = JsonSerializer.Serialize(
                notes,
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
                List<Note> notes =
                    JsonSerializer.Deserialize<List<Note>>(json)
                    ?? throw new JsonException(
                        "O arquivo não contém uma lista de notas.");

                foreach (Note note in notes)
                {
                    if (note is null)
                    {
                        throw new JsonException(
                            "O arquivo contém uma nota nula.");
                    }

                    if (note.Title is null || note.Content is null)
                    {
                        throw new JsonException(
                            "O arquivo contém uma nota com título ou conteúdo nulo.");
                    }

                    if (note.Color is null ||
                        !Regex.IsMatch(note.Color, @"\A#[0-9a-fA-F]{6}\z"))
                    {
                        throw new JsonException(
                            "Uma nota contém uma cor inválida.");
                    }
                }

                return notes;
            }
            catch (JsonException)
            {
                string backupPath = Path.Combine(
                    folderPath,
                    $"notes-invalid-{Guid.NewGuid():N}.json");

                File.Copy(filePath, backupPath);

                warningMessage =
                    "Não foi possível carregar as notas porque o arquivo " +
                    "contém dados inválidos.\n\n" +
                    "Uma cópia do arquivo foi preservada em: \n" +
                    backupPath;

                return new List<Note>();
            }
        }
    }
}
