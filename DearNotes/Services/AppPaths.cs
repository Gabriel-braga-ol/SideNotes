using System;
using System.IO;

namespace DearNotes.Services
{
    public static class AppPaths
    {
        public static string DataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "DearNotes");

        public static string NotesFilePath { get; } = Path.Combine(DataDirectory, "notes.json");

        public static string DeskPositionFilePath { get; } = Path.Combine(DataDirectory, "desk_position.json");

        public static string LogsDirectory { get; } = Path.Combine(DataDirectory, "logs");

        public static string LogFilePath { get; } = Path.Combine(LogsDirectory, "app.log");
    }
}
