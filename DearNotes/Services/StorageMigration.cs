using System;
using System.IO;

namespace DearNotes.Services
{
    public static class StorageMigration
    {
        // Nome usado até o rebrand para DearNotes. Os arquivos originais são mantidos como cópia.
        private const string LegacyFolderName = "SideNotes";

        private const string NotesFileName = "notes.json";

        private const string DeskPositionFileName = "desk_position.json";

        private const string CompletionMarkerFileName =
            ".migrated-from-sidenotes";

        public static bool TryMigrateIfNeeded(out string? errorMessage)
        {
            errorMessage = null;

            string legacyDirectory = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                LegacyFolderName);

            if (!Directory.Exists(legacyDirectory)) return true;

            string markerPath = Path.Combine(
                AppPaths.DataDirectory,
                CompletionMarkerFileName);

            if (File.Exists(markerPath)) return true;

            try
            {
                Directory.CreateDirectory(AppPaths.DataDirectory);

                CopyIfMissing(
                    Path.Combine(legacyDirectory, NotesFileName),
                    AppPaths.NotesFilePath);

                CopyIfMissing(
                    Path.Combine(legacyDirectory, DeskPositionFileName),
                    AppPaths.DeskPositionFilePath);

                File.WriteAllText(
                    markerPath,
                    DateTimeOffset.Now.ToString("O"));

                return true;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException)
            {
                AppLogger.LogError(
                    "Falha ao migrar os dados da pasta SideNotes",
                    exception);

                errorMessage =
                    "Não foi possível migrar as notas da instalação anterior " +
                    $"de {legacyDirectory}.\n\n" +
                    "O aplicativo será encerrado para evitar alterações sobre " +
                    "uma cópia incompleta.";

                return false;
            }
        }

        private static void CopyIfMissing(
            string sourcePath,
            string destinationPath)
        {
            if (!File.Exists(sourcePath)) return;
            if (File.Exists(destinationPath)) return;

            File.Copy(sourcePath, destinationPath);
        }
    }
}
