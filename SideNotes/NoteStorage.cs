using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace SideNotes
{
    public static class NoteStorage 
    {
        private static readonly string FolderPath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SideNotes");
        
        private static readonly string FilePath =
            Path.Combine(FolderPath,  "notes.json");

        public static void Save(List<Note> notes)
        {
            Directory.CreateDirectory(FolderPath);
            
            string json = JsonSerializer.Serialize(notes, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string temporaryPath = FilePath + ".tmp";
            
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(FilePath))
            {
                File.Replace(temporaryPath, FilePath, null);
            }
            else
            {
                File.Move(temporaryPath, FilePath);
            }
        }

        public static List<Note> Load(out string? warningMessage)
        {
            warningMessage = null;

            string json;

            try
            {
                json = File.ReadAllText(FilePath);
            }
            catch (FileNotFoundException)
            {
                return new List<Note>();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<Note>();
            };

            try
            {
                List<Note> notes = JsonSerializer.Deserialize<List<Note>>(json)
                    ?? throw new JsonException("O arquivo não contém uma lista de notas.");

                foreach (Note note in notes)
                {
                    if (note is null)
                    {
                        throw new JsonException("O arquivo contém uma nota nula.");
                    }

                    if (note.Title is null || note.Content is null)
                    {
                        throw new JsonException("O arquivo contém uma nota com título ou conteúdo nulo.");
                    }

                    if (note.Color is null || !Regex.IsMatch(note.Color, @"\A#[0-9a-fA-F]{6}\z"))
                    {
                        throw new JsonException("Uma nota contém uma cor inválida.");
                    }
                }

                return notes;

            }
            catch (JsonException)
            {
                string backupPath = Path.Combine(FolderPath, $"notes-invalid-{Guid.NewGuid():N}.json");
                
                File.Copy(FilePath, backupPath);

                warningMessage = "Não foi possível carregar as notas porque o arquivo contém dados inválidos.\n\n" +
                                 "Uma cópia do arquivo foi preservada em: \n" + backupPath;
                
                return new List<Note>();
            }
        }
    }
}

