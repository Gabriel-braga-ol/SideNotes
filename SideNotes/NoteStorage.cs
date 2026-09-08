using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

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

        public static List<Note> Load()
        {
            if (!File.Exists(FilePath))
            {
                return new List<Note>();
            }

            string json = File.ReadAllText(FilePath);

            try
            {
                return JsonSerializer.Deserialize<List<Note>>(json)
                       ?? throw new JsonException("O arquivo não contém uma lista de notas.");
            }
            catch (JsonException)
            {
                string backupPath = Path.Combine(FolderPath, $"notes-invalid-{Guid.NewGuid():N}.json");
                
                File.Copy(FilePath, backupPath);

                System.Windows.MessageBox.Show(
                    "Não foi possível carregar as notas porque o JSON é inválido.\n\n" +
                    "Uma cópia do arquivo foi preservada em: \n" + backupPath, "SideNotes");
                
                return new List<Note>();
            }
        }
    }
}

