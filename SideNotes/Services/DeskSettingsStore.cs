using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using SideNotes.Models;

namespace SideNotes.Services
{
    // Assume o caminho do arquivo, a leitura, a gravação e a validação da posição carregada.
    public sealed class DeskSettingsStore
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SideNotes",
            "desk_settings.json");

        public DeskPosition Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new DeskPosition();

                string json = File.ReadAllText(FilePath);

                DeskPosition? position = JsonSerializer.Deserialize<DeskPosition>(json);

                if (position is null || !double.IsFinite(position.VerticalPosition)) return new DeskPosition();

                position.VerticalPosition = Math.Clamp(position.VerticalPosition, 0, 1);

                return position;

            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                Debug.WriteLine($"Não foi possível carregar a posição do Desk: {ex.Message}");
                return new DeskPosition();
            }
        }

        public void Save(DeskPosition position)
        {
            try
            {
                string folder = Path.GetDirectoryName(FilePath)!;

                Directory.CreateDirectory(folder);

                string json = JsonSerializer.Serialize(
                    position,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                Debug.WriteLine(
                    $"Não foi possível salvar a posição do Desk: {ex.Message}");
            }
        }
    }
}
