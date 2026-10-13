using System;
using System.IO;

namespace SideNotes.Services
{
    public static class AppPaths
    {
        public static string DataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "SideNotes");

        public static string NotesFilePath { get; } = Path.Combine(DataDirectory, "notes.json");

        public static string DeskPositionFilePath { get; } = Path.Combine(DataDirectory, "desk_position.json");
    }
}
