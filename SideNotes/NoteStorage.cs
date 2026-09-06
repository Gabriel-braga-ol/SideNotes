using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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
            
        File.WriteAllText(FilePath, json);
        }

        public static List<Note> Load()
        {
            if (!File.Exists(FilePath))
            {
                return new List<Note>();
            }

            string json = File.ReadAllText(FilePath);

            return JsonSerializer.Deserialize<List<Note>>(json)
                   ?? new List<Note>();
        }
    }
}

